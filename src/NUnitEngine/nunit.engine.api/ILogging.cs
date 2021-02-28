// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine
{
    /// <summary>
    /// Interface to abstract getting loggers
    /// </summary>
    public interface ILogging
    {
        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="name">The name of the logger to get.</param>
        /// <returns></returns>
        ILogger GetLogger(string name);
    }
}
