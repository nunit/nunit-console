// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnitLite;

namespace NUnit.Engine.Tests
{
    internal class Program
    {
        /// <summary>Runs the normal NUnit suite or an explicitly selected child-process helper.</summary>
        /// <remarks>
        /// The fixtures using these helpers are discovered and run by the normal suite.
        /// Exit probes use a separate process so an unhandled native-exit callback fails
        /// one test instead of terminating its parent runner. Provider-server helpers
        /// exercise real connection loss and standard-input EOF ownership. These modes
        /// do not create additional test suites or change ordinary test selection.
        /// </remarks>
        private static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--test-provider-server")
                return TestHelpers.ProviderServerHost.Run(args);

            if (args.Length > 0 && args[0] == "--agent-exit-probe")
                return Services.TestAgentServiceExitTests.RunProbe(args);

            if (args.Length > 0 && args[0] == "--agent-exit-child")
                return Services.TestAgentServiceExitTests.RunChild(args);

            return new TextRunner(typeof(Program).Assembly).Execute(args);
        }
    }
}
