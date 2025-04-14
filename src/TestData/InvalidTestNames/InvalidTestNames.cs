// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace InvalidTestNames
{
    // Issue 1469 showed that the NUnit framework sometimes sends char
    // '\uffff' in test names, if the user provides it as an argument.
    // Therefore, the runner has to protect itself. If additional bad
    // characters are detected, we can add more tests here.
    public class InvalidTestNames
    {
        // Generates test name TestContainsInvalidCharacter("\uffff");
        [TestCase(char.MaxValue)]
        public void TestNameContainsInvalidChar(char c)
        {
            Console.WriteLine($"Test for char {c}");
        }
    }
}
