// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine
{
    /// <summary>
    /// The exception that is thrown if a valid test engine is not found
    /// </summary>
    [Serializable]
    public class NUnitEngineNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NUnitEngineNotFoundException"/> class.
        /// </summary>
        public NUnitEngineNotFoundException() : base()
        {
        }
    }
}