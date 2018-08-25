// ***********************************************************************
// Copyright (c) 2018 Charlie Poole, Rob Prouse
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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Common;

namespace NUnit.Engine.Internal.Metadata
{
    internal sealed partial class DirectReflectionMetadataProvider : IAssemblyMetadataProvider
    {
        private readonly string _path;
        private Assembly _assembly;

        public DirectReflectionMetadataProvider(string path)
        {
            Guard.ArgumentNotNull(path, nameof(path));

            _path = path;
        }

        private Assembly Assembly => _assembly ?? (_assembly = Assembly.ReflectionOnlyLoadFrom(_path));

        public bool RequiresX86
        {
            get
            {
                Assembly.ManifestModule.GetPEKind(out var peKind, out _);
                return (peKind & PortableExecutableKinds.Required32Bit) != 0;
            }
        }

        public string MetadataVersion => Assembly.ImageRuntimeVersion;

        public AssemblyName AssemblyName => Assembly.GetName();

        public AssemblyName[] AssemblyReferences => Assembly.GetReferencedAssemblies();

        public bool HasAttribute(string fullAttributeTypeName)
        {
            return GetAttributes(fullAttributeTypeName).Any();
        }

        public IEnumerable<AttributeMetadata> GetAttributes(string fullAttributeTypeName)
        {
            return DoWithReflectionOnlyAssemblyResolve(() =>
                GetAttributes(CustomAttributeData.GetCustomAttributes(Assembly), fullAttributeTypeName));
        }

        public IEnumerable<ITypeMetadataProvider> Types
        {
            get => Assembly.GetTypes().Select(type => (ITypeMetadataProvider)new TypeMetadataProvider(type));
        }

        private static IEnumerable<AttributeMetadata> GetAttributes(IEnumerable<CustomAttributeData> data, string fullAttributeTypeName)
        {
            foreach (var attribute in data)
            {
                if (attribute.Constructor.DeclaringType.FullName == fullAttributeTypeName)
                {
                    yield return new AttributeMetadata(
                        attribute.ConstructorArguments.ConvertAll(a => a.Value),
                        attribute.NamedArguments.ConvertAll(a => new NamedArgument(a.MemberInfo.Name, a.TypedValue.Value)));
                }
            }
        }

        private T DoWithReflectionOnlyAssemblyResolve<T>(Func<T> func)
        {
            Guard.ArgumentNotNull(func, nameof(func));

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += OnReflectionOnlyAssemblyResolve;
            try
            {
                return func.Invoke();
            }
            finally
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += OnReflectionOnlyAssemblyResolve;
            }

            Assembly OnReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
            {
                var fromSameDirectory = AssemblyMetadataProvider.TryResolveAssemblyPath(args.Name, Path.GetDirectoryName(_path));

                return fromSameDirectory != null
                    ? Assembly.ReflectionOnlyLoadFrom(fromSameDirectory)
                    : Assembly.ReflectionOnlyLoad(args.Name);
            }
        }
    }
}
