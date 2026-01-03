// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace NUnit.Common
{
    /// <summary>
    /// Static class holding SettingDefinitions for all standard package settings
    /// known to the runner and engine. Except for internal settings with names
    /// starting "Image...", the runner creates the settings. Some settings are
    /// used by the engine, some by the framework and some by both.
    /// </summary>
    public static class SettingDefinitions
    {
        private static readonly Dictionary<string, SettingDefinition> _knownSettings;

        static SettingDefinitions()
        {
            // Use Reflection to build a dictionary of all known setting definitions
            var properties = typeof(SettingDefinitions).GetProperties(BindingFlags.Public | BindingFlags.Static);

            _knownSettings = new Dictionary<string, SettingDefinition>();

            foreach (var property in properties)
            {
                var propValue = property.GetValue(null, null) as SettingDefinition;
                if (propValue is not null)
                    _knownSettings.Add(property.Name, propValue);
            }
        }

        /// <summary>
        /// Lookup a known SettingDefinition by name
        /// </summary>
        /// <param name="name">The name of the setting</param>
        /// <returns>A SettingDefinition if there is one, otherwise null.</returns>
        public static SettingDefinition? Lookup(string name)
        {
            if (_knownSettings.TryGetValue(name, out var definition))
                return definition;
            return null;
        }

        #region Settings Used by the Engine

        /// <summary>
        /// The name of the config to use in loading a project.
        /// If not specified, the first config found is used.
        /// </summary>
        public static SettingDefinition<string> ActiveConfig { get; } = new(nameof(ActiveConfig), string.Empty);

        /// <summary>
        /// Bool indicating whether the engine should determine the private
        /// bin path by examining the paths to all the tests. Defaults to
        /// true unless PrivateBinPath is specified.
        /// </summary>
        public static SettingDefinition<bool> AutoBinPath { get; } = new(nameof(AutoBinPath), false);

        /// <summary>
        /// The ApplicationBase to use in loading the tests. If not
        /// specified, and each assembly has its own process, then the
        /// location of the assembly is used. For multiple  assemblies
        /// in a single process, the closest common root directory is used.
        /// </summary>
        public static SettingDefinition<string> BasePath { get; } = new(nameof(BasePath), string.Empty);

        /// <summary>
        /// Path to the config file to use in running the tests.
        /// </summary>
        public static SettingDefinition<string> ConfigurationFile { get; } = new(nameof(ConfigurationFile), string.Empty);

        /// <summary>
        /// A list of the available configs for this project.
        /// </summary>
        public static SettingDefinition<string> ConfigNames { get; } = new(nameof(ConfigNames), string.Empty);

        ///// <summary>
        ///// Flag (bool) indicating whether tests are being debugged.
        ///// </summary>
        //public static SettingDefinition<bool> DebugTests { get; } = new(nameof(DebugTests));

        /// <summary>
        /// Bool flag indicating whether a debugger should be launched at agent
        /// startup. Used only for debugging NUnit itself.
        /// </summary>
        public static SettingDefinition<bool> DebugAgent { get; } = new(nameof(DebugAgent), false);

        /// <summary>
        /// Bool flag indicating whether a debugger should be launched at console
        /// startup. Used only for debugging NUnit itself.
        /// </summary>
        public static SettingDefinition<bool> DebugConsole { get; } = new(nameof(DebugConsole), false);

        /// <summary>
        /// The private binpath used to locate assemblies. Directory paths
        /// is separated by a semicolon. It's an error to specify this and
        /// also set AutoBinPath to true.
        /// </summary>
        public static SettingDefinition<string> PrivateBinPath { get; } = new(nameof(PrivateBinPath), string.Empty);

        /// <summary>
        /// The maximum number of test agents permitted to run simultaneously.
        /// </summary>
        public static SettingDefinition<int> MaxAgents { get; } = new(nameof(MaxAgents), 0);

        /// <summary>
        /// Indicates the desired runtime to use for the tests. Values
        /// are strings like "net-4.5", "mono-4.0", etc. Default is to
        /// use the target framework for which an assembly was built.
        /// </summary>
        public static SettingDefinition<string> RequestedRuntimeFramework { get; } = new(nameof(RequestedRuntimeFramework), string.Empty);

        /// <summary>
        /// Indicates the desired runtime to use for the tests. Values
        /// are strings like "net-4.5", "mono-4.0", etc. Default is to
        /// use the target framework for which an assembly was built.
        /// </summary>
        public static SettingDefinition<string> RequestedFrameworkName { get; } = new(nameof(RequestedFrameworkName), string.Empty);

        /// <summary>
        /// Indicates the Target runtime selected for use by the engine,
        /// based on the requested runtime and assembly metadata.
        /// </summary>
        public static SettingDefinition<string> TargetFrameworkName { get; } = new(nameof(TargetFrameworkName), string.Empty);

        /// <summary>
        /// Indicates the name of the agent requested by the user.
        /// </summary>
        public static SettingDefinition<string> RequestedAgentName { get; } = new(nameof(RequestedAgentName), string.Empty);

        /// <summary>
        /// Indicates the name of the agent that was actually used.
        /// </summary>
        public static SettingDefinition<string> SelectedAgentName { get; } = new(nameof(SelectedAgentName), string.Empty);

        /// <summary>
        /// Bool flag indicating that the test should be run in a 32-bit process
        /// on a 64-bit system. By default, NUNit runs in a 64-bit process on
        /// a 64-bit system. Ignored if set on a 32-bit system.
        /// </summary>
        public static SettingDefinition<bool> RunAsX86 { get; } = new(nameof(RunAsX86), false);

        /// <summary>
        /// Indicates that test runners should be disposed after the tests are executed
        /// </summary>
        public static SettingDefinition<bool> DisposeRunners { get; } = new(nameof(DisposeRunners), false);

        /// <summary>
        /// Bool flag indicating that the test assemblies should be shadow copied.
        /// Defaults to false.
        /// </summary>
        public static SettingDefinition<bool> ShadowCopyFiles { get; } = new(nameof(ShadowCopyFiles), false);

        /// <summary>
        /// Bool flag indicating that user profile should be loaded on test runner processes
        /// </summary>
        public static SettingDefinition<bool> LoadUserProfile { get; } = new(nameof(LoadUserProfile), false);

        /// <summary>
        /// Bool flag indicating that non-test assemblies should be skipped without error.
        /// </summary>
        public static SettingDefinition<bool> SkipNonTestAssemblies { get; } = new(nameof(SkipNonTestAssemblies), false);

        /// <summary>
        /// Int value in milliseconds used to cancel the entire test run if it is exceeded.
        /// </summary>
        public static SettingDefinition<int> TestRunTimeout { get; } = new SettingDefinition<int>(nameof(TestRunTimeout), 0);

        ///// <summary>
        ///// Flag (bool) indicating whether to pause execution of tests to allow
        ///// the user to attach a debugger.
        ///// </summary>
        //public static SettingDefinition<bool> PauseBeforeRun { get; } = new(nameof(PauseBeforeRun));

        ///// <summary>
        ///// The InternalTraceLevel for this run. Values are: "Default",
        ///// "Off", "Error", "Warning", "Info", "Debug", "Verbose".
        ///// Default is "Off". "Debug" and "Verbose" are synonyms.
        ///// </summary>
        //public static SettingDefinition<string> InternalTraceLevel { get; } = new(nameof(InternalTraceLevel));

        /// <summary>
        /// The PrincipalPolicy to set on the test application domain. Values are:
        /// "UnauthenticatedPrincipal", "NoPrincipal" and "WindowsPrincipal".
        /// </summary>
        public static SettingDefinition<string> PrincipalPolicy { get; } = new(nameof(PrincipalPolicy), string.Empty);

        ///// <summary>
        ///// Full path of the directory to be used for work and result files.
        ///// This path is provided to tests by the framework TestContext.
        ///// </summary>
        //public static SettingDefinition<string> WorkDirectory { get; } = new(nameof(WorkDirectory));

        /// <summary>
        /// Set to true to list statistics for dependency resolution under .NET Core.
        /// </summary>
        public static SettingDefinition<bool> ListResolutionStats { get; } = new(nameof(ListResolutionStats), false);

#endregion

        #region Settings Used by both the Engine and the Framework

        /// <summary>
        /// Flag (bool) indicating whether tests are being debugged.
        /// </summary>
        public static SettingDefinition<bool> DebugTests { get; } = new(nameof(DebugTests), false);

        /// <summary>
        /// Flag (bool) indicating whether to pause execution of tests to allow
        /// the user to attach a debugger.
        /// </summary>
        public static SettingDefinition<bool> PauseBeforeRun { get; } = new(nameof(PauseBeforeRun), false);

        /// <summary>
        /// The InternalTraceLevel for this run. Values are: "Default",
        /// "Off", "Error", "Warning", "Info", "Debug", "Verbose".
        /// Default is "Off". "Debug" and "Verbose" are synonyms.
        /// </summary>
        public static SettingDefinition<string> InternalTraceLevel { get; } = new(nameof(InternalTraceLevel), "Off");

        /// <summary>
        /// Full path of the directory to be used for work and result files.
        /// This path is provided to tests by the framework TestContext.
        /// </summary>
        public static SettingDefinition<string> WorkDirectory { get; } = new(nameof(WorkDirectory), string.Empty);

        #endregion

        #region Internal Settings Used Only within the Engine

        /// <summary>
        /// If the package represents an assembly, then this is the CLR version
        /// stored in the assembly image. If it represents a project or other
        /// group of assemblies, it is the maximum version for all the assemblies.
        /// </summary>
        public static SettingDefinition<string> ImageRuntimeVersion { get; } = new(nameof(ImageRuntimeVersion), "2.0");

        /// <summary>
        /// True if any assembly in the package requires running as a 32-bit
        /// process when on a 64-bit system.
        /// </summary>
        public static SettingDefinition<bool> ImageRequiresX86 { get; } = new(nameof(ImageRequiresX86), false);

        /// <summary>
        /// True if any assembly in the package requires a special assembly resolution hook
        /// in the default application domain in order to find dependent assemblies.
        /// </summary>
        public static SettingDefinition<bool> ImageRequiresDefaultAppDomainAssemblyResolver { get; } = new(nameof(ImageRequiresDefaultAppDomainAssemblyResolver), false);

        /// <summary>
        /// The FrameworkName specified on a TargetFrameworkAttribute for the assembly
        /// </summary>
        public static SettingDefinition<string> ImageTargetFrameworkName { get; } = new(nameof(ImageTargetFrameworkName), string.Empty);

        /// <summary>
        /// Set this to true to force use of the default assembly load context for the
        /// test assembly and in resolving all dependencies rather than creating and
        /// using a separate instance of AssemblyLoadContext.
        /// </summary>
        /// <remarks>
        /// This is provided for use by the NUnit3 VS Adapter and may not work if used
        /// outside of  that context. It must be set in the top-level package via the
        /// AddSetting method so that the same value is passed to all subpackages.
        /// </remarks>
        public const string UseDefaultAssemblyLoadContext = "UseDefaultAssemblyLoadContext";

        #endregion

        #region Settings Used by the NUnit Framework

        /// <summary>
        /// Integer value in milliseconds for the default timeout value
        /// for test cases. If not specified, there is no timeout except
        /// as specified by attributes on the tests themselves.
        /// </summary>
        public static SettingDefinition<int> DefaultTimeout { get; } = new(nameof(DefaultTimeout), 0);

        /// <summary>
        /// A string representing the default thread culture to be used for
        /// running tests. String should be a valid BCP-47 culture name. If
        /// culture is unset, tests run on the machine's default culture.
        /// </summary>
        public static SettingDefinition<string> DefaultCulture { get; } = new(nameof(DefaultCulture), string.Empty);

        /// <summary>
        /// A string representing the default thread UI culture to be used for
        /// running tests. String should be a valid BCP-47 culture name. If
        /// culture is unset, tests run on the machine's default culture.
        /// </summary>
        public static SettingDefinition<string> DefaultUICulture { get; } = new(nameof(DefaultUICulture), string.Empty);

        /// <summary>
        /// A TextWriter to which the internal trace will be sent.
        /// </summary>
        public static SettingDefinition<TextWriter> InternalTraceWriter { get; } = new(nameof(InternalTraceWriter), null!);

        /// <summary>
        /// A list of tests to be loaded.
        /// </summary>
        public static SettingDefinition<IList<string>> LOAD { get; } = new(nameof(LOAD), Array.Empty<string>());

        /// <summary>
        /// The number of test threads to run for the assembly. If set to
        /// 1, a single queue is used. If set to 0, tests are executed
        /// directly, without queuing.
        /// </summary>
        public static SettingDefinition<int> NumberOfTestWorkers { get; } = new(nameof(NumberOfTestWorkers), 1);

        /// <summary>
        /// The random seed to be used for this assembly. If specified
        /// as the value reported from a prior run, the framework should
        /// generate identical random values for tests as were used for
        /// that run, provided that no change has been made to the test
        /// assembly. Default is a random value itself.
        /// </summary>
        public static SettingDefinition<int> RandomSeed { get; } = new(nameof(RandomSeed), 0);

        /// <summary>
        /// If true, execution stops after the first error or failure.
        /// </summary>
        public static SettingDefinition<bool> StopOnError { get; } = new(nameof(StopOnError), false);

        /// <summary>
        /// If true, asserts in multiple asserts block will throw first-chance exception on failure.
        /// </summary>
        public static SettingDefinition<bool> ThrowOnEachFailureUnderDebugger { get; } = new(nameof(ThrowOnEachFailureUnderDebugger), false);

        /// <summary>
        /// If true, use of the event queue is suppressed and test events are synchronous.
        /// </summary>
        public static SettingDefinition<bool> SynchronousEvents { get; } = new(nameof(SynchronousEvents), false);

        /// <summary>
        /// The default naming pattern used in generating test names
        /// </summary>
        public static SettingDefinition<string> DefaultTestNamePattern { get; } = new(nameof(DefaultTestNamePattern), string.Empty);

        /// <summary>
        /// Parameters to be passed on to the tests, serialized to a single string which needs parsing.
        /// Obsoleted by <see cref="TestParametersDictionary"/>; kept for backward compatibility.
        /// </summary>
        public static SettingDefinition<string> TestParameters { get; } = new(nameof(TestParameters), string.Empty);

        /// <summary>
        /// If true, the tests will run on the same thread as the NUnit runner itself
        /// </summary>
        public static SettingDefinition<bool> RunOnMainThread { get; } = new(nameof(RunOnMainThread), false);

        /// <summary>
        /// Parameters to be passed on to the tests, already parsed into an IDictionary&lt;string, string>. Replaces <see cref="TestParameters"/>.
        /// </summary>
        public static SettingDefinition<IDictionary<string, string>> TestParametersDictionary { get; } = new(nameof(TestParametersDictionary), new Dictionary<string, string>());

        #endregion
    }
}
