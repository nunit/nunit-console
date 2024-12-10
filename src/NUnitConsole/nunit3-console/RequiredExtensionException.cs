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
        private static string Message1(string extensionName) => $"Required extension {extensionName} is not installed.";
        private static string Message2(string extensionName, string option) => $"Option {option} specified but {extensionName} is not installed.";
        
        /// <summary>
         /// Construct with the name of an extension
         /// </summary>
        public RequiredExtensionException(string extensionName) : base(Message1(extensionName))
        {
        }

        /// <summary>
        /// Construct with the name of an extension and the command-line option requiring that extension.
        /// </summary>
        public RequiredExtensionException(string extensionName, string option) : base(Message2(extensionName, option))
        {
        }

        /// <summary>
        /// Construct with the name of an extension and inner exception
        /// </summary>
        public RequiredExtensionException(string extensionName, Exception innerException)
            : base(Message1(extensionName), innerException)
        {
        }

        /// <summary>
        /// Construct with the name of an extension, a command-line option and inner exception
        /// </summary>
        public RequiredExtensionException(string extensionName, string option, Exception innerException)
            : base(Message2(extensionName, option), innerException)
        {
        }
    }
}
