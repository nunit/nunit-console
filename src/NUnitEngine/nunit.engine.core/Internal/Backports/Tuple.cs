//-----------------------------------------------------------------------
// <copyright file="Tuple.cs" company="TillW">
//   Copyright 2021 TillW. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace NUnit.Engine.Internal.Backports
{
    /// <summary>
    /// Represents a tuple with two elements.
    /// </summary>
    /// <remarks>This is a partial backport of the Tuple-class described here: https://docs.microsoft.com/en-us/dotnet/api/system.tuple-2</remarks>
    internal sealed class Tuple<T1, T2>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Tuple{T1, T2}"/> class.
        /// </summary>
        /// <param name="first">First element</param>
        /// <param name="second">Second element</param>
        public Tuple(T1 first, T2 second)
        {
            Item1 = first;
            Item2 = second;
        }

        /// <summary>
        /// Gets the first element contained in the tuple.
        /// </summary>
        public T1 Item1 { get; }

        /// <summary>
        /// Gets the second element contained in the tuple.
        /// </summary>
        public T2 Item2 { get; }
    }
}
