// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Common;
using NUnit.Framework;

namespace NUnit.ConsoleRunner.Utilities.Tests
{
    [TestFixture]
    public class ColorStyleTests
    {
        // We can only test colors that are the same for all console backgrounds
        [TestCase(ColorStyle.Pass, ConsoleColor.Green)]
        [TestCase(ColorStyle.Failure, ConsoleColor.Red)]
        //[TestCase(ColorStyle.Warning, ConsoleColor.Yellow)]
        [TestCase(ColorStyle.Error, ConsoleColor.Red)]
        public void TestGetColor( ColorStyle style, ConsoleColor expected )
        {
            Assert.That(ColorConsole.GetColor(style), Is.EqualTo(expected));
        }
    }
}
