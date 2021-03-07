// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NUnit.Engine
{

    /// <summary>
    /// NUnitEngineUnloadException is thrown when a test run has completed successfully
    /// but one or more errors were encountered when attempting to unload
    /// and shut down the test run cleanly.
    /// </summary>
    [Serializable]
    public class NUnitEngineUnloadException : NUnitEngineException  //Inherits from NUnitEngineException for backwards compatibility of calling runners
    {
        private const string AggregatedExceptionsMsg =
            "Multiple exceptions encountered. Retrieve AggregatedExceptions property for more information";


        /// <summary>
        /// Construct with a message
        /// </summary>
        public NUnitEngineUnloadException(string message) : base(message)
        {
        }

        /// <summary>
        /// Construct with a message and inner exception
        /// </summary>
        public NUnitEngineUnloadException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Construct with a message and a collection of exceptions.
        /// </summary>
        public NUnitEngineUnloadException(ICollection<Exception> aggregatedExceptions) : base(AggregatedExceptionsMsg)
        {
            AggregatedExceptions = aggregatedExceptions;
        }

        /// <summary>
        /// Serialization constructor.
        /// </summary>
        public NUnitEngineUnloadException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <summary>
        /// Gets the collection of exceptions .
        /// </summary>
        public ICollection<Exception> AggregatedExceptions { get; }
    }
}