// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Linq;
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

        [TestCase(42)]
        [TestCase(99)]
        public void IntProperty(int value)
        {
            _wrapper.IntProperty = value;
            Assert.That(_wrappedClass.LastCall, Is.EqualTo("SetIntProperty"));
            var val = _wrappedClass.IntProperty;
            Assert.That(_wrappedClass.LastCall, Is.EqualTo("GetIntProperty"));
            Assert.That(val, Is.EqualTo(value));

            Assert.That(_wrapper.GetPropertyNames(), Does.Contain("IntProperty"));
        }

        [TestCase("THE ANSWER")]
        [TestCase(null)]
        public void StringProperty(string? value)
        {
            _wrapper.StringProperty = value;
            Assert.That(_wrappedClass.LastCall, Is.EqualTo("SetStringProperty"));
            var val = _wrapper.StringProperty;
            Assert.That(_wrappedClass.LastCall, Is.EqualTo("GetStringProperty"));
            Assert.That(val, Is.EqualTo(value));

            Assert.That(_wrapper.GetPropertyNames(), Does.Contain("StringProperty"));
        }

        [Test]
        public void CheckCachingWithoutParameters()
        {
            _wrapper.CallVoidMethod();
            Assert.That(_wrappedClass.LastCall, Is.EqualTo("VoidMethod()"));
            _wrapper.CallVoidMethod();
            Assert.That(_wrappedClass.LastCall, Is.EqualTo("VoidMethod()"));

            // There should only be one method as otherwise the cache is not working
            var voidMethodSignatures = _wrapper.GetMethodSignatures().Where(sig => sig.Name == "VoidMethod" && sig.ArgTypes.Length == 0).ToArray();
            Assert.That(voidMethodSignatures, Has.Length.EqualTo(1));
        }

        [Test]
        public void CheckCachingWithParameters()
        {
            _wrapper.CallVoidMethod(5, "foo");
            Assert.That(_wrappedClass.LastCall, Is.EqualTo("VoidMethod(5,foo)"));
            _wrapper.CallVoidMethod(42, "galaxy");
            Assert.That(_wrappedClass.LastCall, Is.EqualTo("VoidMethod(42,galaxy)"));

            // There should only be one method as otherwise the cache is not working
            var voidMethodSignatures = _wrapper.GetMethodSignatures().Where(sig => sig.Name == "VoidMethod" && sig.ArgTypes.Length == 2).ToArray();
            Assert.That(voidMethodSignatures, Has.Length.EqualTo(1));
        }
    }

    #region DummyExtensionWrapper

    class DummyExtensionWrapper : ExtensionWrapper
    {
        private static readonly Type[] IntStringTypes = [typeof(int), typeof(string)];
        private static readonly Type[] IntType = [typeof(int)];

        public DummyExtensionWrapper(object wrappedInstance) : base(wrappedInstance) { }

        public void CallVoidMethod() => Invoke("VoidMethod", NoTypes);

        public void CallVoidMethod(int n, string s) => Invoke("VoidMethod", IntStringTypes, n, s);

        public void CallVoidMethod(string s) => Invoke("VoidMethod", StringType, s);

        public int CallIntMethod() => Invoke<int>("IntMethod", NoTypes);

        public int CallIntMethod(int val) => Invoke<int>("IntMethod", IntType, val);

        public string CallStringMethod() => Invoke<string>("StringMethod", NoTypes);

        public string CallStringMethod(string val) => Invoke<string>("StringMethod", StringType, val);

        public IThingy CallThingyMethod() => new ThingyWrapper(Invoke<object>("GetThingy", NoTypes));

        public int IntProperty
        {
            get => GetProperty<int>("IntProperty");
            set => SetProperty("IntProperty", value);
        }

        public string? StringProperty
        {
            get => GetProperty<string>("StringProperty");
            set => SetProperty("StringProperty", value);
        }
    }

    #endregion

    #region DummyExtensionClass

    public class DummyExtensionClass
    {
        public string? LastCall;

        public int IntValue;
        public string? StringValue;

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
                LastCall = "GetIntProperty";
                return IntValue;
            }
            set
            {
                LastCall = "SetIntProperty";
                IntValue = value;
            }
        }

        public string? StringProperty
        {
            get
            {
                LastCall = "GetStringProperty";
                return StringValue;
            }
            set
            {
                LastCall = "SetStringProperty";
                StringValue = value;
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
