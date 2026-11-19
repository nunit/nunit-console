// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.IO;
using System.Xml.Linq;

namespace NUnit.Engine.TestHelpers
{
    /// <summary>
    /// Reads a published readiness receipt without treating a temporary file lock as readiness.
    /// </summary>
    internal static class ProviderServerReadiness
    {
        private const int SharingViolation = unchecked((int)0x80070020);
        private const int LockViolation = unchecked((int)0x80070021);

        /// <summary>
        /// Returns a complete receipt once it can be read. The caller retains its original
        /// readiness deadline and continues to check whether the child process has exited.
        /// </summary>
        /// <param name="path">The published receipt filename.</param>
        /// <returns>The receipt, or null while the file is absent or temporarily locked.</returns>
        /// <remarks>
        /// Malformed XML and other I/O failures propagate. A readable receipt still needs
        /// the caller's process-identity and endpoint validation.
        /// </remarks>
        internal static XElement? ReadIfAvailable(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                return XElement.Load(path);
            }
            catch (IOException exception) when (exception.HResult == SharingViolation || exception.HResult == LockViolation)
            {
                return null;
            }
        }
    }
}
