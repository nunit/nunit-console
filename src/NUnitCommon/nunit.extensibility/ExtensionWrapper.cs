// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Extensibility.Wrappers;

namespace NUnit.Extensibility
{
    public abstract class ExtensionWrapper
    {
        public static readonly Type[] NoTypes = [];
        public static readonly Type[] StringType = [typeof(string)];

        static protected readonly Logger log = InternalTrace.GetLogger(typeof(ExtensionWrapper));

        private readonly object _wrappedInstance;
        private readonly Type _wrappedType;

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

        protected void Invoke(string methodName, Type[] argTypes, params object[] args)
        {
            WrapMethod(methodName, argTypes).Invoke(_wrappedInstance, args);
        }

        protected void Invoke(string methodName, params object[] args)
            => Invoke(methodName, Type.GetTypeArray(args), args);

        protected void Invoke(string methodName, string arg)
            => Invoke(methodName, StringType, arg);

        protected T Invoke<T>(string methodName, Type[] argTypes, params object[] args)
        {
            Console.WriteLine($"Wrapper called for {typeof(T)} {methodName}");
            var result = WrapMethod(methodName, argTypes).Invoke(_wrappedInstance, args);
            Console.WriteLine($"    Returning {(T)result.ShouldNotBeNull()}");
            return (T)result.ShouldNotBeNull();
        }

        protected T Invoke<T>(string methodName, params object[] args)
            => Invoke<T>(methodName, Type.GetTypeArray(args), args);

        protected T Invoke<T>(string methodName, string arg)
            => Invoke<T>(methodName, StringType, arg);

        protected T? GetProperty<T>(string propertyName) =>
            (T?)WrapProperty(propertyName).GetValue(_wrappedInstance);

        protected void SetProperty<T>(string propertyName, T val) =>
            WrapProperty(propertyName).SetValue(_wrappedInstance, val);

        private MethodInfo WrapMethod(string methodName, Type[] argTypes)
        {
            var sig = new MethodSignature(methodName, argTypes);
            if (_methods.TryGetValue(sig, out MethodInfo? method))
                return method;

            method = _wrappedType.GetMethod(methodName, argTypes);
            if (method == null)
                throw new MissingMethodException(methodName);

            return _methods[sig] = method;
        }

        private PropertyInfo WrapProperty(string propertyName)
        {
            if (_properties.TryGetValue(propertyName, out PropertyInfo? property))
                return property;
            property = _wrappedType.GetProperty(propertyName);
            if (property == null)
                throw new MissingMemberException(propertyName);
            return _properties[propertyName] = property;
        }

        internal IEnumerable<MethodSignature> GetMethodSignatures() => _methods.Keys;
        internal IEnumerable<string> GetPropertyNames() => _properties.Keys;

        internal class MethodSignature : IEquatable<MethodSignature>
        {
            public readonly string Name;
            public readonly object[] ArgTypes;

            public MethodSignature(string name, object[] argTypes)
            {  
                Name = name; 
                ArgTypes = argTypes;
            }

            public bool Equals(MethodSignature? other)
            {
                if (other is null || Name != other.Name)
                    return false;

                if (ReferenceEquals(ArgTypes, other.ArgTypes))
                    return true;

                if (ArgTypes.Length != other.ArgTypes.Length)
                    return false;

                for (int i = 0; i < ArgTypes.Length; i++)
                    if (ArgTypes[i] != other.ArgTypes[i])
                        return false;

                return true;
            }

            public override bool Equals(object? obj) => Equals(obj as MethodSignature);

            override public int GetHashCode()
            {
                var hash = Name.GetHashCode();
                for (int i = 0; i < ArgTypes.Length; i++)
                    hash ^= ArgTypes[i].GetHashCode();
                return hash;
            }
        }

        private readonly Dictionary<MethodSignature, MethodInfo> _methods = new();
        private readonly Dictionary<string, PropertyInfo> _properties = new();
    }
}
