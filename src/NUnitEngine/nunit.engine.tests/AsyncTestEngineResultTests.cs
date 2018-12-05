// ***********************************************************************
// Copyright (c) 2015 Charlie Poole, Rob Prouse
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

using System;
using System.Xml;
using NUnit.Framework;

namespace NUnit.Engine.Tests
{
    public class AsyncTestEngineResultTests
    {
        private AsyncTestEngineResult _asyncResult;

        [SetUp]
        public void SetUp()
        {
            _asyncResult = new AsyncTestEngineResult();
        }

        [Test]
        public void Result_ThrowsIfNotSet()
        {
            XmlNode result = null;
            Assert.Throws<InvalidOperationException>(() => result = _asyncResult.EngineResult.Xml);
        }

        [Test]
        public void SetResult_ThrowsIfNull()
        {
            Assert.Throws<ArgumentNullException>(() => _asyncResult.SetResult(null));
        }

        [Test]
        public void SetResult_ThrowsIfSetTwice()
        {
            var result = new TestEngineResult();
            _asyncResult.SetResult(result);
            Assert.Throws<InvalidOperationException>(() => _asyncResult.SetResult(result));
        }

        [Test]
        public void IsComplete_FalseIfNotComplete()
        {
            Assert.That(_asyncResult.IsComplete, Is.False);
        }

        [Test]
        public void IsComplete_TrueIfComplete()
        {
            var result = new TestEngineResult();
            _asyncResult.SetResult(result);
            Assert.That(_asyncResult.IsComplete, Is.True);
        }

        [Test]
        public void Wait_ReturnsFalseTillTestCompletes()
        {
            var result = new TestEngineResult("<test-assembly />");

            Assert.That(_asyncResult.Wait(0), Is.False, "Expected wait to be false because test hasn't completed yet");

            _asyncResult.SetResult(result);

            Assert.That(_asyncResult.Wait(0), Is.True, "Expected wait to be true because the test is complete");

            Assert.That(_asyncResult.EngineResult.Xml, Is.EqualTo(result.Xml));
        }

        [Test]
        public void Wait_AllowsMultipleWaits()
        {
            _asyncResult.SetResult(new TestEngineResult());
            
            Assert.That(_asyncResult.Wait(0), Is.True, "Expected wait to be true because the test is complete");

            Assert.That(_asyncResult.Wait(0), Is.True, "Expected the second wait to be non blocking");
        }
    }
}
