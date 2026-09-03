// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt
using NUnit.Engine;
using System;
using System.Diagnostics;
using System.IO;

namespace NUnit
{
    /// <summary>
    /// A trace listener that writes to a separate file per domain
    /// and process using it.
    /// </summary>
    public class InternalTraceWriter
    {
        private const string TIME_FORMAT = "HH:mm:ss.fff";
        private const string TRACE_FORMAT = "{0} {1,-5} [{2,2}] {3}: {4}";

        private TextWriter? _writer;
        private readonly object _myLock = new object();

        /// <summary>
        /// Gets a flag indicating whether the InternalTraceWriter is initialized.
        /// </summary>
        public bool Initialized { get; set; } = false;

        /// <summary>
        /// The current log file path. This is set to a default value, but may be changed if requested.
        /// </summary>
        public string LogPath { get; private set; } = $"InternalTrace_{Process.GetCurrentProcess().Id}.log";

        /// <summary>
        /// Gets the default tace level used by this writer.
        /// </summary>
        public InternalTraceLevel DefaultTraceLevel { get; private set; }

        #region Construction and Initialization

        /// <summary>
        /// Construct an InternalTraceWriter that writes to a file.
        /// </summary>
        internal InternalTraceWriter()
        {
        }

        /// <summary>
        /// Construct an InternalTraceWriter that writes to a
        /// TextWriter provided by the caller.
        /// </summary>
        public InternalTraceWriter(TextWriter writer)
        {
            _writer = writer;
        }

        public void Initialize(string logName, InternalTraceLevel level)
        {
            LogPath = logName;
            DefaultTraceLevel = level;
            Initialized = true;
        }

        public void Initialize(InternalTraceLevel level)
        {
            DefaultTraceLevel = level;
            Initialized = true;
        }

        #endregion

        public Logger GetLogger(string name, InternalTraceLevel level, bool echo)
            => new Logger(name, level, this, echo);

        public void WriteLogEntry(Logger logger, InternalTraceLevel level, string message, bool echoToConsole)
        {
            WriteLog(logger.Name, level, message, echoToConsole);
        }

        public void WriteLog(string loggerName, InternalTraceLevel level, string message, bool echoToConsole = false)
        {
#if NET20 || NET30 || NET35 || NET40
            int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
#else
            int threadId = Environment.CurrentManagedThreadId;
#endif
            string formattedMessage = string.Format(TRACE_FORMAT,
                DateTime.Now.ToString(TIME_FORMAT),
                level,
                threadId,
                loggerName,
                message);

            WriteLine(formattedMessage);

            if (echoToConsole)
                Console.WriteLine(formattedMessage);
        }

        /// <summary>
        /// Writes a string followed by a line terminator to the text string or stream.
        /// </summary>
        /// <param name="value">The string to write. If <paramref name="value" /> is null,
        /// only the line terminator is written.</param>
        public void WriteLine(string? value)
        {
            lock (_myLock)
            {
                //// We are about to write. If needed do just-in-time initialization of the writer.
                //if (!Initialized)
                //    Initialize();

                if (_writer is null)
                    _writer = new StreamWriter(LogPath, true) { AutoFlush = true };
                _writer.WriteLine(value);
            }
        }

        /// <summary>
        /// Flushes and closes the writer, ensuring that the log file is comlete.
        /// </summary>
        protected void Close()
        {
            lock (_myLock)
            {
                if (_writer is not null)
                {
                    _writer.Flush();
                    _writer.Dispose();
                    _writer = null!;
                }
            }
        }
    }
}
