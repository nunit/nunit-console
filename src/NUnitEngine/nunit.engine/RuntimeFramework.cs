// ***********************************************************************
// Copyright (c) 2007 Charlie Poole
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace NUnit.Engine
{
    /// <summary>
    /// RuntimeFramework represents a particular version
    /// of a common language runtime implementation.
    /// </summary>
    [Serializable]
    public sealed class RuntimeFramework : IRuntimeFramework
    {
        #region Static Fields

        /// <summary>
        /// DefaultVersion is an empty Version, used to indicate that
        /// NUnit should select the CLR version to use for the test.
        /// </summary>
        public static readonly Version DefaultVersion = new Version(0, 0);

        private static RuntimeFramework _currentFramework;
        private static List<RuntimeFramework> _availableFrameworks;

        #endregion

        #region Constructors

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
        /// <param name="runtime">The runtime type of the framework</param>
        /// <param name="version">The version of the framework</param>
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

                    case RuntimeType.Silverlight:
                        this.ClrVersion = version.Major >= 4
                            ? new Version(4, 0, 60310)
                            : new Version(2, 0, 50727);
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

        #endregion

        #region Static Properties

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
                        : Environment.OSVersion.Platform == PlatformID.WinCE
                            ? RuntimeType.NetCF
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
                                _currentFramework.MonoVersion = new Version(version);
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
        // TODO: Special handling for netcf
        public static RuntimeFramework[] AvailableFrameworks
        {
            get
            {
                if (_availableFrameworks == null)
                    FindAvailableFrameworks();

                return _availableFrameworks.ToArray();
            }
        }

        #endregion

        #region Instance Properties

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
        /// The version of Mono in use or null if not Mono runtime.
        /// </summary>
        /// <value>The mono version.</value>
        public Version MonoVersion { get; set; }

        /// <summary>
        /// The Profile for this framwork, where relevant.
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

        #endregion

        #region Public Methods

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

        /// <summary>
        /// Returns the best available framework that matches a target framework.
        /// If the target framework has a build number specified, then an exact
        /// match is needed. Otherwise, the matching framework with the highest
        /// build number is used.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static RuntimeFramework GetBestAvailableFramework(RuntimeFramework target)
        {
            RuntimeFramework result = target;

            if (target.ClrVersion.Build < 0)
            {
                foreach (RuntimeFramework framework in AvailableFrameworks)
                    if (framework.Supports(target) &&
                        framework.ClrVersion.Build > result.ClrVersion.Build)
                    {
                        result = framework;
                    }
            }

            return result;
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

        #endregion

        #region Helper Methods - General

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
            {
                FindDotNetFrameworks();
                FindMonoFrameworksOnWindows();
            }
            else
            {
                FindMonoFrameworksOnLinux();
            }
        }

        #endregion

        #region Helper Methods - Mono

        private static void FindMonoFrameworksOnWindows()
        {
            FindRecentMonoFrameworksOnWindows();
            FindOlderMonoFrameworksOnWindows();
            //AppendDefaultMonoFrameworkOnWindows();
        }

        private static void FindMonoFrameworksOnLinux()
        {
            // Get Current runtime - it should be Mono, but check
            var current = RuntimeFramework.CurrentFramework;

            // First check for profiles - only found for older framework versions
            int originalCount = _availableFrameworks.Count;
            string libMonoDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
            string monoPrefix = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(libMonoDir)));
            FindAllMonoProfiles(monoPrefix, current.MonoVersion);
            if (_availableFrameworks.Count > originalCount)
                return;

            // If no profiles, we may be on a newer mono framework
            if (current.Runtime == RuntimeType.Mono)
                _availableFrameworks.Add(RuntimeFramework.CurrentFramework);
        }

        private static void FindRecentMonoFrameworksOnWindows()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Mono");
            Version monoVersion = null;

            if (key != null && (int)key.GetValue("Installed", 0) == 1)
            {
                var version = key.GetValue("Version") as string;
                if (version != null)
                {
                    monoVersion = new Version(version);
                }
            }
            else if (!Directory.Exists(@"C:\Program Files\Mono"))
                return;

            if (monoVersion != null)
            {
                AddMonoFramework(monoVersion, new Version(4, 5));
            }
        }

        private static void FindOlderMonoFrameworksOnWindows()
        {
            // Use registry to find alternate versions
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Novell\Mono");
            if (key == null) return;

            foreach (string version in key.GetSubKeyNames())
            {
                RegistryKey subKey = key.OpenSubKey(version);
                if (subKey == null) continue;

                string monoPrefix = subKey.GetValue("SdkInstallRoot") as string;
                if (monoPrefix != null)
                    FindAllMonoProfiles(monoPrefix, new Version(version));
            }
        }

        // Keeping this for the moment, till we are sure we don't want it
        //private static void FindOlderMonoDefaultFrameworkOnWindows()
        //{
        //    string monoPrefix = null;
        //    string version = null;

        //    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        //    {
        //        RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Novell\Mono");
        //        if (key != null)
        //        {
        //            version = key.GetValue("DefaultCLR") as string;
        //            if (version != null && version != "")
        //            {
        //                key = key.OpenSubKey(version);
        //                if (key != null)
        //                    monoPrefix = key.GetValue("SdkInstallRoot") as string;
        //            }
        //        }
        //    }

        //    FindAllMonoProfiles(monoPrefix, version);
        //}

        private static void FindAllMonoProfiles(string monoPrefix, Version monoVersion)
        {
            if (monoPrefix != null)
            {
                if (File.Exists(Path.Combine(monoPrefix, "lib/mono/1.0/mscorlib.dll")))
                    AddMonoFramework(monoVersion, new Version(1, 1, 4322), "1.0");

                if (File.Exists(Path.Combine(monoPrefix, "lib/mono/2.0/mscorlib.dll")))
                    AddMonoFramework(monoVersion, new Version(2, 0), "2.0");

                if (Directory.Exists(Path.Combine(monoPrefix, "lib/mono/3.5")))
                    AddMonoFramework(monoVersion, new Version(3, 5), "3.5");

                if (File.Exists(Path.Combine(monoPrefix, "lib/mono/4.0/mscorlib.dll")))
                    AddMonoFramework(monoVersion, new Version(4, 0), "4.0");

                if (File.Exists(Path.Combine(monoPrefix, "lib/mono/4.5/mscorlib.dll")))
                    AddMonoFramework(monoVersion, new Version(4, 5), "4.5");
            }
        }

        private static void AddMonoFramework(Version monoVersion, Version frameworkVersion)
        {
            var framework = new RuntimeFramework(RuntimeType.Mono, frameworkVersion);
            framework.DisplayName = monoVersion != null
                ? "Mono " + monoVersion.ToString()
                : "Mono";
            _availableFrameworks.Add(framework);
        }

        private static void AddMonoFramework(Version monoVersion, Version frameworkVersion, string profile)
        {
            var framework = new RuntimeFramework(RuntimeType.Mono, frameworkVersion, profile);
            framework.DisplayName = monoVersion != null
                ? "Mono " + monoVersion.ToString() + " - " + profile + " Profile"
                : "Mono - " + profile + " Profile";
            _availableFrameworks.Add(framework);
        }

        #endregion

        #region Helper Methods - .NET

        private static void FindDotNetFrameworks()
        {
            // Handle Version 1.0, using a different registry key
            FindExtremelyOldDotNetFrameworkVersions();

            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\NET Framework Setup\NDP");
            if (key != null)
            {
                foreach (string name in key.GetSubKeyNames())
                {
                    if (name.StartsWith("v") && name != "v4.0") // v4.0 is a duplicate, legacy key
                    {
                        var versionKey = key.OpenSubKey(name);
                        if (versionKey == null) continue;

                        if (name.StartsWith("v4", StringComparison.Ordinal))
                            // Version 4 and 4.5
                            AddDotNetFourFrameworkVersions(versionKey);
                        else if (CheckInstallDword(versionKey))
                            // Versions 1.1 through 3.5
                            AddDotNetFramework(new Version(name.Substring(1)));
                    }
                }
            }
        }

        private static void FindExtremelyOldDotNetFrameworkVersions()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\.NETFramework\policy\v1.0");
            if (key != null)
                foreach (string build in key.GetValueNames())
                    _availableFrameworks.Add(new RuntimeFramework(RuntimeType.Net, new Version("1.0." + build)));
        }

        // Note: this method cannot be generalized past V4, because (a)  it has
        // specific code for detecting .NET 4.5 and (b) we don't know what
        // microsoft will do in the future
        private static void AddDotNetFourFrameworkVersions(RegistryKey versionKey)
        {
            foreach (string profile in new string[] { "Full", "Client" })
            {
                var profileKey = versionKey.OpenSubKey(profile);
                if (profileKey == null) continue;
                
                if (CheckInstallDword(profileKey))
                {
                    AddDotNetFramework(new Version(4, 0), profile);

                    var release = (int)profileKey.GetValue("Release", 0);
                    if (release > 0) // TODO: Other higher versions?
                        AddDotNetFramework(new Version(4, 5));

                    return;     //If full profile found, return and don't check for client profile
                }
            }
        }

        private static void AddDotNetFramework(Version version)
        {
            _availableFrameworks.Add(new RuntimeFramework(RuntimeType.Net, version));
        }

        private static void AddDotNetFramework(Version version, string profile)
        {
            _availableFrameworks.Add(new RuntimeFramework(RuntimeType.Net, version, profile));
        }

        private static bool CheckInstallDword(RegistryKey key)
        {
            return ((int)key.GetValue("Install", 0) == 1);
        }

        #endregion
    }
}
