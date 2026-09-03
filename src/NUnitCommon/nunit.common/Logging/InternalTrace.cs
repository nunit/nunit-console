// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Engine;

namespace NUnit
{
    /// <summary>
    /// InternalTrace provides facilities for tracing the execution
    /// of the NUnit framework. Tests and classes under test may make use
    /// of Console writes, System.Diagnostics.Trace or various loggers and
    /// NUnit itself traps and processes each of them. For that reason, a
    /// separate internal trace is needed.
    ///
    /// Note:
    /// InternalTrace uses a global lock to allow multiple threads to write
    /// trace messages. This can easily make it a bottleneck so it must be
    /// used sparingly. Keep the trace Level as low as possible and only
    /// insert InternalTrace writes where they are needed.
    /// TODO: add some buffering and a separate writer thread as an option.
    /// TODO: figure out a way to turn on trace in specific classes only.
    /// </summary>
    public static class InternalTrace
    {
        /// <summary>
        /// The InternalTraceWriter used to write trace messages, created here as a singleton.
        /// It is initialized when the InternalTrace is initialized.
        /// </summary>
        private static readonly InternalTraceWriter _traceWriter = new InternalTraceWriter();

        /// <summary>
        /// Gets a flag indicating whether the InternalTrace is initialized
        /// </summary>
        public static bool Initialized => _traceWriter.Initialized;

        /// <summary>
        /// Gets the default trace level used by the writer.
        /// </summary>
        public static InternalTraceLevel DefaultTraceLevel => _traceWriter.DefaultTraceLevel;

        /// <summary>
        /// Initialize the internal trace facility using the name of the log
        /// to be written to and the trace level.
        /// </summary>
        /// <param name="logName">The log name</param>
        /// <param name="level">The trace level</param>
        public static void Initialize(string logName, InternalTraceLevel level)
            => _traceWriter.Initialize(logName, level);

        /// <summary>
        /// Initialize the trace specifying only the trace level.
        /// The log name will be set to a default value.
        /// </summary>
        /// <param name="level"></param>
        public static void Initialize(InternalTraceLevel level)
            => _traceWriter.Initialize(level);

        /// <summary>
        /// Get a named Logger specifying the TraceLevel
        /// </summary>
        public static Logger GetLogger(string name, InternalTraceLevel level)
        {
            return GetLogger(name, level, false);
        }

        /// <summary>
        /// Get a logger named for a particular Type, specifying the TraceLevel.
        /// </summary>
        public static Logger GetLogger(Type type, InternalTraceLevel level)
        {
            return GetLogger(type.FullName ?? type.Name, level);
        }

        /// <summary>
        /// Get a named Logger using the default TraceLevel
        /// </summary>
        public static Logger GetLogger(string name)
        {
            return new Logger(name, InternalTraceLevel.Default, _traceWriter);
        }

        /// <summary>
        /// Get a logger named for a particular Type using the default TraceLevel.
        /// </summary>
        public static Logger GetLogger(Type type)
        {
            return GetLogger(type.FullName ?? type.Name);
        }

        /// <summary>
        /// Get a Logger specifying the log file name and optionally the trace level and echo flag.
        /// </summary>
        public static Logger GetLogger(string name, InternalTraceLevel level = InternalTraceLevel.Default, bool echo = false)
            => _traceWriter.GetLogger(name, level, echo);
        /// <summary>
        /// Get a logger named for a particular Type, specifying the TraceLevel.
        /// </summary>
        public static Logger GetLogger(Type type, InternalTraceLevel level = InternalTraceLevel.Default, bool echo = false)
            => _traceWriter.GetLogger(type.FullName ?? type.Name, level, echo);
    }
}
