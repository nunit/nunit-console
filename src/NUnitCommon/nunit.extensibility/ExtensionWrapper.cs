// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Reflection;

namespace NUnit.Extensibility
{
    using Wrappers;

    public abstract class ExtensionWrapper
    {
        static protected readonly Logger log = InternalTrace.GetLogger(typeof(ExtensionWrapper));

        private object _wrappedInstance;
        private Type _wrappedType;

        public static ExtensionWrapper Wrap(object extension, string path)
        {
            switch (path)
            {
                // NYI and probably will not be implemented as we know of
                // no existing v3 IService extensions.
                //case "/NUnit/Engine/TypeExtensions/IService":
                //    return new ServiceWrapper(extension);
                case "/NUnit/Engine/TypeExtensions/ITestEventListener":
                    return new TestEventListenerWrapper(extension);
                // TODO: NYI
                //case "/NUnit/Engine/TypeExtensions/IDriverFactory":
                //    return new DriverFactoryWrapper(extension);
                case "/NUnit/Engine/TypeExtensions/IProjectLoader":
                    return new ProjectLoaderWrapper(extension);
                case "/NUnit/Engine/TypeExtensions/IResultWriter":
                    return new ResultWriterWrapper(extension);
                default:
                    throw new NUnitExtensibilityException($"No wrapper available for extension path {path}");
            }
        }

        protected ExtensionWrapper(object wrappedInstance)
        {
            _wrappedInstance = wrappedInstance;
            _wrappedType = wrappedInstance.GetType();
        }

        protected void Invoke(string methodName, params object[] args)
        {
            WrapMethod(methodName, Type.GetTypeArray(args)).Invoke(_wrappedInstance, args);
        }

        protected T Invoke<T>(string methodName, params object[] args)
        {
            Console.WriteLine($"Wrapper called for {typeof(T)} {methodName}");
            if (args == null) args = Array.Empty<object>();
            var result = WrapMethod(methodName, Type.GetTypeArray(args)).Invoke(_wrappedInstance, args);
            Console.WriteLine($"    Returning {(T)result.ShouldNotBeNull()}");
            return (T)result.ShouldNotBeNull();
        }

        protected T? GetProperty<T>(string propertyName) =>
            (T?)WrapMethod($"get_{propertyName}").Invoke(_wrappedInstance, Array.Empty<object>());

        protected void SetProperty<T>(string propertyName, T val)
        {
            if (val != null)
                WrapMethod($"set_{propertyName}", new[] { typeof(T) }).Invoke(_wrappedInstance, new object[] { val });
        }

        private MethodInfo WrapMethod(string methodName, params Type[] argTypes)
        {
            var sig = new MethodSignature(methodName, argTypes);
            if (_methods.ContainsKey(sig))
                return _methods[sig];

            var method = _wrappedType.GetMethod(methodName, argTypes);
            if (method == null)
                throw new MissingMethodException(methodName);

            return _methods[sig] = method;
        }

        // TODO: Support indexed properties only if we need them
        private PropertyInfo WrapProperty(string propertyName)
        {
            var prop = _wrappedType.GetProperty(propertyName);
            if (prop == null)
                throw new MissingMemberException(propertyName);
            return prop;
        }

        private struct MethodSignature
        {
            public string Name;
            public object[] ArgTypes;

            public MethodSignature(string name, object[] argTypes)
            {  
                Name = name; 
                ArgTypes = argTypes;
            }
        }

        private Dictionary<MethodSignature, MethodInfo> _methods = new Dictionary<MethodSignature, MethodInfo>();
    }
}
