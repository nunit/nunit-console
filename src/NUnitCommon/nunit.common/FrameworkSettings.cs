// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine;
using System.Collections.Generic;
using System.IO;

namespace NUnit.Common
{
    /// <summary>
    /// FrameworkSettings is a static class containing SettingDefinitions
    /// for settings used by the NUnit Framework. It is based on and references the
    /// FrameworkPackageSettings Type imported from the framework. We rely on these
    /// definitions to allow us to properly transmit the settings to the framework.
    /// <para>Coding Examples:
    /// <code>Settings.Add(FrameworkSettings.DefaultTimeout).WithValue(5000);</code>
    /// </para>
    /// </summary>
    /// <remarks>
    /// Both files must be maintained in sync and deletion of any settings should be
    /// considered a breaking change. Deletion of any settings from FrameworkPackageSettings
    /// will cause a compiler error in this file. The reverse check, for missing entries
    /// in this file, requires running a test.
    /// </remarks>
    public static class FrameworkSettings
    {
        /// <summary>
        /// Flag (bool) indicating whether tests are being debugged.
        /// </summary>
        public static readonly SettingDefinition<bool> DebugTests =
            new SettingDefinition<bool>(FrameworkPackageSettings.DebugTests);

        /// <summary>
        /// Flag (bool) indicating whether to pause execution of tests to allow
        /// the user to attach a debugger.
        /// </summary>
        public static readonly SettingDefinition<bool> PauseBeforeRun =
            new SettingDefinition<bool>(FrameworkPackageSettings.PauseBeforeRun);

        /// <summary>
        /// The InternalTraceLevel for this run. Values are: "Default",
        /// "Off", "Error", "Warning", "Info", "Debug", "Verbose".
        /// Default is "Off". "Debug" and "Verbose" are synonyms.
        /// </summary>
        public static readonly SettingDefinition<string> InternalTraceLevel =
            new SettingDefinition<string>(FrameworkPackageSettings.InternalTraceLevel);

        /// <summary>
        /// Full path of the directory to be used for work and result files.
        /// This path is provided to tests by the framework TestContext.
        /// </summary>
        public static readonly SettingDefinition<string> WorkDirectory =
            new SettingDefinition<string>(FrameworkPackageSettings.WorkDirectory);

        /// <summary>
        /// Integer value in milliseconds for the default timeout value
        /// for test cases. If not specified, there is no timeout except
        /// as specified by attributes on the tests themselves.
        /// </summary>
        public static readonly SettingDefinition<int> DefaultTimeout =
            new SettingDefinition<int>(FrameworkPackageSettings.DefaultTimeout);

        /// <summary>
        /// A string representing the default thread culture to be used for
        /// running tests. String should be a valid BCP-47 culture name. If
        /// culture is unset, tests run on the machine's default culture.
        /// </summary>
        public static readonly SettingDefinition<string> DefaultCulture =
            new SettingDefinition<string>(FrameworkPackageSettings.DefaultCulture);

        /// <summary>
        /// A string representing the default thread UI culture to be used for
        /// running tests. String should be a valid BCP-47 culture name. If
        /// culture is unset, tests run on the machine's default culture.
        /// </summary>
        public static readonly SettingDefinition<string> DefaultUICulture =
            new SettingDefinition<string>(FrameworkPackageSettings.DefaultUICulture);

        /// <summary>
        /// A TextWriter to which the internal trace will be sent.
        /// </summary>
        public static readonly SettingDefinition<TextWriter> InternalTraceWriter =
            new SettingDefinition<TextWriter>(FrameworkPackageSettings.InternalTraceWriter);

        /// <summary>
        /// A list of tests to be loaded.
        /// </summary>
        public static readonly SettingDefinition<IList<string>> LOAD =
            new SettingDefinition<IList<string>>(FrameworkPackageSettings.LOAD);

        /// <summary>
        /// The number of test threads to run for the assembly. If set to
        /// 1, a single queue is used. If set to 0, tests are executed
        /// directly, without queuing.
        /// </summary>
        public static readonly SettingDefinition<int> NumberOfTestWorkers =
            new SettingDefinition<int>(FrameworkPackageSettings.NumberOfTestWorkers);

        /// <summary>
        /// The random seed to be used for this assembly. If specified
        /// as the value reported from a prior run, the framework should
        /// generate identical random values for tests as were used for
        /// that run, provided that no change has been made to the test
        /// assembly. Default is a random value itself.
        /// </summary>
        public static readonly SettingDefinition<int> RandomSeed =
            new SettingDefinition<int>(FrameworkPackageSettings.RandomSeed);

        /// <summary>
        /// If true, execution stops after the first error or failure.
        /// </summary>
        public static readonly SettingDefinition<bool> StopOnError =
            new SettingDefinition<bool>(FrameworkPackageSettings.StopOnError);

        /// <summary>
        /// If true, asserts in multiple asserts block will throw first-chance exception on failure.
        /// </summary>
        public static readonly SettingDefinition<bool> ThrowOnEachFailureUnderDebugger =
            new SettingDefinition<bool>(FrameworkPackageSettings.ThrowOnEachFailureUnderDebugger);

        /// <summary>
        /// If true, use of the event queue is suppressed and test events are synchronous.
        /// </summary>
        public static readonly SettingDefinition<bool> SynchronousEvents =
            new SettingDefinition<bool>(FrameworkPackageSettings.SynchronousEvents);

        /// <summary>
        /// The default naming pattern used in generating test names
        /// </summary>
        public static readonly SettingDefinition<string> DefaultTestNamePattern =
            new SettingDefinition<string>(FrameworkPackageSettings.DefaultTestNamePattern);

        /// <summary>
        /// Parameters to be passed on to the tests, serialized to a single string which needs parsing. Obsoleted by <see cref="TestParametersDictionary"/>; kept for backward compatibility.
        /// </summary>
        public static readonly SettingDefinition<string> TestParameters =
            new SettingDefinition<string>(FrameworkPackageSettings.TestParameters);

        /// <summary>
        /// If true, the tests will run on the same thread as the NUnit runner itself
        /// </summary>
        public static readonly SettingDefinition<bool> RunOnMainThread =
            new SettingDefinition<bool>(FrameworkPackageSettings.RunOnMainThread);

        /// <summary>
        /// Parameters to be passed on to the tests, already parsed into an IDictionary&lt;string, string>. Replaces <see cref="TestParameters"/>.
        /// </summary>
        public static readonly SettingDefinition<IDictionary<string, string>> TestParametersDictionary =
            new SettingDefinition<IDictionary<string, string>>(FrameworkPackageSettings.TestParametersDictionary);
    }
}
