// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Runtime.Serialization;

namespace NUnit.Engine
{
    /// <summary>
    /// NUnitEngineException is thrown when the engine has been
    /// called with improper values or when a particular facility
    /// is not available.
    /// </summary>
    [Serializable]
    public class NUnitEngineException : Exception
    {
        /// <summary>
        /// Construct with a message
        /// </summary>
        public NUnitEngineException(string message) : base(message)
        {
        }

        /// <summary>
        /// Construct with a message and inner exception
        /// </summary>
        public NUnitEngineException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Serialization constructor
        /// </summary>
        public NUnitEngineException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
