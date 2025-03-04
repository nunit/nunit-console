// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.ConsoleRunner
{
    /// <summary>
    /// RequiredExtensionException is thrown when the console runner is executed
    /// with a command-line option that requires a particular extension and
    /// that extension has not been installed.
    /// </summary>
    public class RequiredExtensionException : Exception
    {
        private static string BuildMessage(string extensionName) => $"Required extension '{extensionName}' is not installed.";

        /// <summary>
        /// Construct with the name of an extension
        /// </summary>
        public RequiredExtensionException(string extensionName) : base(BuildMessage(extensionName))
        {
        }

        /// <summary>
        /// Construct with the name of an extension and inner exception
        /// </summary>
        public RequiredExtensionException(string extensionName, Exception innerException)
            : base(BuildMessage(extensionName), innerException)
        {
        }
    }
}
