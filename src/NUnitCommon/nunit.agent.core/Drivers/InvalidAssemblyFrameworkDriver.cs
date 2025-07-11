// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Common;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Drivers
{
    public sealed class InvalidAssemblyFrameworkDriver : IFrameworkDriver
    {
        private readonly string _name;
        private readonly string _fullname;
        private readonly string _message;
        private readonly string _type;

        private const string RUNSTATE = "NotRunnable";
        private const string RESULT = "FAILED";
        private const string LABEL = "INVALID";

        public InvalidAssemblyFrameworkDriver(string assemblyPath, string id, string message)
        {
            _name = Escape(Path.GetFileName(assemblyPath));
            _fullname = Escape(Path.GetFullPath(assemblyPath));
            _message = Escape(message);
            _type = PathUtils.IsAssemblyFileType(assemblyPath) ? "Assembly" : "Unknown";
            ID = id;
        }

        public string ID { get; }

        public string Load(string assemblyPath, IDictionary<string, object> settings)
        {
            return GetLoadResult();
        }

        public int CountTestCases(string filter)
        {
            return 0;
        }

        public string Run(ITestEventListener? listener, string filter)
        {
            return GetRunResult();
        }

        public void RunAsync(ITestEventListener? listener, string filter)
        {
            listener?.OnTestEvent(GetRunResult());
        }

        public string Explore(string filter)
        {
            return GetLoadResult();
        }

        public void StopRun(bool force)
        {
        }

        private static string Escape(string original)
        {
            return original
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private string GetLoadResult() =>
            $"<test-suite type='{_type}' id='{TestID}' name='{_name}' fullname='{_fullname}' testcasecount='0' runstate='{RUNSTATE}'>" +
                "<properties>" +
                    $"<property name='_SKIPREASON' value='{_message}'/>" +
                "</properties>" +
            "</test-suite>";

        private string GetRunResult() =>
            $"<test-suite type='{_type}' id='{TestID}' name='{_name}' fullname='{_fullname}' testcasecount='0' runstate='{RUNSTATE}' result='{RESULT}' label='{LABEL}'>" +
                "<properties>" +
                    $"<property name='_SKIPREASON' value='{_message}'/>" +
                "</properties>" +
                "<reason>" +
                    $"<message>{_message}</message>" +
                "</reason>" +
            "</test-suite>";

        private string TestID => ID + "-1";
    }
}
