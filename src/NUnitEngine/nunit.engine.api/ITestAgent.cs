// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine
{
    /// <summary>
    /// The ITestAgent interface is implemented by remote test agents.
    /// </summary>
    public interface ITestAgent
    {
        /// <summary>
        /// Gets a Guid that uniquely identifies this agent.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Stops the agent, releasing any resources
        /// </summary>
        void Stop();

        /// <summary>
        ///  Creates a test runner
        /// </summary>
        ITestEngineRunner CreateRunner(TestPackage package);
    }
}
