// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Runtime.Versioning;

namespace NUnit
{
    /// <summary>
    /// EngineSettings contains constant values used as keys in setting up a TestPackage.
    /// The required value type is indicated as a comment after each definition.
    /// </summary>
    public static class EnginePackageSettings
    {
        #region Values Set by the Runner

        /// <summary>
        /// The name of the config to use in loading a project.
        /// If not specified, the first config found is used.
        /// </summary>
        public const string ActiveConfig = nameof(ActiveConfig); // TYPE: string

        /// <summary>
        /// Bool indicating whether the engine should determine the private
        /// bin path by examining the paths to all the tests. Defaults to
        /// true unless PrivateBinPath is specified.
        /// </summary>
        public const string AutoBinPath = nameof(AutoBinPath); // TYPE: bool

        /// <summary>
        /// The ApplicationBase to use in loading the tests. If not
        /// specified, and each assembly has its own process, then the
        /// location of the assembly is used. For multiple  assemblies
        /// in a single process, the closest common root directory is used.
        /// </summary>
        public const string BasePath = nameof(BasePath); // TYPE: string

        /// <summary>
        /// Path to the config file to use in running the tests.
        /// </summary>
        public const string ConfigurationFile = nameof(ConfigurationFile); // TYPE: string

        /// <summary>
        /// Flag (bool) indicating whether tests are being debugged.
        /// </summary>
        public const string DebugTests = nameof(DebugTests); // TYPE: bool

        /// <summary>
        /// Bool flag indicating whether a debugger should be launched at agent
        /// startup. Used only for debugging NUnit itself.
        /// </summary>
        public const string DebugAgent = nameof(DebugAgent); // TYPE: bool

        /// <summary>
        /// The private binpath used to locate assemblies. Directory paths
        /// is separated by a semicolon. It's an error to specify this and
        /// also set AutoBinPath to true.
        /// </summary>
        public const string PrivateBinPath = nameof(PrivateBinPath); // TYPE: string

        /// <summary>
        /// The maximum number of test agents permitted to run simultaneously.
        /// Ignored if the ProcessModel is not set or defaulted to Multiple.
        /// </summary>
        public const string MaxAgents = nameof(MaxAgents); // TYPE: int

        /// <summary>
        /// Indicates the desired runtime to use for the tests. Values
        /// are strings like nameof(net-4.5", "mono-4.0", etc. Default is to
        /// use the target framework for which an assembly was built.
        /// </summary>
        public const string RequestedRuntimeFramework = nameof(RequestedRuntimeFramework); // TYPE: string

        /// <summary>
        /// Indicates the desired runtime to use for the tests. Values
        /// are strings like "net-4.5", "mono-4.0", etc. Default is to
        /// use the target framework for which an assembly was built.
        /// </summary>
        public const string RequestedFrameworkName = nameof(RequestedFrameworkName); // TYPE: string

        /// <summary>
        /// Indicates the Target runtime selected for use by the engine,
        /// based on the requested runtime and assembly metadata.
        /// </summary>
        public const string TargetFrameworkName = nameof(TargetFrameworkName); // TYPE: string

        /// <summary>
        /// Indicates the name of the agent requested by the user.
        /// </summary>
        public const string RequestedAgentName = nameof(RequestedAgentName); // TYPE: string

        /// <summary>
        /// Indicates the name of the agent that was actually used.
        /// </summary>
        public const string SelectedAgentName = nameof(SelectedAgentName); // TYPE: string

        /// <summary>
        /// Bool flag indicating that the test should be run in a 32-bit process
        /// on a 64-bit system. By default, NUNit runs in a 64-bit process on
        /// a 64-bit system. Ignored if set on a 32-bit system.
        /// </summary>
        public const string RunAsX86 = nameof(RunAsX86); // TYPE: bool

        /// <summary>
        /// Indicates that test runners should be disposed after the tests are executed
        /// </summary>
        public const string DisposeRunners = nameof(DisposeRunners); // TYPE: bool

        /// <summary>
        /// Bool flag indicating that the test assemblies should be shadow copied.
        /// Defaults to false.
        /// </summary>
        public const string ShadowCopyFiles = nameof(ShadowCopyFiles); // TYPE: bool

        /// <summary>
        /// Bool flag indicating that user profile should be loaded on test runner processes
        /// </summary>
        public const string LoadUserProfile = nameof(LoadUserProfile); // TYPE: bool

        /// <summary>
        /// Bool flag indicating that non-test assemblies should be skipped without error.
        /// </summary>
        public const string SkipNonTestAssemblies = nameof(SkipNonTestAssemblies); // TYPE: bool

        /// <summary>
        /// Flag (bool) indicating whether to pause execution of tests to allow
        /// the user to attach a debugger.
        /// </summary>
        public const string PauseBeforeRun = nameof(PauseBeforeRun); // TYPE: bool

        /// <summary>
        /// The InternalTraceLevel for this run. Values are: "Default",
        /// "Off", "Error", "Warning", "Info", "Debug", "Verbose".
        /// Default is "Off". "Debug" and "Verbose" are synonyms.
        /// </summary>
        public const string InternalTraceLevel = nameof(InternalTraceLevel); // TYPE: string

        /// <summary>
        /// The PrincipalPolicy to set on the test application domain. Values are:
        /// "UnauthenticatedPrincipal", "NoPrincipal" and "WindowsPrincipal".
        /// </summary>
        public const string PrincipalPolicy = nameof(PrincipalPolicy); // TYPE: string

        /// <summary>
        /// Full path of the directory to be used for work and result files.
        /// This path is provided to tests by the framework TestContext.
        /// </summary>
        public const string WorkDirectory = nameof(WorkDirectory); // TYPE: string

        #endregion

        #region Values Set and Used Within the Engine Itself

        /// <summary>
        /// If the package represents an assembly, then this is the CLR version
        /// stored in the assembly image. If it represents a project or other
        /// group of assemblies, it is the maximum version for all the assemblies.
        /// </summary>
        public const string ImageRuntimeVersion = nameof(ImageRuntimeVersion); // TYPE: string

        /// <summary>
        /// True if any assembly in the package requires running as a 32-bit
        /// process when on a 64-bit system.
        /// </summary>
        public const string ImageRequiresX86 = nameof(ImageRequiresX86); // TYPE: bool

        /// <summary>
        /// True if any assembly in the package requires a special assembly resolution hook
        /// in the default application domain in order to find dependent assemblies.
        /// </summary>
        public const string ImageRequiresDefaultAppDomainAssemblyResolver = nameof(ImageRequiresDefaultAppDomainAssemblyResolver); // TYPE: bool

        /// <summary>
        /// The FrameworkName specified on a TargetFrameworkAttribute for the assembly
        /// </summary>
        public const string ImageTargetFrameworkName = nameof(ImageTargetFrameworkName); // TYPE: string

        #endregion
    }
}
