// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine
{
    /// <summary>
    /// The ITestAgent interface is implemented by remote test agents.
    /// </summary>
    public interface ITestAgent
    {
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
