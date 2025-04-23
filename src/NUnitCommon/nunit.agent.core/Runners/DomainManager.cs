// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt
#if NETFRAMEWORK
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Security;
using System.Security.Policy;
using System.Security.Principal;
using System.Linq;
using NUnit.Common;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// The DomainManager class handles the creation and unloading
    /// of domains as needed and keeps track of all existing domains.
    /// </summary>
    public class DomainManager
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(DomainManager));

        private static readonly PropertyInfo TargetFrameworkNameProperty =
            typeof(AppDomainSetup).GetProperty("TargetFrameworkName", BindingFlags.Public | BindingFlags.Instance)!;

        /// <summary>
        /// Construct an application domain for running a test package
        /// </summary>
        /// <param name="package">The TestPackage to be run</param>
        public AppDomain CreateDomain(TestPackage package)
        {
            AppDomainSetup setup = CreateAppDomainSetup(package);

            string hashCode = string.Empty;
            if (package.Name is not null)
            {
                hashCode = package.Name.GetHashCode().ToString("x") + "-";
            }

            string domainName = "domain-" + hashCode + package.Name;
            Evidence evidence = new Evidence(AppDomain.CurrentDomain.Evidence);

            log.Info("Creating application domain " + domainName);

            AppDomain runnerDomain = AppDomain.CreateDomain(domainName, evidence, setup);

            // Set PrincipalPolicy for the domain if called for in the package settings
            if (package.Settings.ContainsKey(EnginePackageSettings.PrincipalPolicy))
            {
                PrincipalPolicy policy = (PrincipalPolicy)Enum.Parse(typeof(PrincipalPolicy),
                    package.GetSetting(EnginePackageSettings.PrincipalPolicy, "UnauthenticatedPrincipal"));

                runnerDomain.SetPrincipalPolicy(policy);
            }

            return runnerDomain;
        }

        // Made separate and internal for testing
        private static AppDomainSetup CreateAppDomainSetup(TestPackage package)
        {
            AppDomainSetup setup = new AppDomainSetup();

            //For parallel tests, we need to use distinct application name
            setup.ApplicationName = "Tests" + "_" + Environment.TickCount;

            string appBase = GetApplicationBase(package).ShouldNotBeNull();
            setup.ApplicationBase = appBase;
            setup.ConfigurationFile = GetConfigFile(appBase, package);
            setup.PrivateBinPath = GetPrivateBinPath(appBase, package);

            if (!string.IsNullOrEmpty(package.FullName))
            {
                // Setting the target framework is only supported when running with
                // multiple AppDomains, one per assembly.
                // TODO: Remove this limitation

                // .NET versions greater than v4.0 report as v4.0, so look at
                // the TargetFrameworkAttribute on the assembly if it exists
                // If property is null, .NET 4.5+ is not installed, so there is no need
                if (TargetFrameworkNameProperty is not null)
                {
                    var frameworkName = package.GetSetting(EnginePackageSettings.ImageTargetFrameworkName, string.Empty);
                    if (frameworkName != string.Empty)
                        TargetFrameworkNameProperty.SetValue(setup, frameworkName, null);
                }
            }

            if (package.GetSetting(EnginePackageSettings.ShadowCopyFiles, false))
            {
                setup.ShadowCopyFiles = "true";
                setup.ShadowCopyDirectories = setup.ApplicationBase;
            }
            else
                setup.ShadowCopyFiles = "false";

            return setup;
        }

        public static void Unload(AppDomain domain)
        {
            new DomainUnloader(domain).Unload();
        }

        private class DomainUnloader
        {
            private readonly AppDomain _domain;
            private Thread? _unloadThread;
            private NUnitEngineException? _unloadException;

            public DomainUnloader(AppDomain domain)
            {
                _domain = domain;
            }

            public void Unload()
            {
                _unloadThread = new Thread(new ThreadStart(UnloadOnThread));
                _unloadThread.Start();

                var timeout = TimeSpan.FromSeconds(30);

                if (!_unloadThread.Join(timeout))
                {
                    var msg = DomainDetailsBuilder.DetailsFor(_domain,
                        $"Unable to unload application domain: unload thread timed out after {timeout.TotalSeconds} seconds.");

                    log.Error(msg);
                    Kill(_unloadThread);

                    throw new NUnitEngineUnloadException(msg);
                }

                if (_unloadException is not null)
                    throw new NUnitEngineUnloadException("Exception encountered unloading application domain", _unloadException);
            }

            private void UnloadOnThread()
            {
                try
                {
                    // Uncomment to simulate an error in unloading
                    //throw new CannotUnloadAppDomainException("Testing: simulated unload error");

                    // Uncomment to simulate a timeout while unloading
                    //while (true) ;

                    AppDomain.Unload(_domain);
                }
                catch (Exception ex)
                {
                    // We assume that the tests did something bad and just leave
                    // the orphaned AppDomain "out there".
                    var msg = DomainDetailsBuilder.DetailsFor(_domain,
                        $"Exception encountered unloading application domain: {ex.Message}");

                    _unloadException = new NUnitEngineException(msg);
                    log.Error(msg);
                }
            }
        }

        /// <summary>
        /// Figure out the ApplicationBase for a package
        /// </summary>
        /// <param name="package">The package</param>
        /// <returns>The ApplicationBase</returns>
        public static string? GetApplicationBase(TestPackage package)
        {
            Guard.ArgumentNotNull(package);

            var appBase = package.GetSetting(EnginePackageSettings.BasePath, string.Empty);

            if (string.IsNullOrEmpty(appBase))
                appBase = string.IsNullOrEmpty(package.FullName)
                    ? GetCommonAppBase(package.SubPackages)
                    : Path.GetDirectoryName(package.FullName);

            if (!string.IsNullOrEmpty(appBase))
            {
                char lastChar = appBase[appBase.Length - 1];
                if (lastChar != Path.DirectorySeparatorChar && lastChar != Path.AltDirectorySeparatorChar)
                    appBase += Path.DirectorySeparatorChar;
            }

            return appBase;
        }

        public static string? GetConfigFile(string appBase, TestPackage package)
        {
            Guard.ArgumentNotNullOrEmpty(appBase);
            Guard.ArgumentNotNull(package);

            // Use provided setting if available
            string configFile = package.GetSetting(EnginePackageSettings.ConfigurationFile, string.Empty);
            if (configFile != string.Empty)
                return Path.Combine(appBase, configFile);

            // The ProjectService adds any project config to the settings.
            // So, at this point, we only want to handle assemblies or an
            // anonymous package created from the command-line.
            string? fullName = package.FullName;
            if (IsExecutable(fullName))
                return fullName + ".config";

            // Command-line package gets no config unless it's a single assembly
            if (string.IsNullOrEmpty(fullName) && package.SubPackages.Count == 1)
            {
                fullName = package.SubPackages[0].FullName;
                if (IsExecutable(fullName))
                    return fullName + ".config";
            }

            // No config file will be specified
            return null;
        }

        private static bool IsExecutable(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            string ext = Path.GetExtension(fileName).ToLower();
            return ext == ".dll" || ext == ".exe";
        }

        public static string? GetCommonAppBase(IList<TestPackage> packages)
        {
            var assemblies = new List<string>();

            // All subpackages have full names, but this is a public method in a public class so we have no control.
            foreach (var package in packages.Where(p => p.FullName is not null))
                assemblies.Add(package.FullName!);

            return GetCommonAppBase(assemblies);
        }

        public static string? GetCommonAppBase(IList<string> assemblies)
        {
            string? commonBase = null;

            foreach (string assembly in assemblies)
            {
                string? dir = Path.GetDirectoryName(Path.GetFullPath(assembly))!;
                if (commonBase is null)
                    commonBase = dir;
                else
                    while (commonBase is not null && !PathUtils.SamePathOrUnder(commonBase, dir))
                        commonBase = Path.GetDirectoryName(commonBase)!;
            }

            return commonBase;
        }

        public static string? GetPrivateBinPath(string basePath, string fileName)
        {
            return GetPrivateBinPath(basePath, new string[] { fileName });
        }

        public static string? GetPrivateBinPath(string appBase, TestPackage package)
        {
            var binPath = package.GetSetting(EnginePackageSettings.PrivateBinPath, string.Empty);

            if (package.GetSetting(EnginePackageSettings.AutoBinPath, binPath == string.Empty))
                binPath = package.SubPackages.Count > 0
                    ? GetPrivateBinPath(appBase, package.SubPackages)
                    : package.FullName is not null
                        ? GetPrivateBinPath(appBase, package.FullName)
                        : null;

            return binPath;
        }

        public static string? GetPrivateBinPath(string basePath, IList<TestPackage> packages)
        {
            var assemblies = new List<string>();
            foreach (var package in packages.Where(p => p.FullName is not null))
                assemblies.Add(package.FullName!);

            return GetPrivateBinPath(basePath, assemblies);
        }

        public static string? GetPrivateBinPath(string basePath, IList<string> assemblies)
        {
            List<string> dirList = new List<string>();
            StringBuilder sb = new StringBuilder(200);

            foreach (string assembly in assemblies)
            {
                string? dir = PathUtils.RelativePath(
                    Path.GetFullPath(basePath),
                    Path.GetDirectoryName(Path.GetFullPath(assembly))!);
                if (dir is not null && dir != string.Empty && dir != "." && !dirList.Contains(dir))
                {
                    dirList.Add(dir);
                    if (sb.Length > 0)
                        sb.Append(Path.PathSeparator);
                    sb.Append(dir);
                }
            }

            return sb.Length == 0 ? null : sb.ToString();
        }

        /// <summary>
        /// Do our best to kill a thread, passing state info
        /// </summary>
        /// <param name="thread">The thread to kill</param>
        private static void Kill(Thread thread)
        {
            try
            {
                thread.Abort();
            }
            catch (ThreadStateException)
            {
                // Although obsolete, this use of Resume() takes care of
                // the odd case where a ThreadStateException is received.
#pragma warning disable 0618, 0612    // Thread.Resume has been deprecated
                thread.Resume();
#pragma warning restore 0618, 0612   // Thread.Resume has been deprecated
            }

            if ((thread.ThreadState & System.Threading.ThreadState.WaitSleepJoin) != 0)
                thread.Interrupt();
        }
    }
}
#endif