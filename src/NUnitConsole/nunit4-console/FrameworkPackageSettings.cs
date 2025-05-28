// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using StringSetting = NUnit.Engine.PackageSetting.SettingDefinition<string>;
using IntSetting = NUnit.Engine.PackageSetting.SettingDefinition<int>;
using BoolSetting = NUnit.Engine.PackageSetting.SettingDefinition<bool>;
using FrameworkSetting = NUnit.Engine.PackageSetting.SettingDefinition<object>;

namespace NUnit.ConsoleRunner
{
    /// <summary>
    /// FrameworkPackageSettings is a static class containing constant values that
    /// are used as keys in setting up a TestPackage. These values are used in
    /// the framework, and set in the runner. Setting values may be a string, int or bool.
    /// </summary>
    public static class FrameworkPackageSettings
    {
        /// <summary>
        /// Flag (bool) indicating whether tests are being debugged.
        /// </summary>
        public static readonly BoolSetting DebugTests = new BoolSetting("DebugTests");

        /// <summary>
        /// Flag (bool) indicating whether to pause execution of tests to allow
        /// the user to attach a debugger.
        /// </summary>
        public static readonly BoolSetting PauseBeforeRun = new BoolSetting("PauseBeforeRun");

        /// <summary>
        /// The InternalTraceLevel for this run. Values are: "Default",
        /// "Off", "Error", "Warning", "Info", "Debug", "Verbose".
        /// Default is "Off". "Debug" and "Verbose" are synonyms.
        /// </summary>
        public static readonly StringSetting InternalTraceLevel = new StringSetting("InternalTraceLevel");

        /// <summary>
        /// Full path of the directory to be used for work and result files.
        /// This path is provided to tests by the framework TestContext.
        /// </summary>
        public static readonly StringSetting WorkDirectory = new StringSetting("WorkDirectory");

        /// <summary>
        /// Integer value in milliseconds for the default timeout value
        /// for test cases. If not specified, there is no timeout except
        /// as specified by attributes on the tests themselves.
        /// </summary>
        public static readonly IntSetting DefaultTimeout = new IntSetting("DefaultTimeout");

        /// <summary>
        /// A TextWriter to which the internal trace will be sent.
        /// </summary>
        public const string InternalTraceWriter = "InternalTraceWriter";

        /// <summary>
        /// A list of tests to be loaded.
        /// </summary>
        // TODO: Remove?
        public const string LOAD = "LOAD";

        /// <summary>
        /// The number of test threads to run for the assembly. If set to
        /// 1, a single queue is used. If set to 0, tests are executed
        /// directly, without queuing.
        /// </summary>
        public static readonly IntSetting NumberOfTestWorkers = new IntSetting("NumberOfTestWorkers");

        /// <summary>
        /// The random seed to be used for this assembly. If specified
        /// as the value reported from a prior run, the framework should
        /// generate identical random values for tests as were used for
        /// that run, provided that no change has been made to the test
        /// assembly. Default is a random value itself.
        /// </summary>
        public static readonly IntSetting RandomSeed = new IntSetting("RandomSeed");

        /// <summary>
        /// If true, execution stops after the first error or failure.
        /// </summary>
        public static readonly BoolSetting StopOnError = new BoolSetting("StopOnError");

        /// <summary>
        /// If true, use of the event queue is suppressed and test events are synchronous.
        /// </summary>
        public static readonly BoolSetting SynchronousEvents = new BoolSetting("SynchronousEvents");

        /// <summary>
        /// The default naming pattern used in generating test names
        /// </summary>
        public static readonly StringSetting DefaultTestNamePattern = new StringSetting("DefaultTestNamePattern");

        /// <summary>
        /// Parameters to be passed on to the tests, serialized to a single string which needs parsing. Obsoleted by <see cref="TestParametersDictionary"/>; kept for backward compatibility.
        /// </summary>
        public static readonly StringSetting TestParameters = new StringSetting("TestParameters");

        /// <summary>
        /// Parameters to be passed on to the tests, already parsed into an IDictionary&lt;string, string>. Replaces <see cref="TestParameters"/>.
        /// </summary>
        public static readonly FrameworkSetting TestParametersDictionary = new FrameworkSetting("TestParametersDictionary");
    }
}
