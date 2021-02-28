// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Common;
using NUnit.Framework;

namespace NUnit.ConsoleRunner.Tests
{
    using Utilities;

    [TestFixture, Parallelizable(ParallelScope.None)]
    public class ColorConsoleTests
    {
        private ColorStyle _testStyle;

        [SetUp]
        public void SetUp()
        {
            // Find a test color that is different than the console color
            if (Console.ForegroundColor != ColorConsole.GetColor( ColorStyle.Error ))
                _testStyle = ColorStyle.Error;
            else if (Console.ForegroundColor != ColorConsole.GetColor( ColorStyle.Pass ))
                _testStyle = ColorStyle.Pass;
            else
                Assert.Inconclusive("Could not find a color to test with");

            // Set to an unknown, unlikely color so that we can test for change
            Console.ForegroundColor = ConsoleColor.Magenta;

            Assume.That(Console.ForegroundColor, Is.EqualTo(ConsoleColor.Magenta), "Color tests cannot be run because the current console does not support color");
        }

        [TearDown]
        public void TearDown()
        {
            Console.ResetColor();
        }

        [Test]
        public void TestConstructor()
        {
            ConsoleColor expected = ColorConsole.GetColor(_testStyle);
            using(new ColorConsole(_testStyle))
            {
                Assert.That(Console.ForegroundColor, Is.EqualTo(expected));
            }
            Assert.That( Console.ForegroundColor, Is.Not.EqualTo(expected) );
        }
    }
}
