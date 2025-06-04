// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine;
using System.Collections.Generic;
using System.IO;

namespace NUnit.Common
{
    /// <summary>
    /// Static class holding SettingDefinitions for all standard package settings
    /// known to the runner and engine. Except for internal settings with names
    /// starting "Image...", the runner creates the settings. Some settings are
    /// used by the engine, some by the framework and some by both.
    /// </summary>
    /// <remarks>
    /// Although 'SettingDefinitions' might be a better descriptive name for the class,
    /// 'PackageSettings' seems to work slightly better frp, a usage point of view.
    /// </remarks>
    public static class PackageSettings
    {
        #region Settings Used by the Engine

        /// <summary>
        /// The name of the config to use in loading a project.
        /// If not specified, the first config found is used.
        /// </summary>
        public static SettingDefinition ActiveConfig => new SettingDefinition<string>(nameof(ActiveConfig));

        /// <summary>
        /// Bool indicating whether the engine should determine the private
        /// bin path by examining the paths to all the tests. Defaults to
        /// true unless PrivateBinPath is specified.
        /// </summary>
        public static SettingDefinition AutoBinPath => new SettingDefinition<bool>(nameof(AutoBinPath));

        /// <summary>
        /// The ApplicationBase to use in loading the tests. If not
        /// specified, and each assembly has its own process, then the
        /// location of the assembly is used. For multiple  assemblies
        /// in a single process, the closest common root directory is used.
        /// </summary>
        public static SettingDefinition BasePath => new SettingDefinition<string>(nameof(BasePath));

        /// <summary>
        /// Path to the config file to use in running the tests.
        /// </summary>
        public static SettingDefinition ConfigurationFile => new SettingDefinition<string>(nameof(ConfigurationFile));

        ///// <summary>
        ///// Flag (bool) indicating whether tests are being debugged.
        ///// </summary>
        //public static SettingDefinition DebugTests => new SettingDefinition<bool>(nameof(DebugTests));

        /// <summary>
        /// Bool flag indicating whether a debugger should be launched at agent
        /// startup. Used only for debugging NUnit itself.
        /// </summary>
        public static SettingDefinition DebugAgent => new SettingDefinition<bool>(nameof(DebugAgent));

        /// <summary>
        /// The private binpath used to locate assemblies. Directory paths
        /// is separated by a semicolon. It's an error to specify this and
        /// also set AutoBinPath to true.
        /// </summary>
        public static SettingDefinition PrivateBinPath => new SettingDefinition<string>(nameof(PrivateBinPath));

        /// <summary>
        /// The maximum number of test agents permitted to run simultaneously.
        /// Ignored if the ProcessModel is not set or defaulted to Multiple.
        /// </summary>
        public static SettingDefinition MaxAgents => new SettingDefinition<int>(nameof(MaxAgents));

        /// <summary>
        /// Indicates the desired runtime to use for the tests. Values
        /// are strings like "net-4.5", "mono-4.0", etc. Default is to
        /// use the target framework for which an assembly was built.
        /// </summary>
        public static SettingDefinition RequestedRuntimeFramework => new SettingDefinition<string>(nameof(RequestedRuntimeFramework));

        /// <summary>
        /// Indicates the desired runtime to use for the tests. Values
        /// are strings like "net-4.5", "mono-4.0", etc. Default is to
        /// use the target framework for which an assembly was built.
        /// </summary>
        public static SettingDefinition RequestedFrameworkName => new SettingDefinition<string>(nameof(RequestedFrameworkName));

        /// <summary>
        /// Indicates the Target runtime selected for use by the engine,
        /// based on the requested runtime and assembly metadata.
        /// </summary>
        public static SettingDefinition TargetFrameworkName => new SettingDefinition<string>(nameof(TargetFrameworkName));

        /// <summary>
        /// Indicates the name of the agent requested by the user.
        /// </summary>
        public static SettingDefinition RequestedAgentName => new SettingDefinition<string>(nameof(RequestedAgentName));

        /// <summary>
        /// Indicates the name of the agent that was actually used.
        /// </summary>
        public static SettingDefinition SelectedAgentName => new SettingDefinition<string>(nameof(SelectedAgentName));

        /// <summary>
        /// Bool flag indicating that the test should be run in a 32-bit process
        /// on a 64-bit system. By default, NUNit runs in a 64-bit process on
        /// a 64-bit system. Ignored if set on a 32-bit system.
        /// </summary>
        public static SettingDefinition RunAsX86 => new SettingDefinition<bool>(nameof(RunAsX86));

        /// <summary>
        /// Indicates that test runners should be disposed after the tests are executed
        /// </summary>
        public static SettingDefinition DisposeRunners => new SettingDefinition<bool>(nameof(DisposeRunners));

        /// <summary>
        /// Bool flag indicating that the test assemblies should be shadow copied.
        /// Defaults to false.
        /// </summary>
        public static SettingDefinition ShadowCopyFiles => new SettingDefinition<bool>(nameof(ShadowCopyFiles));

        /// <summary>
        /// Bool flag indicating that user profile should be loaded on test runner processes
        /// </summary>
        public static SettingDefinition LoadUserProfile => new SettingDefinition<bool>(nameof(LoadUserProfile));

        /// <summary>
        /// Bool flag indicating that non-test assemblies should be skipped without error.
        /// </summary>
        public static SettingDefinition SkipNonTestAssemblies => new SettingDefinition<bool>(nameof(SkipNonTestAssemblies));

        ///// <summary>
        ///// Flag (bool) indicating whether to pause execution of tests to allow
        ///// the user to attach a debugger.
        ///// </summary>
        //public static SettingDefinition PauseBeforeRun => new SettingDefinition<bool>(nameof(PauseBeforeRun));

        ///// <summary>
        ///// The InternalTraceLevel for this run. Values are: "Default",
        ///// "Off", "Error", "Warning", "Info", "Debug", "Verbose".
        ///// Default is "Off". "Debug" and "Verbose" are synonyms.
        ///// </summary>
        //public static SettingDefinition InternalTraceLevel => new SettingDefinition<string>(nameof(InternalTraceLevel));

        /// <summary>
        /// The PrincipalPolicy to set on the test application domain. Values are:
        /// "UnauthenticatedPrincipal", "NoPrincipal" and "WindowsPrincipal".
        /// </summary>
        public static SettingDefinition PrincipalPolicy => new SettingDefinition<string>(nameof(PrincipalPolicy));

        ///// <summary>
        ///// Full path of the directory to be used for work and result files.
        ///// This path is provided to tests by the framework TestContext.
        ///// </summary>
        //public static SettingDefinition WorkDirectory => new SettingDefinition<string>(nameof(WorkDirectory));

        #endregion

        #region Settings Used by both the Engine and the Framework

        /// <summary>
        /// Flag (bool) indicating whether tests are being debugged.
        /// </summary>
        public static SettingDefinition DebugTests =>
            new SettingDefinition<bool>(FrameworkPackageSettings.DebugTests);

        /// <summary>
        /// Flag (bool) indicating whether to pause execution of tests to allow
        /// the user to attach a debugger.
        /// </summary>
        public static SettingDefinition PauseBeforeRun =>
            new SettingDefinition<bool>(FrameworkPackageSettings.PauseBeforeRun);

        /// <summary>
        /// The InternalTraceLevel for this run. Values are: "Default",
        /// "Off", "Error", "Warning", "Info", "Debug", "Verbose".
        /// Default is "Off". "Debug" and "Verbose" are synonyms.
        /// </summary>
        public static SettingDefinition<string> InternalTraceLevel =>
            new SettingDefinition<string>(FrameworkPackageSettings.InternalTraceLevel);

        /// <summary>
        /// Full path of the directory to be used for work and result files.
        /// This path is provided to tests by the framework TestContext.
        /// </summary>
        public static SettingDefinition<string> WorkDirectory =>
            new SettingDefinition<string>(FrameworkPackageSettings.WorkDirectory);

        #endregion

        #region Internal Settings Used Only within the Engine

        /// <summary>
        /// If the package represents an assembly, then this is the CLR version
        /// stored in the assembly image. If it represents a project or other
        /// group of assemblies, it is the maximum version for all the assemblies.
        /// </summary>
        public static SettingDefinition ImageRuntimeVersion => new SettingDefinition<string>(nameof(ImageRuntimeVersion));

        /// <summary>
        /// True if any assembly in the package requires running as a 32-bit
        /// process when on a 64-bit system.
        /// </summary>
        public static SettingDefinition ImageRequiresX86 => new SettingDefinition<bool>(nameof(ImageRequiresX86));

        /// <summary>
        /// True if any assembly in the package requires a special assembly resolution hook
        /// in the default application domain in order to find dependent assemblies.
        /// </summary>
        public static SettingDefinition ImageRequiresDefaultAppDomainAssemblyResolver => new SettingDefinition<bool>(nameof(ImageRequiresDefaultAppDomainAssemblyResolver));

        /// <summary>
        /// The FrameworkName specified on a TargetFrameworkAttribute for the assembly
        /// </summary>
        public static SettingDefinition ImageTargetFrameworkName => new SettingDefinition<string>(nameof(ImageTargetFrameworkName));

        #endregion

        #region Settings Used by the NUnit Framework

        /// <summary>
        /// Integer value in milliseconds for the default timeout value
        /// for test cases. If not specified, there is no timeout except
        /// as specified by attributes on the tests themselves.
        /// </summary>
        public static SettingDefinition<int> DefaultTimeout =>
            new SettingDefinition<int>(FrameworkPackageSettings.DefaultTimeout);

        /// <summary>
        /// A string representing the default thread culture to be used for
        /// running tests. String should be a valid BCP-47 culture name. If
        /// culture is unset, tests run on the machine's default culture.
        /// </summary>
        public static SettingDefinition<string> DefaultCulture =>
            new SettingDefinition<string>(FrameworkPackageSettings.DefaultCulture);

        /// <summary>
        /// A string representing the default thread UI culture to be used for
        /// running tests. String should be a valid BCP-47 culture name. If
        /// culture is unset, tests run on the machine's default culture.
        /// </summary>
        public static SettingDefinition<string> DefaultUICulture =>
            new SettingDefinition<string>(FrameworkPackageSettings.DefaultUICulture);

        /// <summary>
        /// A TextWriter to which the internal trace will be sent.
        /// </summary>
        public static SettingDefinition<TextWriter> InternalTraceWriter =>
            new SettingDefinition<TextWriter>(FrameworkPackageSettings.InternalTraceWriter);

        /// <summary>
        /// A list of tests to be loaded.
        /// </summary>
        public static SettingDefinition<IList<string>> LOAD =>
            new SettingDefinition<IList<string>>(FrameworkPackageSettings.LOAD);

        /// <summary>
        /// The number of test threads to run for the assembly. If set to
        /// 1, a single queue is used. If set to 0, tests are executed
        /// directly, without queuing.
        /// </summary>
        public static SettingDefinition<int> NumberOfTestWorkers =>
            new SettingDefinition<int>(FrameworkPackageSettings.NumberOfTestWorkers);

        /// <summary>
        /// The random seed to be used for this assembly. If specified
        /// as the value reported from a prior run, the framework should
        /// generate identical random values for tests as were used for
        /// that run, provided that no change has been made to the test
        /// assembly. Default is a random value itself.
        /// </summary>
        public static SettingDefinition<int> RandomSeed =>
            new SettingDefinition<int>(FrameworkPackageSettings.RandomSeed);

        /// <summary>
        /// If true, execution stops after the first error or failure.
        /// </summary>
        public static SettingDefinition StopOnError =>
            new SettingDefinition<bool>(FrameworkPackageSettings.StopOnError);

        /// <summary>
        /// If true, asserts in multiple asserts block will throw first-chance exception on failure.
        /// </summary>
        public static SettingDefinition ThrowOnEachFailureUnderDebugger =>
            new SettingDefinition<bool>(FrameworkPackageSettings.ThrowOnEachFailureUnderDebugger);

        /// <summary>
        /// If true, use of the event queue is suppressed and test events are synchronous.
        /// </summary>
        public static SettingDefinition SynchronousEvents =>
            new SettingDefinition<bool>(FrameworkPackageSettings.SynchronousEvents);

        /// <summary>
        /// The default naming pattern used in generating test names
        /// </summary>
        public static SettingDefinition<string> DefaultTestNamePattern =>
            new SettingDefinition<string>(FrameworkPackageSettings.DefaultTestNamePattern);

        /// <summary>
        /// Parameters to be passed on to the tests, serialized to a single string which needs parsing. Obsoleted by <see cref=>"TestParametersDictionary"/>; kept for backward compatibility.
        /// </summary>
        public static SettingDefinition<string> TestParameters =>
            new SettingDefinition<string>(FrameworkPackageSettings.TestParameters);

        /// <summary>
        /// If true, the tests will run on the same thread as the NUnit runner itself
        /// </summary>
        public static SettingDefinition RunOnMainThread =>
            new SettingDefinition<bool>(FrameworkPackageSettings.RunOnMainThread);

        /// <summary>
        /// Parameters to be passed on to the tests, already parsed into an IDictionary&lt;string, string>. Replaces <see cref=>"TestParameters"/>.
        /// </summary>
        public static SettingDefinition TestParametersDictionary =>
            new SettingDefinition<IDictionary<string, string>>(FrameworkPackageSettings.TestParametersDictionary);

        #endregion
    }
}
