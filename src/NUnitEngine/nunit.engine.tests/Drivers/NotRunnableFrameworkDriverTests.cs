// ***********************************************************************
// Copyright (c) 2014 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System.Collections.Generic;
using System.IO;
using System.Xml;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Drivers.Tests
{
    // Functional tests of the NUnitFrameworkDriver calling into the framework.
    public class NotRunnableFrameworkDriverTests
    {
        private const string REASON = "Assembly could not be found";

        [TestCase("junk.dll", "Assembly")]
        [TestCase("junk.exe", "Assembly")]
        [TestCase("junk.cfg", "Unknown")]
        public void Load_ReturnsNonRunnableSuite(string badFile, string expectedType)
        {
            IFrameworkDriver driver = CreateDriver(badFile);
            var result = XmlHelper.CreateXmlNode(driver.Load(badFile, new Dictionary<string, object>()));

            Assert.That(result.Name, Is.EqualTo("test-suite"));
            Assert.That(result.GetAttribute("type"), Is.EqualTo(expectedType));
            Assert.That(result.GetAttribute("id"), Is.EqualTo("99-1"));
            Assert.That(result.GetAttribute("name"), Is.EqualTo(badFile));
            Assert.That(result.GetAttribute("fullname"), Is.EqualTo(Path.GetFullPath(badFile)));
            Assert.That(result.GetAttribute("runstate"), Is.EqualTo("NotRunnable"));
            Assert.That(result.GetAttribute("testcasecount"), Is.EqualTo("0"));
            Assert.That(GetSkipReason(result), Is.EqualTo(REASON));
            Assert.That(result.SelectNodes("test-suite").Count, Is.EqualTo(0), "Load result should not have child tests");
        }

        [TestCase("junk.dll", "Assembly")]
        [TestCase("junk.exe", "Assembly")]
        [TestCase("junk.cfg", "Unknown")]
        public void Explore_ReturnsNonRunnableSuite(string badFile, string expectedType)
        {
            IFrameworkDriver driver = CreateDriver(badFile);
            var result = XmlHelper.CreateXmlNode(driver.Explore(TestFilter.Empty.Text));

            Assert.That(result.Name, Is.EqualTo("test-suite"));
            Assert.That(result.GetAttribute("id"), Is.EqualTo("99-1"));
            Assert.That(result.GetAttribute("name"), Is.EqualTo(badFile));
            Assert.That(result.GetAttribute("fullname"), Is.EqualTo(Path.GetFullPath(badFile)));
            Assert.That(result.GetAttribute("type"), Is.EqualTo(expectedType));
            Assert.That(result.GetAttribute("runstate"), Is.EqualTo("NotRunnable"));
            Assert.That(result.GetAttribute("testcasecount"), Is.EqualTo("0"));
            Assert.That(GetSkipReason(result), Is.EqualTo(REASON));
            Assert.That(result.SelectNodes("test-suite").Count, Is.EqualTo(0), "Result should not have child tests");
        }

        [TestCase("junk.dll")]
        [TestCase("junk.exe")]
        [TestCase("junk.cfg")]
        public void CountTestCases_ReturnsZero(string badFile)
        {
            IFrameworkDriver driver = CreateDriver(badFile);
            Assert.That(driver.CountTestCases(TestFilter.Empty.Text), Is.EqualTo(0));
        }

        [TestCase("junk.dll", "Assembly")]
        [TestCase("junk.exe", "Assembly")]
        [TestCase("junk.cfg", "Unknown")]
        public void Run_ReturnsNonRunnableSuite(string badFile, string expectedType)
        {
            IFrameworkDriver driver = CreateDriver(badFile);
            var result = XmlHelper.CreateXmlNode(driver.Run(new NullListener(), TestFilter.Empty.Text));
            Assert.That(result.Name, Is.EqualTo("test-suite"));
            Assert.That(result.GetAttribute("id"), Is.EqualTo("99-1"));
            Assert.That(result.GetAttribute("name"), Is.EqualTo(badFile));
            Assert.That(result.GetAttribute("fullname"), Is.EqualTo(Path.GetFullPath(badFile)));
            Assert.That(result.GetAttribute("type"), Is.EqualTo(expectedType));
            Assert.That(result.GetAttribute("runstate"), Is.EqualTo("NotRunnable"));
            Assert.That(result.GetAttribute("testcasecount"), Is.EqualTo("0"));
            Assert.That(GetSkipReason(result), Is.EqualTo(REASON));
            Assert.That(result.SelectNodes("test-suite").Count, Is.EqualTo(0), "Load result should not have child tests");
            Assert.That(result.GetAttribute("result"), Is.EqualTo("Failed"));
            Assert.That(result.GetAttribute("label"), Is.EqualTo("Invalid"));
            Assert.That(result.SelectSingleNode("reason/message").InnerText, Is.EqualTo(REASON));
        }

        #region Helper Methods
        private IFrameworkDriver CreateDriver(string filePath)
        {
            IFrameworkDriver driver = new NotRunnableFrameworkDriver(filePath, REASON);
            driver.ID = "99";
            return driver;
        }
        private static string GetSkipReason(XmlNode result)
        {
            var propNode = result.SelectSingleNode(string.Format("properties/property[@name='{0}']", PropertyNames.SkipReason));
            return propNode == null ? null : propNode.GetAttribute("value");
        }
        #endregion

        #region Nested NullListener Class
        private class NullListener : ITestEventListener
        {
            public void OnTestEvent(string testEvent)
            {
                // No action
            }
        }
        #endregion
    }
}
