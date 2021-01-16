// ***********************************************************************
// Copyright (c) 2017 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
#if !NETSTANDARD1_6
using System.Runtime.Serialization;
#endif

namespace NUnit.Engine
{

    /// <summary>
    /// NUnitEngineUnloadException is thrown when a test run has completed successfully
    /// but one or more errors were encountered when attempting to unload
    /// and shut down the test run cleanly.
    /// </summary>
#if !NETSTANDARD1_6
    [Serializable]
#endif
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

#if !NETSTANDARD1_6
        /// <summary>
        /// Serialization constructor.
        /// </summary>
        public NUnitEngineUnloadException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif

        /// <summary>
        /// Gets the collection of exceptions .
        /// </summary>
        public ICollection<Exception> AggregatedExceptions { get; }
    }
}