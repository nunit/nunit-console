// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
namespace NUnit.Engine.Internal
{
    /// <summary>
    /// Extension methods to get some Linq-feeling in .NET 2.0 projects.
    /// </summary>
    internal static class IEnumerableExtensions
    {
        /// <summary>
        /// Gets a value indicating whether the enumeration contains at least one item or not.
        /// </summary>
        /// <typeparam name="T">The type of objects contained in the enumeration.</typeparam>
        /// <param name="enumeration">The enumeration.</param>
        /// <returns><see langword="true"/> if <paramref name="enumeration"/> contains at least one item; otherwhise, <see langword="false"/>.</returns>
        public static bool Any<T>(this IEnumerable<T> enumeration)
        {
            foreach (var _ in enumeration)
            {
                return true;
            }

            return false;
        }
    }
}
