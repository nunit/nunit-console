// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnit.Engine
{
    /// <summary>
    /// Base Class for all package Settings
    /// </summary>
    public abstract class PackageSetting
    {
        /// <summary>
        /// Construct a PackageSetting
        /// </summary>
        /// <param name="name">The name of this setting.</param>
        /// <param name="value">The value of the setting</param>
        protected PackageSetting(string name, object value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Gets the name of this setting.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the value of this setting.
        /// </summary>
        public object Value { get; }

        #region Static Setting Definitions

        /// <summary>
        /// The name of the config to use in loading a project.
        /// If not specified, the first config found is used.
        /// </summary>
        public static readonly SettingDefinition<string> ActiveConfig = new SettingDefinition<string>("ActiveConfig");

        /// <summary>
        /// Bool indicating whether the engine should determine the private
        /// bin path by examining the paths to all the tests. Defaults to
        /// true unless PrivateBinPath is specified.
        /// </summary>
        public static readonly SettingDefinition<bool> AutoBinPath = new SettingDefinition<bool>("AutoBinPath");

        /// <summary>
        /// The ApplicationBase to use in loading the tests. If not
        /// specified, and each assembly has its own process, then the
        /// location of the assembly is used. For multiple  assemblies
        /// in a single process, the closest common root directory is used.
        /// </summary>
        public static readonly SettingDefinition<string> BasePath = new SettingDefinition<string>("BasePath");

        /// <summary>
        /// Path to the config file to use in running the tests.
        /// </summary>
        public static readonly SettingDefinition<string> ConfigurationFile = new SettingDefinition<string>("ConfigurationFile");

        /// <summary>
        /// Flag (bool) indicating whether tests are being debugged.
        /// </summary>
        public static readonly SettingDefinition<bool> DebugTests = new SettingDefinition<bool>("DebugTests");

        /// <summary>
        /// Bool flag indicating whether a debugger should be launched at agent
        /// startup. Used only for debugging NUnit itself.
        /// </summary>
        public static readonly SettingDefinition<bool> DebugAgent = new SettingDefinition<bool>("DebugAgent");

        /// <summary>
        /// The private binpath used to locate assemblies. Directory paths
        /// is separated by a semicolon. It's an error to specify this and
        /// also set AutoBinPath to true.
        /// </summary>
        public static readonly SettingDefinition<string> PrivateBinPath = new SettingDefinition<string>("PrivateBinPath");

        /// <summary>
        /// The maximum number of test agents permitted to run simultaneously.
        /// Ignored if the ProcessModel is not set or defaulted to Multiple.
        /// </summary>
        public static readonly SettingDefinition<int> MaxAgents = new SettingDefinition<int>("MaxAgents");

        /// <summary>
        /// Indicates the desired runtime to use for the tests. Values
        /// are strings like "net-4.5", "mono-4.0", etc. Default is to
        /// use the target framework for which an assembly was built.
        /// </summary>
        public static readonly SettingDefinition<string> RequestedRuntimeFramework = new SettingDefinition<string>("RequestedRuntimeFramework");

        /// <summary>
        /// Indicates the desired runtime to use for the tests. Values
        /// are strings like "net-4.5", "mono-4.0", etc. Default is to
        /// use the target framework for which an assembly was built.
        /// </summary>
        public static readonly SettingDefinition<string> RequestedFrameworkName = new SettingDefinition<string>("RequestedFrameworkName");

        /// <summary>
        /// Indicates the Target runtime selected for use by the engine,
        /// based on the requested runtime and assembly metadata.
        /// </summary>
        public static readonly SettingDefinition<string> TargetFrameworkName = new SettingDefinition<string>("TargetFrameworkName");

        /// <summary>
        /// Indicates the name of the agent requested by the user.
        /// </summary>
        public static readonly SettingDefinition<string> RequestedAgentName = new SettingDefinition<string>("RequestedAgentName");

        /// <summary>
        /// Indicates the name of the agent that was actually used.
        /// </summary>
        public static readonly SettingDefinition<string> SelectedAgentName = new SettingDefinition<string>("SelectedAgentName");

        /// <summary>
        /// Bool flag indicating that the test should be run in a 32-bit process
        /// on a 64-bit system. By default, NUNit runs in a 64-bit process on
        /// a 64-bit system. Ignored if set on a 32-bit system.
        /// </summary>
        public static readonly SettingDefinition<bool> RunAsX86 = new SettingDefinition<bool>("RunAsX86");

        /// <summary>
        /// Indicates that test runners should be disposed after the tests are executed
        /// </summary>
        public static readonly SettingDefinition<bool> DisposeRunners = new SettingDefinition<bool>("DisposeRunners");

        /// <summary>
        /// Bool flag indicating that the test assemblies should be shadow copied.
        /// Defaults to false.
        /// </summary>
        public static readonly SettingDefinition<bool> ShadowCopyFiles = new SettingDefinition<bool>("ShadowCopyFiles");

        /// <summary>
        /// Bool flag indicating that user profile should be loaded on test runner processes
        /// </summary>
        public static readonly SettingDefinition<bool> LoadUserProfile = new SettingDefinition<bool>("LoadUserProfile");

        /// <summary>
        /// Bool flag indicating that non-test assemblies should be skipped without error.
        /// </summary>
        public static readonly SettingDefinition<bool> SkipNonTestAssemblies = new SettingDefinition<bool>("SkipNonTestAssemblies");

        /// <summary>
        /// Flag (bool) indicating whether to pause execution of tests to allow
        /// the user to attach a debugger.
        /// </summary>
        public static readonly SettingDefinition<bool> PauseBeforeRun = new SettingDefinition<bool>("PauseBeforeRun");

        /// <summary>
        /// The InternalTraceLevel for this run. Values are: "Default",
        /// "Off", "Error", "Warning", "Info", "Debug", "Verbose".
        /// Default is "Off". "Debug" and "Verbose" are synonyms.
        /// </summary>
        public static readonly SettingDefinition<string> InternalTraceLevel = new SettingDefinition<string>("InternalTraceLevel");

        /// <summary>
        /// The PrincipalPolicy to set on the test application domain. Values are:
        /// "UnauthenticatedPrincipal", "NoPrincipal" and "WindowsPrincipal".
        /// </summary>
        public static readonly SettingDefinition<string> PrincipalPolicy = new SettingDefinition<string>("PrincipalPolicy");

        /// <summary>
        /// Full path of the directory to be used for work and result files.
        /// This path is provided to tests by the framework TestContext.
        /// </summary>
        public static readonly SettingDefinition<string> WorkDirectory = new SettingDefinition<string>("WorkDirectory");

        /// <summary>
        /// If the package represents an assembly, then this is the CLR version
        /// stored in the assembly image. If it represents a project or other
        /// group of assemblies, it is the maximum version for all the assemblies.
        /// </summary>
        public static readonly SettingDefinition<string> ImageRuntimeVersion = new SettingDefinition<string>("ImageRuntimeVersion");

        /// <summary>
        /// True if any assembly in the package requires running as a 32-bit
        /// process when on a 64-bit system.
        /// </summary>
        public static readonly SettingDefinition<bool> ImageRequiresX86 = new SettingDefinition<bool>("ImageRequiresX86");

        /// <summary>
        /// True if any assembly in the package requires a special assembly resolution hook
        /// in the default application domain in order to find dependent assemblies.
        /// </summary>
        public static readonly SettingDefinition<bool> ImageRequiresDefaultAppDomainAssemblyResolver = new SettingDefinition<bool>("ImageRequiresDefaultAppDomainAssemblyResolver");

        /// <summary>
        /// The FrameworkName specified on a TargetFrameworkAttribute for the assembly
        /// </summary>
        public static readonly SettingDefinition<string> ImageTargetFrameworkName = new SettingDefinition<string>("ImageTargetFrameworkName");

        #endregion

        #region Nested Setting Definition Class

        /// <summary>
        /// Nested SettingDefinition class is used as a sort of template for creating settings.
        /// It specifies the name of the setting and the Type required for its value but not
        /// the value itself. Its WithValue method is used to generate an actual PackageSetting.
        /// </summary>
        public class SettingDefinition<T>
        {
            /// <summary>
            /// Construct a SettingDefinition
            /// </summary>
            /// <param name="name">Name of this setting</param>
            public SettingDefinition(string name)
            {
                Name = name;
            }

            /// <summary>
            /// Gets the name of this setting.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Create a PackageSetting based on this definition.
            /// </summary>
            /// <param name="value">The value to assign the setting.</param>
            /// <returns>A PackageSetting.</returns>
            public PackageSetting<object> WithValue(object value)
            {
                return new PackageSetting<object>(Name, value);
            }

            /// <summary>
            /// Create a PackageSetting based on this definition.
            /// </summary>
            /// <param name="value">The value to assign the setting.</param>
            /// <returns>A PackageSetting.</returns>
            public PackageSetting<string> WithValue(string value)
            {
                return new PackageSetting<string>(Name, value);
            }

            /// <summary>
            /// Create a PackageSetting based on this definition.
            /// </summary>
            /// <param name="value">The value to assign the setting.</param>
            /// <returns>A PackageSetting.</returns>
            public PackageSetting<int> WithValue(int value)
            {
                return new PackageSetting<int>(Name, value);
            }

            /// <summary>
            /// Create a PackageSetting based on this definition.
            /// </summary>
            /// <param name="value">The value to assign the setting.</param>
            /// <returns>A PackageSetting.</returns>
            public PackageSetting<bool> WithValue(bool value)
            {
                return new PackageSetting<bool>(Name, value);
            }
        }

        #endregion
    }

    /// <summary>
    /// The PackageSetting class represents one setting value contained in a
    /// TestPackage. Setting values are constrained to be string, int or bool.
    /// Instances of PackageSetting are immutable.
    /// </summary>
    public class PackageSetting<T> : PackageSetting
        where T : notnull
    {
        /// <summary>
        /// Construct a PackageSetting with a string value
        /// </summary>
        /// <param name="name">The setting name.</param>
        /// <param name="value">The value of this setting instance.</param>
        public PackageSetting(string name, T value) : base(name, value)
        {
        }

        ///// <summary>
        ///// Construct a PackageSetting with a bool value
        ///// </summary>
        ///// <param name="name">The setting name.</param>
        ///// <param name="value">The value of this setting instance.</param>
        //public PackageSetting(string name, bool value)
        //{
        //    Name = name;
        //    Value = value;
        //}

        ///// <summary>
        ///// Construct a PackageSetting with an int value
        ///// </summary>
        ///// <param name="name">The setting name.</param>
        ///// <param name="value">The value of this setting instance.</param>
        //public PackageSetting(string name, int value)
        //{
        //    Name = name;
        //    Value = value;
        //}

        ///// <summary>
        ///// Construct a PackageSetting with a value of any Type
        ///// </summary>
        ///// <param name="name">The setting name.</param>
        ///// <param name="value">The value of this setting instance.</param>
        ///// <remarks>
        ///// Used internally to support settings of other types required by
        ///// specific frameworks. Should not be used for any engine settings.
        ///// </remarks>
        //public PackageSetting(string name, object value)
        //{
        //    Name = name;
        //    Value = value;
        //}

        ///// <summary>
        ///// Get the setting value as an object.
        ///// </summary>
        //public T Value { get; }

        //public T GetSettingOrDefault<T>(T defaultValue)
        //    where T : notnull
        //{
        //    if (Value is T)
        //        return (T)Value;

        //    string? s = Value as string;
        //    if (s is not null)
        //        if (typeof(T) == typeof(int))
        //            return int.Parse(s);
        //}
    }
}
