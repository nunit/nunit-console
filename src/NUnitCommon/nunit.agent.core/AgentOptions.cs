// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine;
using System;
using System.Collections.Generic;
using System.IO;

namespace NUnit.Agents
{
    /// <summary>
    /// All agents, either built-in or pluggable, must be able to
    /// handle the options defined in this class. In some cases,
    /// it may be permissible to ignore them but they should never
    /// give rise to an error.
    /// </summary>
    public class AgentOptions
    {
        private static readonly char[] DELIMS = new[] { '=', ':' };
        // Dictionary containing valid options with bool value true if a value is required.
        private static readonly Dictionary<string, bool> VALID_OPTIONS = new Dictionary<string, bool>();

        static AgentOptions()
        {
            VALID_OPTIONS["agentId"] = true;
            VALID_OPTIONS["agencyUrl"] = true;
            VALID_OPTIONS["debug-agent"] = false;
            VALID_OPTIONS["debug-tests"] = false;
            VALID_OPTIONS["trace"] = true;
            VALID_OPTIONS["pid"] = true;
            VALID_OPTIONS["work"] = true;
        }

        public AgentOptions(params string[] args)
        {
            int index;
            for (index = 0; index < args.Length; index++)
            {
                string arg = args[index];

                if (IsOption(arg))
                {
                    var option = arg.Substring(2);
                    var delim = option.IndexOfAny(DELIMS);
                    var opt = option;
                    string? val = null;
                    if (delim > 0)
                    {
                        opt = option.Substring(0, delim);
                        val = option.Substring(delim + 1);
                    }

                    // Simultaneously check that the option is valid and determine if it takes an argument
                    if (!VALID_OPTIONS.TryGetValue(opt, out bool optionTakesValue))
                        throw new Exception($"Invalid argument: {arg}");

                    if (optionTakesValue)
                    {
                        if (val == null && index + 1 < args.Length)
                            val = args[++index];

                        if (val == null)
                            throw new Exception($"Option requires a value: {arg}");
                    }
                    else if (delim > 0)
                    {
                        throw new Exception($"Option does not take a value: {arg}");
                    }

                    if (opt == "agentId")
                        AgentId = new Guid(GetArgumentValue(arg));
                    else if (opt == "agencyUrl")
                        AgencyUrl = GetArgumentValue(arg);
                    else if (opt == "debug-agent")
                        DebugAgent = true;
                    else if (opt == "debug-tests")
                        DebugTests = true;
                    else if (opt == "trace")
                        TraceLevel = (InternalTraceLevel)Enum.Parse(typeof(InternalTraceLevel), val.ShouldNotBeNull());
                    else if (opt == "pid")
                        AgencyPid = val.ShouldNotBeNull();
                    else if (opt == "work")
                        WorkDirectory = val.ShouldNotBeNull();
                    else
                        throw new Exception($"Invalid argument: {arg}");
                }
                else if (File.Exists(arg))
                    Files.Add(arg);
                else
                    throw new FileNotFoundException($"FileNotFound: {arg}");
            }

            if (Files.Count > 1)
                throw new ArgumentException($"Only one file argument is allowed but {Files.Count} were supplied");

            string GetArgumentValue(string argument)
            {
                var delim = argument.IndexOfAny(DELIMS);

                if (delim > 0)
                    return argument.Substring(delim + 1);

                if (index + 1 < args.Length)
                    return args[++index];

                throw new Exception($"Option requires a value: {argument}");
            }
        }

        public Guid AgentId { get; } = Guid.Empty;
        public string AgencyUrl { get; } = string.Empty;
        public string AgencyPid { get; } = string.Empty;
        public bool DebugTests { get; } = false;
        public bool DebugAgent { get; } = false;
        public InternalTraceLevel TraceLevel { get; } = InternalTraceLevel.Off;
        public string WorkDirectory { get; } = string.Empty;

        public List<string> Files { get; } = new List<string>();

        private static bool IsOption(string arg)
        {
            return arg.StartsWith("--", StringComparison.Ordinal);
        }
    }
}
