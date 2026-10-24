// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnitLite;

namespace NUnit.Engine.Tests
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            //if (args.Length > 0 && args[0] == "--agent-exit-probe")
            //    return Services.TestAgencyExitTests.RunProbe(args);

            //if (args.Length > 0 && args[0] == "--agent-exit-child")
            //    return Services.TestAgencyExitTests.RunChild(args);

            return new TextRunner(typeof(Program).Assembly).Execute(args);
        }
    }
}
