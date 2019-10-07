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
using Mono.Cecil;

namespace NUnit.Engine.Internal
{
    public class TargetFrameworkHelper
    {
        private readonly AssemblyDefinition _assemblyDef;
        private readonly ModuleDefinition _module;

        public TargetFrameworkHelper(string assemblyPath)
        {
            try
            {
                _assemblyDef = AssemblyDefinition.ReadAssembly(assemblyPath);
                _module = _assemblyDef.MainModule;
            }
            catch (Exception e)
            {
                throw new NUnitEngineException($"{assemblyPath} could not be examined", e);
            }
        }

        public TargetFrameworkHelper(AssemblyDefinition assemblyDef)
        {
            _assemblyDef = assemblyDef;
            _module = _assemblyDef.MainModule;
        }

        public bool RequiresX86
        {
            get
            {
                const ModuleAttributes nativeEntryPoint = (ModuleAttributes)16;
                const ModuleAttributes mask = ModuleAttributes.Required32Bit | nativeEntryPoint;

                return _module.Architecture != TargetArchitecture.AMD64 &&
                       _module.Architecture != TargetArchitecture.IA64 &&
                       (_module.Attributes & mask) != 0;
            }
        }

        public Version TargetRuntimeVersion
        {
            get
            {
                var runtimeVersion = _module.RuntimeVersion;

                if (runtimeVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                    runtimeVersion = runtimeVersion.Remove(0, 1);

                return new Version(runtimeVersion);
            }
        }

        public string FrameworkName
        {
            get
            {
                foreach (var attr in _assemblyDef.CustomAttributes)
                {
                    if (attr.AttributeType.FullName != "System.Runtime.Versioning.TargetFrameworkAttribute")
                        continue;

                    var frameworkName = attr.ConstructorArguments[0].Value as string;
                    if (frameworkName != null)
                        return frameworkName;
                    break;
                }

                return null;
            }
        }


        public bool RequiresAssemblyResolver
        {
            get
            {
                foreach (var attr in _assemblyDef.CustomAttributes)
                {
                    if (attr.AttributeType.FullName == "NUnit.Framework.TestAssemblyDirectoryResolveAttribute")
                        return true;
                }

                return false;
            }
        }
    }
}