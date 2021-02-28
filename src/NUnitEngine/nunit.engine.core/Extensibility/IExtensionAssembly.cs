// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Extensibility
{
    internal interface IExtensionAssembly
    {
        bool FromWildCard { get; }
        string AssemblyName { get; }
        Version AssemblyVersion { get; }
#if NETFRAMEWORK
        RuntimeFramework TargetFramework { get; }
#endif
    }
}
