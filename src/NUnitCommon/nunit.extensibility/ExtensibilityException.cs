// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Runtime.Serialization;

namespace NUnit.Extensibility
{
    /// <summary>
    /// ExtensibilityException is thrown when the extensibility features
    /// are used with improper values or when a particular feature
    /// is not available.
    /// </summary>
    [Serializable]
    public class ExtensibilityException : Exception
    {
        /// <summary>
        /// Construct with a message
        /// </summary>
        public ExtensibilityException(string message) : base(message)
        {
        }

        /// <summary>
        /// Construct with a message and inner exception
        /// </summary>
        public ExtensibilityException(string message, Exception? innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Serialization constructor
        /// </summary>
        public ExtensibilityException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
