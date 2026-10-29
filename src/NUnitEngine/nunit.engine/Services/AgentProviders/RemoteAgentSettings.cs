// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Interprets explicit remote selection without changing the public API package.
    /// ServerDefinition is a provisional package setting for this integration candidate.
    /// </summary>
    internal static class RemoteAgentSettings
    {
        internal static readonly SettingDefinition<string> ServerDefinition = new SettingDefinition<string>("RemoteServerDefinition", string.Empty);
        internal static readonly SettingDefinition<string> AgentType = new SettingDefinition<string>("TestAgentType", "Any");

        /// <summary>
        /// Gets the explicitly requested type. Selecting a server also selects remote execution.
        /// Invalid or conflicting settings fail before a local launcher can be used.
        /// </summary>
        internal static TestAgentType RequestedType(TestPackage package)
        {
            object? value = package.Settings.HasSetting(AgentType.Name) ? package.Settings.GetSetting(AgentType.Name) : null;
            TestAgentType requested = TestAgentType.Any;
            if (value is not null && (!Enum.TryParse(value.ToString(), true, out requested)
                || (requested != TestAgentType.Any && requested != TestAgentType.LocalProcess
                    && requested != TestAgentType.InProcess && requested != TestAgentType.RemoteProcess)))
                throw new ArgumentException("TestAgentType must name one supported agent type.", nameof(package));

            if (package.Settings.HasSetting(ServerDefinition))
            {
                if (requested != TestAgentType.Any && requested != TestAgentType.RemoteProcess)
                    throw new ArgumentException("RemoteServerDefinition conflicts with the requested agent type.", nameof(package));
                return TestAgentType.RemoteProcess;
            }

            return requested;
        }

        /// <summary>Determines whether this request permits a local-process provider.</summary>
        internal static bool AllowsLocalProcess(TestPackage package)
        {
            var requested = RequestedType(package);
            return requested == TestAgentType.Any || requested == TestAgentType.LocalProcess;
        }
    }
}
