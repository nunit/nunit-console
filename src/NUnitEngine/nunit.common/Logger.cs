// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using NUnit.Engine;

namespace NUnit
{
    /// <summary>
    /// Provides internal logging to the NUnit framework
    /// </summary>
    public class Logger
    {
        private const string TimeFmt = "HH:mm:ss.fff";
        private const string TraceFmt = "{0} {1,-5} [{2,2}] {3}: {4}";

        private readonly string _name;
        private readonly Func<TextWriter> _getWriterFn;
        private readonly Func<InternalTraceLevel> _getLevelFn;

        public InternalTraceLevel TraceLevel => _getLevelFn.Invoke();

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <param name="fullName">The name.</param>
        /// <param name="getLevelFn">The log level.</param>
        /// <param name="getWriterFn">The writer where logs are sent.</param>
        public Logger(string fullName, Func<InternalTraceLevel> getLevelFn, Func<TextWriter> getWriterFn)
        {
            _getLevelFn = getLevelFn;
            _getWriterFn = getWriterFn;

            var index = fullName.LastIndexOf('.');
            _name = index >= 0 ? fullName.Substring(index + 1) : fullName;
        }

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

        private void Log(InternalTraceLevel level, string message)
        {
            if (TraceLevel >= level)
                WriteLog(level, message);
        }

        private void Log(InternalTraceLevel level, string format, params object[] args)
        {
            if (TraceLevel >= level)
                WriteLog(level, string.Format(format, args));
        }

        private void WriteLog(InternalTraceLevel level, string message)
        {
            _getWriterFn.Invoke().WriteLine(TraceFmt,
                DateTime.Now.ToString(TimeFmt),
                level,
#if NET20
                System.Threading.Thread.CurrentThread.ManagedThreadId,
#else
                Environment.CurrentManagedThreadId,
#endif
                _name, message);
        }
    }
}
