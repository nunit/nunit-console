// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

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
