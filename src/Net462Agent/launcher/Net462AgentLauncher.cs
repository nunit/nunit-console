// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using NUnit.Common;
using NUnit.Extensibility;

namespace NUnit.Engine.Agents
{
    [Extension]
    public class Net462AgentLauncher : LocalProcessAgentLauncher
    {
        private static readonly string LAUNCHER_DIR = AssemblyHelper.GetDirectoryName(Assembly.GetExecutingAssembly());

        protected override string AgentName => "Net462Agent";
        protected override TestAgentType AgentType => TestAgentType.LocalProcess;
        protected override FrameworkName AgentRuntime => new FrameworkName(FrameworkIdentifiers.NetFramework, new Version(4, 6, 2));

        protected override string AgentPath => Path.Combine(LAUNCHER_DIR, $"agents/net462/nunit-agent-net462.exe");
        protected override string X86AgentPath => Path.Combine(LAUNCHER_DIR, $"agents/net462/nunit-agent-net462-x86.exe");
    }
}
#endif
