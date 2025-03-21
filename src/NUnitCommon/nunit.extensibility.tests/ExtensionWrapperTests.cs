// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NUnit.Extensibility
{
    public class ExtensionWrapperTests
    {
        private DummyExtensionWrapper _wrapper;

        [OneTimeSetUp]
        public void CreateExtensionAndWrapper()
        {
            // Test class is also the wrapped class
            _wrapper = new DummyExtensionWrapper(this);
        }

        [Test]
        public void CallVoidMethodNoArgs()
        {
            _wrapper.CallVoidMethod();
            Assert.That(LastCall, Is.EqualTo("VoidMethod()"));
        }

        [Test]
        public void CallVoidMethodWithArgs()
        {
            _wrapper.CallVoidMethod(5, "foo");
            Assert.That(LastCall, Is.EqualTo("VoidMethod(5,foo)"));
        }

        [Test]
        public void CallVoidMethodWithBadArgs()
        {
            Assert.That(() => _wrapper.CallVoidMethod("bad"), Throws.TypeOf<MissingMethodException>());
        }

        [Test]
        public void CallIntMethod()
        {
            var val = _wrapper.CallIntMethod();
            Assert.That(LastCall, Is.EqualTo("IntMethod()"));
            Assert.That(val, Is.EqualTo(42));
        }

        [Test]
        public void CallIntMethodWithArg()
        {
            var val = _wrapper.CallIntMethod(99);
            Assert.That(LastCall, Is.EqualTo("IntMethod(99)"));
            Assert.That(val, Is.EqualTo(99));
        }

        [Test]
        public void CallStringMethod()
        {
            var val = _wrapper.CallStringMethod();
            Assert.That(LastCall, Is.EqualTo("StringMethod()"));
            Assert.That(val, Is.EqualTo("RESULT"));
        }

        [Test]
        public void CallStringMethodWithArg()
        {
            var val = _wrapper.CallStringMethod("ANSWER");
            Assert.That(LastCall, Is.EqualTo("StringMethod(ANSWER)"));
            Assert.That(val, Is.EqualTo("ANSWER"));
        }

        private List<string> Calls = new List<string>();
        private string LastCall => Calls[Calls.Count - 1];

        #region Nested ExtensionWrapper

        class DummyExtensionWrapper : ExtensionWrapper
        {
            public DummyExtensionWrapper(object wrappedInstance) : base(wrappedInstance) { }

            public void CallVoidMethod() => Invoke("VoidMethod");

            public void CallVoidMethod(int n, string s) => Invoke("VoidMethod", n, s);

            public void CallVoidMethod(string s) => Invoke("VoidMethod", s);

            public int CallIntMethod() => Invoke<int>("IntMethod");

            public int CallIntMethod(int val) => Invoke<int>("IntMethod", val);

            public string CallStringMethod() => Invoke<string>("StringMethod");

            public string CallStringMethod(string val) => Invoke<string>("StringMethod", val);
        }

        #endregion

        #region Methods called by the wrapper

        public void VoidMethod() => Calls.Add("VoidMethod()");

        public void VoidMethod(int n, string s) => Calls.Add($"VoidMethod({n},{s})");

        public int IntMethod()
        {
            Calls.Add("IntMethod()");
            return 42;
        }

        public int IntMethod(int n)
        {
            Calls.Add($"IntMethod({n})");
            return n;
        }

        public string StringMethod()
        {
            Calls.Add("StringMethod()");
            return "RESULT";
        }

        public string StringMethod(string s)
        {
            Calls.Add($"StringMethod({s})");
            return s;
        }

        #endregion
    }
}
