// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Runtime.Serialization;

namespace NUnit.Extensibility
{
    /// <summary>
    /// NUnitEngineException is thrown when the engine has been
    /// called with improper values or when a particular facility
    /// is not available.
    /// </summary>
    [Serializable]
    public class NUnitExtensibilityException : Exception
    {
        /// <summary>
        /// Construct with a message
        /// </summary>
        public NUnitExtensibilityException(string message) : base(message)
        {
        }

        /// <summary>
        /// Construct with a message and inner exception
        /// </summary>
        public NUnitExtensibilityException(string message, Exception? innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Serialization constructor
        /// </summary>
        public NUnitExtensibilityException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
