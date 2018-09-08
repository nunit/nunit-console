#if !NET35
using System.Reflection;
using NUnitLite;

namespace NUnit.Engine.Tests
{
    class Program
    {
        static int Main(string[] args)
        {
            return new TextRunner(typeof(Program).GetTypeInfo().Assembly).Execute(args);
        }
    }
}
#endif