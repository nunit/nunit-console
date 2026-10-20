// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Supplies optional exit diagnostics for an agent without exposing its native process.
    /// </summary>
    internal interface IAgentExitDiagnostics
    {
        /// <summary>
        /// Returns an exit diagnostic for an owned agent, or null if none is available.
        /// </summary>
        string? GetAgentExitMessage(ITestAgent agent);
    }
}
