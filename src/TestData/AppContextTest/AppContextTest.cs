// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using NUnit.Framework;

namespace NUnit.Engine.Core.Tests
{
    public class AppContextTest
    {
        [Test]
        public void VerifyBasePath()
        {
            var thisAssembly = GetType().Assembly;
            var expectedPath = Path.GetDirectoryName(GetType().Assembly.Location);

            Assert.That(AppContext.BaseDirectory, Is.SamePath(expectedPath));
        }
    }
}
