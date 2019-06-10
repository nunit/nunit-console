// ***********************************************************************
// Copyright (c) 2007 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

#if !NETSTANDARD
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using NUnit.Engine.Internal.RuntimeFrameworks;

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

        /// <summary>
        /// DefaultVersion is an empty Version, used to indicate that
        /// NUnit should select the CLR version to use for the test.
        /// </summary>
        public static readonly Version DefaultVersion = new Version(0, 0);

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
        public RuntimeFramework(RuntimeType runtime, Version version)
            : this(runtime, version, null)
        {
        }

        /// <summary>
        /// Construct from a runtime type, version and profile. If the version has
        /// two parts, it is taken as a framework version. If it has three
        /// or more, it is taken as a CLR version. In either case, the other
        /// version is deduced based on the runtime type and provided version.
        /// </summary>
        /// <param name="runtime">The runtime type of the framework.</param>
        /// <param name="version">The version of the framework.</param>
        /// <param name="profile">The profile of the framework. Null if unspecified.</param>
        public RuntimeFramework(RuntimeType runtime, Version version, string profile)
        {
            Runtime = runtime;

            if (version.Build < 0)
                InitFromFrameworkVersion(version);
            else
                InitFromClrVersion(version);

            Profile = profile;

            DisplayName = GetDefaultDisplayName(runtime, FrameworkVersion, profile);
        }

        private void InitFromFrameworkVersion(Version version)
        {
            this.FrameworkVersion = this.ClrVersion = version;

            if (version.Major > 0) // 0 means any version
                switch (Runtime)
                {
                    case RuntimeType.Net:
                    case RuntimeType.Mono:
                    case RuntimeType.Any:
                        switch (version.Major)
                        {
                            case 1:
                                switch (version.Minor)
                                {
                                    case 0:
                                        this.ClrVersion = Runtime == RuntimeType.Mono
                                            ? new Version(1, 1, 4322)
                                            : new Version(1, 0, 3705);
                                        break;
                                    case 1:
                                        if (Runtime == RuntimeType.Mono)
                                            this.FrameworkVersion = new Version(1, 0);
                                        this.ClrVersion = new Version(1, 1, 4322);
                                        break;
                                    default:
                                        ThrowInvalidFrameworkVersion(version);
                                        break;
                                }
                                break;
                            case 2:
                            case 3:
                                this.ClrVersion = new Version(2, 0, 50727);
                                break;
                            case 4:
                                this.ClrVersion = new Version(4, 0, 30319);
                                break;
                            default:
                                ThrowInvalidFrameworkVersion(version);
                                break;
                        }
                        break;
                }
        }

        private static void ThrowInvalidFrameworkVersion(Version version)
        {
            throw new ArgumentException("Unknown framework version " + version.ToString(), "version");
        }

        private void InitFromClrVersion(Version version)
        {
            this.FrameworkVersion = new Version(version.Major, version.Minor);
            this.ClrVersion = version;
            if (Runtime == RuntimeType.Mono && version.Major == 1)
                this.FrameworkVersion = new Version(1, 0);
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

                    RuntimeType runtime = isMono
                        ? RuntimeType.Mono
                        : RuntimeType.Net;

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
        public static Version MonoVersion { get; private set; }

        /// <summary>
        /// The install directory where the version of mono in
        /// use is located. Null if no Mono runtime is present.
        /// </summary>
        public static string MonoPrefix { get; private set; }

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
        /// Gets the unique Id for this runtime, such as "net-4.5"
        /// </summary>
        public string Id
        {
            get
            {
                if (this.AllowAnyVersion)
                {
                    return Runtime.ToString().ToLower();
                }
                else
                {
                    string vstring = FrameworkVersion.ToString();
                    if (Runtime == RuntimeType.Any)
                        return "v" + vstring;
                    else
                        return Runtime.ToString().ToLower() + "-" + vstring;
                }
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
        /// The type of this runtime framework
        /// </summary>
        public RuntimeType Runtime { get; private set; }

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
        /// Return true if any CLR version may be used in
        /// matching this RuntimeFramework object.
        /// </summary>
        public bool AllowAnyVersion
        {
            get { return this.ClrVersion == DefaultVersion; }
        }

        /// <summary>
        /// Returns the Display name for this framework
        /// </summary>
        public string DisplayName { get; private set; }

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
            RuntimeType runtime = RuntimeType.Any;
            Version version = DefaultVersion;

            string[] parts = s.Split(new char[] { '-' });
            if (parts.Length == 2)
            {
                runtime = (RuntimeType)System.Enum.Parse(typeof(RuntimeType), parts[0], true);
                string vstring = parts[1];
                if (vstring != "")
                    version = new Version(vstring);
            }
            else if (char.ToLower(s[0]) == 'v')
            {
                version = new Version(s.Substring(1));
            }
            else if (IsRuntimeTypeName(s))
            {
                runtime = (RuntimeType)System.Enum.Parse(typeof(RuntimeType), s, true);
            }
            else
            {
                version = new Version(s);
            }

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
        /// one supplied as an argument. Two frameworks match
        /// if their runtime types are the same or either one
        /// is RuntimeType.Any and all specified version components
        /// are equal. Negative (i.e. unspecified) version
        /// components are ignored.
        /// </summary>
        /// <param name="target">The RuntimeFramework to be matched.</param>
        /// <returns><c>true</c> on match, otherwise <c>false</c></returns>
        public bool Supports(RuntimeFramework target)
        {
            if (this.Runtime != RuntimeType.Any
                && target.Runtime != RuntimeType.Any
                && this.Runtime != target.Runtime)
                return false;

            if (this.AllowAnyVersion || target.AllowAnyVersion)
                return true;

            return VersionsMatch(this.ClrVersion, target.ClrVersion)
                && this.FrameworkVersion.Major >= target.FrameworkVersion.Major
                && this.FrameworkVersion.Minor >= target.FrameworkVersion.Minor;
        }

        public bool CanLoad(IRuntimeFramework requested)
        {
            return FrameworkVersion >= requested.FrameworkVersion;
        }

        private static bool IsRuntimeTypeName(string name)
        {
            foreach (string item in Enum.GetNames(typeof(RuntimeType)))
                if (item.ToLower() == name.ToLower())
                    return true;

            return false;
        }

        private static string GetDefaultDisplayName(RuntimeType runtime, Version version, string profile)
        {
            string displayName;

            if (version == DefaultVersion)
                displayName = GetRuntimeDisplayName(runtime);
            else if (runtime == RuntimeType.Any)
                displayName = "v" + version.ToString();
            else
                displayName = GetRuntimeDisplayName(runtime) + " " + version.ToString();

            if (!string.IsNullOrEmpty(profile) && profile != "Full")
                displayName += " - " + profile;

            return displayName;
        }

        private static string GetRuntimeDisplayName(RuntimeType runtime)
        {
            switch (runtime)
            {
                case RuntimeType.Net:
                    return ".NET";
                default:
                    return runtime.ToString();
            }
        }

        private static bool VersionsMatch(Version v1, Version v2)
        {
            return v1.Major == v2.Major &&
                   v1.Minor == v2.Minor &&
                  (v1.Build < 0 || v2.Build < 0 || v1.Build == v2.Build) &&
                  (v1.Revision < 0 || v2.Revision < 0 || v1.Revision == v2.Revision);
        }

        private static void FindAvailableFrameworks()
        {
            _availableFrameworks = new List<RuntimeFramework>();

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                _availableFrameworks.AddRange(DotNetFrameworkLocator.FindDotNetFrameworks());

            FindDefaultMonoFramework();
        }

        private static void FindDefaultMonoFramework()
        {
            if (CurrentFramework.Runtime == RuntimeType.Mono)
                UseCurrentMonoFramework();
            else
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                FindBestMonoFrameworkOnWindows();
        }

        private static void UseCurrentMonoFramework()
        {
            Debug.Assert(CurrentFramework.Runtime == RuntimeType.Mono && MonoPrefix != null && MonoVersion != null);

            // Multiple profiles are no longer supported with Mono 4.0
            if (MonoVersion.Major < 4 && FindAllMonoProfiles() > 0)
                    return;

            // If Mono 4.0+ or no profiles found, just use current runtime
            _availableFrameworks.Add(RuntimeFramework.CurrentFramework);
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
            var framework = new RuntimeFramework(RuntimeType.Mono, frameworkVersion)
            {
                Profile = profile,
                DisplayName = MonoVersion != null
                    ? "Mono " + MonoVersion.ToString() + " - " + profile + " Profile"
                    : "Mono - " + profile + " Profile"
            };

            _availableFrameworks.Add(framework);
        }
    }
}
#endif