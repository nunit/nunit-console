// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NUnit.Extensibility
{
    public class ExtensionWrapperTests
    {
        private DummyExtensionClass _wrappedClass;
        private DummyExtensionWrapper _wrapper;

        [OneTimeSetUp]
        public void CreateExtensionAndWrapper()
        {
            _wrappedClass = new DummyExtensionClass();
            _wrapper = new DummyExtensionWrapper(_wrappedClass);
        }

        [Test]
        public void CallVoidMethodNoArgs()
        {
            _wrapper.CallVoidMethod();
            Assert.That(_wrappedClass.LastCall, Is.EqualTo("VoidMethod()"));
        }

        [Test]
        public void CallVoidMethodWithArgs()
        {
            _wrapper.CallVoidMethod(5, "foo");
            Assert.That(_wrappedClass.LastCall, Is.EqualTo("VoidMethod(5,foo)"));
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
            Assert.That(_wrappedClass.LastCall, Is.EqualTo("IntMethod()"));
            Assert.That(val, Is.EqualTo(42));
        }

        [Test]
        public void CallIntMethodWithArg()
        {
            var val = _wrapper.CallIntMethod(99);
            Assert.That(_wrappedClass.LastCall, Is.EqualTo("IntMethod(99)"));
            Assert.That(val, Is.EqualTo(99));
        }

        [Test]
        public void CallStringMethod()
        {
            var val = _wrapper.CallStringMethod();
            Assert.That(_wrappedClass.LastCall, Is.EqualTo("StringMethod()"));
            Assert.That(val, Is.EqualTo("RESULT"));
        }

        [Test]
        public void CallStringMethodWithArg()
        {
            var val = _wrapper.CallStringMethod("ANSWER");
            Assert.That(_wrappedClass.LastCall, Is.EqualTo("StringMethod(ANSWER)"));
            Assert.That(val, Is.EqualTo("ANSWER"));
        }

        [Test]
        public void CallMethodReturningCustomClass()
        {
            var thingy = _wrapper.CallThingyMethod();
            Assert.That(_wrappedClass.LastCall, Is.EqualTo("GetThingy()"));
            Assert.That(thingy.Name, Is.EqualTo("THINGY"));
        }

        [Test]
        public void GetIntProperty()
        {
            var val = _wrapper.IntProperty;
            Assert.That(_wrappedClass.LastCall, Is.EqualTo("IntProperty"));
            Assert.That(val, Is.EqualTo(42));
        }

        [Test]
        public void GetStringProperty()
        {
            var val = _wrapper.StringProperty;
            Assert.That(_wrappedClass.LastCall, Is.EqualTo("StringProperty"));
            Assert.That(val, Is.EqualTo("The ANSWER"));
        }
    }

    #region DummyExtensionWrapper

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

        public IThingy CallThingyMethod() => new ThingyWrapper(Invoke<object>("GetThingy"));

        public int IntProperty => GetProperty<int>("IntProperty");

        public string? StringProperty => GetProperty<string>("StringProperty");
    }

    #endregion

    #region DummyExtensionClass

    public class DummyExtensionClass
    {
        public string? LastCall;

        public void VoidMethod() => LastCall = "VoidMethod()";

        public void VoidMethod(int n, string s) => LastCall = $"VoidMethod({n},{s})";

        public int IntMethod()
        {
            LastCall = "IntMethod()";
            return 42;
        }

        public int IntMethod(int n)
        {
            LastCall = $"IntMethod({n})";
            return n;
        }

        public string StringMethod()
        {
            LastCall = "StringMethod()";
            return "RESULT";
        }

        public string StringMethod(string s)
        {
            LastCall = $"StringMethod({s})";
            return s;
        }

        public int IntProperty
        {
            get
            {
                LastCall = "IntProperty";
                return 42;
            }
        }

        public string StringProperty
        {
            get
            {
                LastCall = "StringProperty";
                return "The ANSWER";
            }
        }

        public Thingy GetThingy()
        {
            LastCall = "GetThingy()";
            return new Thingy("THINGY");
        }

        public class Thingy
        {
            public Thingy(string name)
            {
                Name = name;
            }

            public string Name { get; set; }
        }
    }

    public interface IThingy
    {
        string Name { get; }
    }

    public class ThingyWrapper : ExtensionWrapper, IThingy
    {
        public ThingyWrapper(object wrappedInstance) : base(wrappedInstance) { }

        public string Name => "THINGY";
    }

    #endregion
}
