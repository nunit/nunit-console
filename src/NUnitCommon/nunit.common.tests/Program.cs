﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnitLite;

namespace NUnit.Engine.Tests
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            return new TextRunner(typeof(Program).Assembly).Execute(args);
        }
    }
}
