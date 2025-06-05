﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine.Tests
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            return new NUnitLite.TextRunner(typeof(Program).Assembly).Execute(args);
        }
    }
}
