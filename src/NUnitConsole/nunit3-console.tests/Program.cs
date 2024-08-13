// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Reflection;

namespace NUnit.Engine.Tests
{
    class Program
    {
        static int Main(string[] args)
        {
#if NETFRAMEWORK
            return new NUnitLite.TextRunner(typeof(Program).Assembly).Execute(args);
#else
            return new NUnitLite.TextRunner(typeof(Program).GetTypeInfo().Assembly).Execute(args);
#endif
        }
    }
}
