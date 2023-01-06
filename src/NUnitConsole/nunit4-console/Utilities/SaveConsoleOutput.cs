// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;

namespace NUnit.ConsoleRunner.Utilities
{
    /// <summary>
    /// Saves Console.Out and Console.Error and restores them when the object
    /// is destroyed
    /// </summary>
    public sealed class SaveConsoleOutput : IDisposable
    {
        private readonly TextWriter _savedOut = Console.Out;
        private readonly TextWriter _savedError = Console.Error;

        /// <summary>
        /// Restores Console.Out and Console.Error
        /// </summary>
        public void Dispose()
        {
            Console.SetOut(_savedOut);
            Console.SetError(_savedError);
        }
    }
}
