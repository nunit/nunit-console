// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Common
{
    /// <summary>
    /// Exit codes returned by all agents from test runs. A value
    /// greater than zero indicates the number of test failures.
    /// </summary>
    public static class AgentExitCodes
    {
        /// <summary>
        /// Tests ran without failure.
        /// </summary>
        public const int OK = 0;

        /// <summary>
        /// The process that created the agent terminated.
        /// </summary>
        public const int PARENT_PROCESS_TERMINATED = -1;
        
        /// <summary>
        /// Could no start a remote agent.
        /// </summary>
        public const int FAILED_TO_START_REMOTE_AGENT = -2;
        
        /// <summary>
        /// Security violation trying to launch the debugger.
        /// </summary>
        public const int DEBUGGER_SECURITY_VIOLATION = -3;
        
        /// <summary>
        /// Debugger is not available on current platform.
        /// </summary>
        public const int DEBUGGER_NOT_IMPLEMENTED = -4;
        
        /// <summary>
        /// Unable to locate the process that created the agent.
        /// </summary>
        public const int UNABLE_TO_LOCATE_AGENCY = -5;
        
        /// <summary>
        /// An unexpected exception terminated execution
        /// </summary>
        public const int UNEXPECTED_EXCEPTION = -100;
        
        /// <summary></summary>
        public const int STACK_OVERFLOW_EXCEPTION = -1073741571;
    }
}
