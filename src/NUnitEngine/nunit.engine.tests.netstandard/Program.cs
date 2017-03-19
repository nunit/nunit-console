using NUnitLite;
using System;
using System.Reflection;

namespace nunit.engine.tests.netstandard
{
    class Program
    {
        static int Main(string[] args)
        {
            int result = new TextRunner(typeof(Program).GetTypeInfo().Assembly).Execute(args);
            return result;
        }
    }
}