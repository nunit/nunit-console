// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

#if NETFRAMEWORK
using FrameworkName = NUnit.Engine.Compatibility.FrameworkName;
#else
using FrameworkName = System.Runtime.Versioning.FrameworkName;
#endif

namespace NUnit.Engine.Extensibility
{
    internal interface IExtensionAssembly
    {
        bool FromWildCard { get; }
        string AssemblyName { get; }
        Version AssemblyVersion { get; }
        FrameworkName TargetRuntime { get; }
    }
}
