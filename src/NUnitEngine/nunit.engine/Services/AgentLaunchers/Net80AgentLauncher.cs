// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using NUnit.Common;

namespace NUnit.Engine.Services.AgentLaunchers
{
    public class Net80AgentLauncher : AgentLauncherBase
    {
        private static readonly string LAUNCHER_DIR = AssemblyHelper.GetDirectoryName(Assembly.GetExecutingAssembly());

        protected override string AgentName => "Net80Agent";
        protected override TestAgentType AgentType => TestAgentType.LocalProcess;
        protected override FrameworkName TargetRuntime => new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(8, 0, 0));

        protected override string AgentPath => Path.Combine(LAUNCHER_DIR, $"agents/net8.0/nunit-agent-net80.dll");
    }
}
#endif
