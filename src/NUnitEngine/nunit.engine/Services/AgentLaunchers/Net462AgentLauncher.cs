// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Permissions;
using System.Text;
using NUnit.Common;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Services.AgentLaunchers
{
    public class Net462AgentLauncher : AgentLauncherBase
    {
        private static readonly string LAUNCHER_DIR = AssemblyHelper.GetDirectoryName(Assembly.GetExecutingAssembly());

        protected override string AgentName => "Net462Agent";
        protected override TestAgentType AgentType => TestAgentType.LocalProcess;
        protected override FrameworkName TargetRuntime => new FrameworkName(FrameworkIdentifiers.NetFramework, new Version(4, 6, 2));

        protected override string AgentPath => Path.Combine(LAUNCHER_DIR, $"agents/net462/nunit-agent-net462.exe");
        protected override string X86AgentPath => Path.Combine(LAUNCHER_DIR, $"agents/net462/nunit-agent-net462-x86.exe");
    }
}
#endif
