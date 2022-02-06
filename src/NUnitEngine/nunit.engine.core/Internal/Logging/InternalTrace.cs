// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Internal
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
        private static InternalTraceWriter _traceWriter;

        public static InternalTraceLevel DefaultTraceLevel { get; private set; }

        /// <summary>
        /// Gets a flag indicating whether the InternalTrace is initialized
        /// </summary>
        public static bool Initialized { get; private set; }

        /// <summary>
        /// Initialize the internal trace facility using the name of the log
        /// to be written to and the trace level.
        /// </summary>
        /// <param name="logName">The log name</param>
        /// <param name="level">The trace level</param>
        public static void Initialize(string logName, InternalTraceLevel level)
        {
            if (!Initialized)
            {
                DefaultTraceLevel = level;

                if (_traceWriter == null && DefaultTraceLevel > InternalTraceLevel.Off)
                {
                    _traceWriter = new InternalTraceWriter(logName);
                    _traceWriter.WriteLine("InternalTrace: Initializing at level {0}", DefaultTraceLevel);
                }

                Initialized = true;
            }
            else
                _traceWriter.WriteLine("InternalTrace: Ignoring attempted re-initialization at level {0}", level);
        }

        /// <summary>
        /// Get a named Logger specifying the TraceLevel
        /// </summary>
        public static Logger GetLogger(string name, InternalTraceLevel level)
        {
            return new Logger(name, level, _traceWriter);
        }

        /// <summary>
        /// Get a logger named for a particular Type, specifying the TraceLevel.
        /// </summary>
        public static Logger GetLogger(Type type, InternalTraceLevel level)
        {
            return GetLogger(type.FullName, level);
        }

        /// <summary>
        /// Get a named Logger using the default TraceLevel
        /// </summary>
        public static Logger GetLogger(string name)
        {
            return new Logger(name, DefaultTraceLevel, _traceWriter);
        }

        /// <summary>
        /// Get a logger named for a particular Type using the default TraceLevel.
        /// </summary>
        public static Logger GetLogger(Type type)
        {
            return GetLogger(type.FullName, DefaultTraceLevel);
        }
    }
}
