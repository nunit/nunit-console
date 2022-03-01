// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using NUnit.Engine.Internal.RuntimeFrameworks;
using NUnit.Common;
using NUnit.Engine.Compatibility;

namespace NUnit.Engine
{
    /// <summary>
    /// RuntimeFramework represents a particular version
    /// of a common language runtime implementation.
    /// </summary>
    [Serializable]
    public sealed class RuntimeFramework : IRuntimeFramework
    {
        // TODO: RuntimeFramework was originally created for use in
        // a single-threaded environment. The introduction of parallel
        // execution and especially parallel loading of tests has
        // exposed some weaknesses.
        //
        // Ideally, we should remove all knowledge of the environment
        // from RuntimeFramework. An instance of RuntimeFramework does
        // not need to know, for example, if it is available on the
        // current system. In the present architecture, that's really
        // the job of the RuntimeFrameworkService. Other functions
        // may actually belong in TestAgency.
        //
        // All the static properties of RuntimeFramework need to be
        // examined for thread-safety, particularly CurrentFramework
        // and AvailableFrameworks. The latter caused a problem with
        // parallel loading, which has been fixed for now through a
        // hack added to RuntimeFrameworkService. We may be able to
        // move all this functionality to services, eliminating the
        // use of public static properties here.

        #region IRuntimeFramework Implementation

        /// <summary>
        /// Gets the unique Id for this runtime, such as "net-4.5"
        /// </summary>
        public string Id => Runtime.ToString().ToLower() + "-" + FrameworkVersion.ToString();

        public FrameworkName FrameworkName { get; }

        /// <summary>
        /// The type of this runtime framework
        /// </summary>
        public Runtime Runtime { get; private set; }

        /// <summary>
        /// The framework version for this runtime framework
        /// </summary>
        public Version FrameworkVersion { get; private set; }

        /// <summary>
        /// The CLR version for this runtime framework
        /// </summary>
        public Version ClrVersion { get; private set; }

        /// <summary>
        /// The Profile for this framework, where relevant.
        /// May be null and will have different sets of
        /// values for each Runtime.
        /// </summary>
        public string Profile { get; private set; }

        /// <summary>
        /// Returns the Display name for this framework
        /// </summary>
        public string DisplayName { get; private set; }

        #endregion

        private static RuntimeFramework _currentFramework;
        private static List<RuntimeFramework> _availableFrameworks;

        private static readonly string DEFAULT_WINDOWS_MONO_DIR =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mono");

        /// <summary>
        /// Construct from a runtime type and version. If the version has
        /// two parts, it is taken as a framework version. If it has three
        /// or more, it is taken as a CLR version. In either case, the other
        /// version is deduced based on the runtime type and provided version.
        /// </summary>
        /// <param name="runtime">The runtime type of the framework</param>
        /// <param name="version">The version of the framework</param>
        public RuntimeFramework(Runtime runtime, Version version)
            : this(runtime, version, null)
        {
        }

        /// <summary>
        /// Construct from a runtime type, version and profile. The version
        /// may be either a framework version or a CLR version. If a CLR
        /// version is provided, we try to deduce the framework version but
        /// this may not always be successful, in which case a version of
        /// 0.0 is used.
        /// </summary>
        /// <param name="runtime">The runtime type of the framework.</param>
        /// <param name="version">The version of the framework.</param>
        /// <param name="profile">The profile of the framework. Null if unspecified.</param>
        public RuntimeFramework(Runtime runtime, Version version, string profile)
        {
            Guard.ArgumentValid(IsFrameworkVersion(version), $"{version} is not a valid framework version", nameof(version));

            Runtime = runtime;
            FrameworkVersion = ClrVersion = version;
            ClrVersion = runtime.GetClrVersionForFramework(version);

            Profile = profile;

            DisplayName = GetDefaultDisplayName(runtime, FrameworkVersion, profile);

            FrameworkName = new FrameworkName(runtime.FrameworkIdentifier, FrameworkVersion);
        }

        private bool IsFrameworkVersion(Version v)
        {
            // All known framework versions have either two components or
            // three. If three, then the Build is currently less than 3.
            return v.Major > 0 && v.Minor >= 0 && v.Build < 3 && v.Revision == -1;
        }

        /// <summary>
        /// Static method to return a RuntimeFramework object
        /// for the framework that is currently in use.
        /// </summary>
        public static RuntimeFramework CurrentFramework
        {
            get
            {
                if (_currentFramework == null)
                {
                    Type monoRuntimeType = Type.GetType("Mono.Runtime", false);
                    bool isMono = monoRuntimeType != null;

                    Runtime runtime = isMono
                        ? Runtime.Mono
                        : Runtime.Net;

                    int major = Environment.Version.Major;
                    int minor = Environment.Version.Minor;

                    if (isMono)
                    {
                        switch (major)
                        {
                            case 1:
                                minor = 0;
                                break;
                            case 2:
                                major = 3;
                                minor = 5;
                                break;
                        }
                    }
                    else /* It's windows */
                        if (major == 2)
                        {
                            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\.NETFramework");
                            if (key != null)
                            {
                                string installRoot = key.GetValue("InstallRoot") as string;
                                if (installRoot != null)
                                {
                                    if (Directory.Exists(Path.Combine(installRoot, "v3.5")))
                                    {
                                        major = 3;
                                        minor = 5;
                                    }
                                    else if (Directory.Exists(Path.Combine(installRoot, "v3.0")))
                                    {
                                        major = 3;
                                        minor = 0;
                                    }
                                }
                            }
                        }
                        else if (major == 4 && Type.GetType("System.Reflection.AssemblyMetadataAttribute") != null)
                        {
                            minor = 5;
                        }

                    _currentFramework = new RuntimeFramework(runtime, new Version(major, minor));
                    _currentFramework.ClrVersion = Environment.Version;

                    if (isMono)
                    {
                        if (MonoPrefix == null)
                            MonoPrefix = GetMonoPrefixFromAssembly(monoRuntimeType.Assembly);

                        MethodInfo getDisplayNameMethod = monoRuntimeType.GetMethod(
                            "GetDisplayName", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding);
                        if (getDisplayNameMethod != null)
                        {
                            string displayName = (string)getDisplayNameMethod.Invoke(null, new object[0]);

                            int space = displayName.IndexOf(' ');
                            if (space >= 3) // Minimum length of a version
                            {
                                string version = displayName.Substring(0, space);
                                displayName = "Mono " + version;
                                if (MonoVersion == null)
                                    MonoVersion = new Version(version);
                            }
                            else
                                displayName = "Mono " + displayName;

                            _currentFramework.DisplayName = displayName;
                        }
                    }
                }

                return _currentFramework;
            }
        }

        /// <summary>
        /// Gets an array of all available frameworks
        /// </summary>
        public static RuntimeFramework[] AvailableFrameworks
        {
            get
            {
                if (_availableFrameworks == null)
                    FindAvailableFrameworks();

                return _availableFrameworks.ToArray();
            }
        }

        /// <summary>
        /// The version of Mono in use or null if no Mono runtime
        /// is available on this machine.
        /// </summary>
        /// <value>The mono version.</value>
        private static Version MonoVersion { get; set; }

        /// <summary>
        /// The install directory where the version of mono in
        /// use is located. Null if no Mono runtime is present.
        /// </summary>
        private static string MonoPrefix { get; set; }

        /// <summary>
        /// The path to the mono executable, based on the
        /// Mono prefix if available. Otherwise, uses "mono",
        /// to invoke a script of that name.
        /// </summary>
        public static string MonoExePath
        {
            get
            {
                return MonoPrefix != null && Environment.OSVersion.Platform == PlatformID.Win32NT
                    ? Path.Combine(MonoPrefix, "bin/mono.exe")
                    : "mono";
            }
        }

        /// <summary>
        /// Returns true if the current RuntimeFramework is available.
        /// In the current implementation, only Mono and Microsoft .NET
        /// are supported.
        /// </summary>
        /// <returns>True if it's available, false if not</returns>
        public bool IsAvailable
        {
            get
            {
                foreach (RuntimeFramework framework in AvailableFrameworks)
                    if (framework.Supports(this))
                        return true;

                return false;
            }
        }

        /// <summary>
        /// Parses a string representing a RuntimeFramework.
        /// The string may be just a RuntimeType name or just
        /// a Version or a hyphenated RuntimeType-Version or
        /// a Version prefixed by 'v'.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static RuntimeFramework Parse(string s)
        {
            Guard.ArgumentNotNullOrEmpty(s, nameof(s));

            string[] parts = s.Split(new char[] { '-' });
            Guard.ArgumentValid(parts.Length == 2 && parts[0].Length > 0 && parts[1].Length > 0, "RuntimeFramework id not in correct format", nameof(s));

            var runtime = Runtime.Parse(parts[0]);
            var version = new Version(parts[1]);
            return new RuntimeFramework(runtime, version);
        }

        public static bool TryParse(string s, out RuntimeFramework runtimeFramework)
        {
            try
            {
                runtimeFramework = Parse(s);
                return true;
            }
            catch
            {
                runtimeFramework = null;
                return false;
            }
        }

        public static RuntimeFramework FromFrameworkName(string frameworkName)
        {
            return FromFrameworkName(new FrameworkName(frameworkName));
        }

        public static RuntimeFramework FromFrameworkName(FrameworkName frameworkName)
        {
            return new RuntimeFramework(GetRuntimeTypeFromFrameworkIdentifier(frameworkName.Identifier), frameworkName.Version, frameworkName.Profile);
        }

        private static Runtime GetRuntimeTypeFromFrameworkIdentifier(string identifier)
        {
            switch (identifier)
            {
                case FrameworkIdentifiers.NetFramework:
                    return Runtime.Net;
                case FrameworkIdentifiers.NetCoreApp:
                    return Runtime.NetCore;
                case FrameworkIdentifiers.NetStandard:
                    throw new NUnitEngineException(
                        "Test assemblies must target a specific platform, rather than .NETStandard.");
            }

            throw new NUnitEngineException("Unrecognized Target Framework Identifier: " + identifier);
        }

        /// <summary>
        /// Overridden to return the short name of the framework
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Id;
        }

        /// <summary>
        /// Returns true if the current framework matches the
        /// one supplied as an argument. Both the RuntimeType
        /// and the version must match.
        /// 
        /// Two RuntimeTypes match if they are equal, if either one
        /// is RuntimeType.Any or if one is RuntimeType.Net and
        /// the other is RuntimeType.Mono.
        /// 
        /// Two versions match if all specified version components
        /// are equal. Negative (i.e. unspecified) version
        /// components are ignored.
        /// </summary>
        /// <param name="target">The RuntimeFramework to be matched.</param>
        /// <returns><c>true</c> on match, otherwise <c>false</c></returns>
        public bool Supports(RuntimeFramework target)
        {
            if (!this.Supports(target.Runtime))
                return false;

            return VersionsMatch(this.ClrVersion, target.ClrVersion)
                && this.FrameworkVersion.Major >= target.FrameworkVersion.Major
                && this.FrameworkVersion.Minor >= target.FrameworkVersion.Minor;
        }

        private bool Supports(Runtime targetRuntime)
        {
            if (this.Runtime == targetRuntime)
                return true;

            if (this.Runtime == Runtime.Net && targetRuntime == Runtime.Mono)
                return true;

            if (this.Runtime == Runtime.Mono && targetRuntime == Runtime.Net)
                return true;

            return false;
        }

        public bool CanLoad(IRuntimeFramework requested)
        {
            return FrameworkVersion >= requested.FrameworkVersion;
        }

        private static bool IsRuntimeTypeName(string name)
        {
            foreach (string item in Enum.GetNames(typeof(Runtime)))
                if (item.ToLower() == name.ToLower())
                    return true;

            return false;
        }

        private static string GetDefaultDisplayName(Runtime runtime, Version version, string profile)
        {
            string displayName = $"{runtime.DisplayName} {version}";

            if (!string.IsNullOrEmpty(profile) && profile != "Full")
                displayName += " - " + profile;

            return displayName;
        }

        private static bool VersionsMatch(Version v1, Version v2)
        {
            return v1.Major == v2.Major &&
                   v1.Minor == v2.Minor &&
                  (v1.Build < 0 || v2.Build < 0 || v1.Build == v2.Build) &&
                  (v1.Revision < 0 || v2.Revision < 0 || v1.Revision == v2.Revision);
        }

        private static string GetMonoPrefixFromAssembly(Assembly assembly)
        {
            string prefix = assembly.Location;

            // In all normal mono installations, there will be sufficient
            // levels to complete the four iterations. But just in case
            // files have been copied to some non-standard place, we check.
            for (int i = 0; i < 4; i++)
            {
                string dir = Path.GetDirectoryName(prefix);
                if (string.IsNullOrEmpty(dir)) break;

                prefix = dir;
            }

            return prefix;
        }

        private static void FindAvailableFrameworks()
        {
            _availableFrameworks = new List<RuntimeFramework>();

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                _availableFrameworks.AddRange(DotNetFrameworkLocator.FindDotNetFrameworks());

            FindDefaultMonoFramework();
            FindDotNetCoreFrameworks();
        }

        private static void FindDefaultMonoFramework()
        {
            if (CurrentFramework.Runtime == Runtime.Mono)
                UseCurrentMonoFramework();
            else
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                FindBestMonoFrameworkOnWindows();
        }

        private static void UseCurrentMonoFramework()
        {
            Debug.Assert(CurrentFramework.Runtime == Runtime.Mono && MonoPrefix != null && MonoVersion != null);

            // Multiple profiles are no longer supported with Mono 4.0
            if (MonoVersion.Major < 4 && FindAllMonoProfiles() > 0)
                    return;

            // If Mono 4.0+ or no profiles found, just use current runtime
            _availableFrameworks.Add(RuntimeFramework.CurrentFramework);
        }

        private static void FindBestMonoFrameworkOnWindows()
        {
            // First, look for recent frameworks that use the Software\Mono Key
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Mono");

            if (key != null && (int)key.GetValue("Installed", 0) == 1)
            {
                string version = key.GetValue("Version") as string;
                MonoPrefix = key.GetValue("SdkInstallRoot") as string;

                if (version != null)
                {
                    MonoVersion = new Version(version);
                    AddMonoFramework(new Version(4, 5), null);
                    return;
                }
            }

            // Some later 3.x Mono releases didn't use the registry
            // so check in standard location for them.
            if (Directory.Exists(DEFAULT_WINDOWS_MONO_DIR))
            {
                MonoPrefix = DEFAULT_WINDOWS_MONO_DIR;
                AddMonoFramework(new Version(4, 5), null);
                return;
            }

            // Look in the Software\Novell key for older versions
            key = Registry.LocalMachine.OpenSubKey(@"Software\Novell\Mono");
            if (key != null)
            {
                string version = key.GetValue("DefaultCLR") as string;
                if (version != null)
                {
                    RegistryKey subKey = key.OpenSubKey(version);
                    if (subKey != null)
                    {
                        MonoPrefix = subKey.GetValue("SdkInstallRoot") as string;
                        MonoVersion = new Version(version);

                        FindAllMonoProfiles();
                    }
                }
            }
        }

        private static int FindAllMonoProfiles()
        {
            int count = 0;

            if (MonoPrefix != null)
            {
                if (File.Exists(Path.Combine(MonoPrefix, "lib/mono/1.0/mscorlib.dll")))
                {
                    AddMonoFramework(new Version(1, 1, 4322), "1.0");
                    count++;
                }

                if (File.Exists(Path.Combine(MonoPrefix, "lib/mono/2.0/mscorlib.dll")))
                {
                    AddMonoFramework(new Version(2, 0), "2.0");
                    count++;
                }

                if (Directory.Exists(Path.Combine(MonoPrefix, "lib/mono/3.5")))
                {
                    AddMonoFramework(new Version(3, 5), "3.5");
                    count++;
                }

                if (File.Exists(Path.Combine(MonoPrefix, "lib/mono/4.0/mscorlib.dll")))
                {
                    AddMonoFramework(new Version(4, 0), "4.0");
                    count++;
                }

                if (File.Exists(Path.Combine(MonoPrefix, "lib/mono/4.5/mscorlib.dll")))
                {
                    AddMonoFramework(new Version(4, 5), "4.5");
                    count++;
                }
            }

            return count;
        }

        private static void AddMonoFramework(Version frameworkVersion, string profile)
        {
            var framework = new RuntimeFramework(Runtime.Mono, frameworkVersion)
            {
                Profile = profile,
                DisplayName = MonoVersion != null
                    ? "Mono " + MonoVersion.ToString() + " - " + profile + " Profile"
                    : "Mono - " + profile + " Profile"
            };

            _availableFrameworks.Add(framework);
        }

        private static void FindDotNetCoreFrameworks()
        {
            const string WINDOWS_INSTALL_DIR = "C:\\Program Files\\dotnet\\";
            const string LINUX_INSTALL_DIR = "/usr/shared/dotnet/";
            string INSTALL_DIR = Path.DirectorySeparatorChar == '\\'
                ? WINDOWS_INSTALL_DIR
                : LINUX_INSTALL_DIR;

            if (!Directory.Exists(INSTALL_DIR))
                return;
            if (!File.Exists(Path.Combine(INSTALL_DIR, "dotnet.exe")))
                return;

            string runtimeDir = Path.Combine(INSTALL_DIR, Path.Combine("shared", "Microsoft.NETCore.App"));
            if (!Directory.Exists(runtimeDir))
                return;

            var dirList = new DirectoryInfo(runtimeDir).GetDirectories();
            var dirNames = new List<string>();
            foreach (var dir in dirList)
                dirNames.Add(dir.Name);
            var runtimes = GetNetCoreRuntimesFromDirectoryNames(dirNames);

            _availableFrameworks.AddRange(runtimes);
        }

        // Deal with oddly named directories, which may sometimes appear when previews are installed
        internal static IList<RuntimeFramework> GetNetCoreRuntimesFromDirectoryNames(IEnumerable<string> dirNames)
        {
            const string VERSION_CHARS = ".0123456789";
            var runtimes = new List<RuntimeFramework>();

            foreach (string dirName in dirNames)
            {
                int len = 0;
                foreach (char c in dirName)
                {
                    if (VERSION_CHARS.IndexOf(c) >= 0)
                        len++;
                    else
                        break;
                }

                if (len == 0)
                    continue;

                Version fullVersion = null;
                try
                {
                    fullVersion = new Version(dirName.Substring(0, len));
                }
                catch
                {
                    continue;
                }

                var newVersion = new Version(fullVersion.Major, fullVersion.Minor);
                int count = runtimes.Count;
                if (count > 0 && runtimes[count - 1].FrameworkVersion == newVersion)
                    continue;

                runtimes.Add(new RuntimeFramework(Runtime.NetCore, newVersion));
            }

            return runtimes;
        }
    }
}
#endif
