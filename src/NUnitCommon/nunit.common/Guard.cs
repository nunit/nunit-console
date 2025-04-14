// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace NUnit
{
    /// <summary>
    /// Class used to guard against unexpected argument values
    /// or operations by throwing an appropriate exception.
    /// </summary>
    public static class Guard
    {
        /// <summary>
        /// Throws an exception if an argument is null
        /// </summary>
        /// <param name="value">The value to be tested</param>
        /// <param name="name">Compiler supplied parameter for the <paramref name="value"/> expression.</param>
        public static void ArgumentNotNull<T>(T value, [CallerArgumentExpression(nameof(value))] string name = "")
            where T : notnull
        {
            if (value is null)
                throw new ArgumentNullException(name);
        }

        /// <summary>
        /// Throws an exception if a result is null
        /// </summary>
        /// <typeparam name="T">The type of the <paramref name="result"/> to check.</typeparam>
        /// <param name="result">The value to check.</param>
        /// <param name="expression">Compiler supplied parameter for the <paramref name="result"/> expression.</param>
        /// <returns>Value of <paramref name="result"/> if not null, throws otherwise.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static T ShouldNotBeNull<T>(this T? result, [CallerArgumentExpression(nameof(result))] string expression = "")
            where T : class
        {
            if (result is null)
                throw new InvalidOperationException($"Result {expression} must not be null");

            return result;
        }

        /// <summary>
        /// Throws an exception if a string argument is null or empty
        /// </summary>
        /// <param name="value">The value to be tested</param>
        /// <param name="name">Compiler supplied parameter for the <paramref name="value"/> expression.</param>
        public static void ArgumentNotNullOrEmpty(string value, [CallerArgumentExpression(nameof(value))] string name = "")
        {
            ArgumentNotNull(value, name);

            if (value == string.Empty)
                throw new ArgumentException("Argument " + name + " must not be the empty string", name);
        }

        /// <summary>
        /// Throws an ArgumentOutOfRangeException if the specified condition is not met.
        /// </summary>
        /// <param name="condition">The condition that must be met</param>
        /// <param name="message">The exception message to be used</param>
        /// <param name="paramName">The name of the argument</param>
        public static void ArgumentInRange([DoesNotReturnIf(false)] bool condition, string message, string paramName)
        {
            if (!condition)
                throw new ArgumentOutOfRangeException(paramName, message);
        }

        /// <summary>
        /// Throws an ArgumentException if the specified condition is not met.
        /// </summary>
        /// <param name="condition">The condition that must be met</param>
        /// <param name="message">The exception message to be used</param>
        /// <param name="paramName">The name of the argument</param>
        public static void ArgumentValid([DoesNotReturnIf(false)] bool condition, string message, string paramName)
        {
            if (!condition)
                throw new ArgumentException(message, paramName);
        }

        /// <summary>
        /// Throws an InvalidOperationException if the specified condition is not met.
        /// </summary>
        /// <param name="condition">The condition that must be met</param>
        /// <param name="message">The exception message to be used</param>
        public static void OperationValid([DoesNotReturnIf(false)] bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
