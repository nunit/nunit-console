// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Runtime.Versioning;

namespace NUnit.Extensibility
{
    public interface IExtensionAssembly
    {
        bool FromWildCard { get; }
        string AssemblyName { get; }
        Version AssemblyVersion { get; }
//#if NETFRAMEWORK
//        RuntimeFramework TargetFramework { get; }
//#endif
        FrameworkName FrameworkName { get; }
    }
}
