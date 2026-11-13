// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Reflection;
using NUnit.Engine.Services;

namespace NUnit.Engine.TestHelpers
{
    /// <summary>
    /// Accesses the local provider's native process records for controlled race tests
    /// and process-cleanup evidence without adding a production test hook.
    /// </summary>
    /// <remarks>
    /// Release tests deliberately retain a record's pre-callback state after native exit,
    /// or supply an unavailable process association. Real acquisition would attach the
    /// exit callback and could update the record before the shutdown path is exercised.
    /// </remarks>
    internal static class LocalProcessAgentTestAccess
    {
        private static readonly FieldInfo AgentStoreField = typeof(LocalProcessAgentProvider)
            .GetField("_agentStore", BindingFlags.Instance | BindingFlags.NonPublic)!;

        internal static AgentStore GetStore(LocalProcessAgentProvider provider) =>
            (AgentStore)AgentStoreField.GetValue(provider)!;
    }
}
