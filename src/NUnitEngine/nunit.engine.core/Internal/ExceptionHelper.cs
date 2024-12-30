// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Engine;

#nullable enable

namespace NUnit.Common
{
    internal static class ExceptionHelper
    {
        /// <summary>
        /// Builds up a message, using the Message field of the specified exception
        /// as well as any InnerExceptions.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>A combined message string.</returns>
        public static string BuildMessage(Exception exception)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0} : ", exception.GetType());
            sb.Append(GetExceptionMessage(exception));

            foreach (Exception inner in FlattenExceptionHierarchy(exception))
            {
                sb.Append(Environment.NewLine);
                sb.Append("  ----> ");
                sb.AppendFormat("{0} : ", inner.GetType());
                sb.Append(GetExceptionMessage(inner));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds up a message, using the Message field of the specified exception
        /// as well as any InnerExceptions.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>A combined stack trace.</returns>
        public static string BuildMessageAndStackTrace(Exception exception)
        {
            var sb = new StringBuilder("--");
            sb.AppendLine(exception.GetType().Name);
            sb.AppendLine(GetExceptionMessage(exception));
            sb.AppendLine(GetSafeStackTrace(exception));

            foreach (Exception inner in FlattenExceptionHierarchy(exception))
            {
                sb.AppendLine("--");
                sb.AppendLine(inner.GetType().Name);
                sb.AppendLine(GetExceptionMessage(inner));
                sb.AppendLine(GetSafeStackTrace(inner));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the stack trace of the exception. If no stack trace
        /// is provided, returns "No stack trace available".
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>A string representation of the stack trace.</returns>
        private static string GetSafeStackTrace(Exception exception)
        {
            const string NoStackTrace = "No stack trace available";

            try
            {
                return exception.StackTrace ?? NoStackTrace;
            }
            catch (Exception)
            {
                return NoStackTrace;
            }
        }

        private static List<Exception> FlattenExceptionHierarchy(Exception exception)
        {
            var result = new List<Exception>();

            var unloadException = exception as NUnitEngineUnloadException;
            if (unloadException?.AggregatedExceptions != null)
            {
                result.AddRange(unloadException.AggregatedExceptions);

                foreach (var aggregatedException in unloadException.AggregatedExceptions)
                    result.AddRange(FlattenExceptionHierarchy(aggregatedException));
            }

            var reflectionException = exception as ReflectionTypeLoadException;
            if (reflectionException != null && reflectionException.LoaderExceptions != null)
            {
                result.AddRange(reflectionException.LoaderExceptions.WhereNotNull());

                foreach (var innerException in reflectionException.LoaderExceptions.WhereNotNull())
                    result.AddRange(FlattenExceptionHierarchy(innerException));
            }

            if (exception.InnerException != null)
            {
                result.Add(exception.InnerException);
                result.AddRange(FlattenExceptionHierarchy(exception.InnerException));
            }

            return result;
        }

        private static string GetExceptionMessage(Exception ex)
        {
            if (string.IsNullOrEmpty(ex.Message))
            {
                // Special handling for Mono 5.0, which returns an empty message
                var fnfEx = ex as System.IO.FileNotFoundException;
                return fnfEx != null
                    ? "Could not load assembly. File not found: " + fnfEx.FileName
                    : "No message provided";
            }

            return ex.Message;
        }

        /// <summary>
        /// Returns the specified sequence with all null references removed.
        /// </summary>
        /// <typeparam name="T">The type of items in source.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to filter.</param>
        /// <returns>
        /// The sequence with all null references removed.
        /// </returns>
        private static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
            where T : class
        {
            return source.Where(x => x is not null)!;
        }
    }
}
