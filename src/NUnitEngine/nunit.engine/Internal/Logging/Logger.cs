// ***********************************************************************
// Copyright (c) 2008-2013 Charlie Poole, Rob Prouse
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
using System.IO;

namespace NUnit.Engine.Internal
{
    /// <summary>
    /// Provides internal logging to the NUnit framework
    /// </summary>
    public class Logger : ILogger
    {
        private const string TimeFmt = "HH:mm:ss.fff";
        private const string TraceFmt = "{0} {1,-5} [{2,2}] {3}: {4}";

        private readonly string _name;
        private readonly InternalTraceLevel _maxLevel;
        private readonly TextWriter _writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <param name="fullName">The name.</param>
        /// <param name="level">The log level.</param>
        /// <param name="writer">The writer where logs are sent.</param>
        public Logger(string fullName, InternalTraceLevel level, TextWriter writer)
        {
            _maxLevel = level;
            _writer = writer;

            var index = fullName.LastIndexOf('.');
            _name = index >= 0 ? fullName.Substring(index + 1) : fullName;
        }

        #region Error
        /// <summary>
        /// Logs the message at error level.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Error(string message)
        {
            Log(InternalTraceLevel.Error, message);
        }

        /// <summary>
        /// Logs the message at error level.
        /// </summary>
        /// <param name="format">The message.</param>
        /// <param name="args">The message arguments.</param>
        public void Error(string format, params object[] args)
        {
            Log(InternalTraceLevel.Error, format, args);
        }

        #endregion

        #region Warning
        /// <summary>
        /// Logs the message at warm level.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Warning(string message)
        {
            Log(InternalTraceLevel.Warning, message);
        }

        /// <summary>
        /// Logs the message at warning level.
        /// </summary>
        /// <param name="format">The message.</param>
        /// <param name="args">The message arguments.</param>
        public void Warning(string format, params object[] args)
        {
            Log(InternalTraceLevel.Warning, format, args);
        }
        #endregion

        #region Info
        /// <summary>
        /// Logs the message at info level.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Info(string message)
        {
            Log(InternalTraceLevel.Info, message);
        }

        /// <summary>
        /// Logs the message at info level.
        /// </summary>
        /// <param name="format">The message.</param>
        /// <param name="args">The message arguments.</param>
        public void Info(string format, params object[] args)
        {
            Log(InternalTraceLevel.Info, format, args);
        }
        #endregion

        #region Debug
        /// <summary>
        /// Logs the message at debug level.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Debug(string message)
        {
            Log(InternalTraceLevel.Verbose, message);
        }

        /// <summary>
        /// Logs the message at debug level.
        /// </summary>
        /// <param name="format">The message.</param>
        /// <param name="args">The message arguments.</param>
        public void Debug(string format, params object[] args)
        {
            Log(InternalTraceLevel.Verbose, format, args);
        }
        #endregion

        #region Helper Methods

        private void Log(InternalTraceLevel level, string message)
        {
            if (_writer != null && _maxLevel >= level)
                WriteLog(level, message);
        }

        private void Log(InternalTraceLevel level, string format, params object[] args)
        {
            if (_maxLevel >= level)
                WriteLog(level, string.Format(format, args));
        }

        private void WriteLog(InternalTraceLevel level, string message)
        {
            _writer.WriteLine(TraceFmt,
                DateTime.Now.ToString(TimeFmt),
                level,
#if NET20
                System.Threading.Thread.CurrentThread.ManagedThreadId,
#else
                Environment.CurrentManagedThreadId,
#endif
                _name, message);
        }

        #endregion
    }
}
