//#if !NET35
using NUnitLite;
using System;
using System.Reflection;

namespace NUnit.Engine.Tests
{
    class Program
    {
        static int Main(string[] args)
        {
#if !NET35
            return new TextRunner(typeof(Program).GetTypeInfo().Assembly).Execute(args);
#else
            return new TextRunner(typeof(Program).Assembly).Execute(args);
#endif
        }
    }
}
//#endif