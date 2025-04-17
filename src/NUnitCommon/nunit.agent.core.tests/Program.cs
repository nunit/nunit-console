// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Reflection;
using NUnitLite;

namespace NUnit.Engine.Tests
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return new TextRunner(typeof(Program).Assembly).Execute(args);
        }
    }
}
