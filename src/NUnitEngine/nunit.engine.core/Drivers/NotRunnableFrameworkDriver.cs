// ***********************************************************************
// Copyright (c) 2014 Charlie Poole, Rob Prouse
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

using System.Collections.Generic;
using System.IO;
using NUnit.Engine.Extensibility;
using System.Reflection;

namespace NUnit.Engine.Drivers
{
    public abstract class NotRunnableFrameworkDriver : IFrameworkDriver
    {
        private const string LOAD_RESULT_FORMAT =
            "<test-suite type='{0}' id='{1}' name='{2}' fullname='{3}' testcasecount='0' runstate='{4}'>" +
                "<properties>" +
                    "<property name='_SKIPREASON' value='{5}'/>" +
                "</properties>" +
            "</test-suite>";

        private const string RUN_RESULT_FORMAT =
            "<test-suite type='{0}' id='{1}' name='{2}' fullname='{3}' testcasecount='0' runstate='{4}' result='{5}' label='{6}'>" +
                "<properties>" +
                    "<property name='_SKIPREASON' value='{7}'/>" +
                "</properties>" +
                "<reason>" +
                    "<message>{7}</message>" +
                "</reason>" +
            "</test-suite>";

        private string _name;
        private string _fullname;
        private string _message;
        private string _type;

        protected string _runstate;
        protected string _result;
        protected string _label;

        public NotRunnableFrameworkDriver(string assemblyPath, string message)
        {
            _name = Escape(Path.GetFileName(assemblyPath));
            _fullname = Escape(Path.GetFullPath(assemblyPath));
            _message = Escape(message);
            _type = new List<string> { ".dll", ".exe" }.Contains(Path.GetExtension(assemblyPath)) ? "Assembly" : "Unknown";
        }

        public string ID { get; set; }

        
        public string Load(string assemblyPath, IDictionary<string, object> settings)
        {
            return GetLoadResult();
        }

        public int CountTestCases(string filter)
        {
            return 0;
        }

        public string Run(ITestEventListener listener, string filter)
        {
            return string.Format(RUN_RESULT_FORMAT, 
                _type, TestID, _name, _fullname, _runstate, _result, _label, _message);
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

        private string GetLoadResult()
        {
            return string.Format(
                LOAD_RESULT_FORMAT,
                _type, TestID, _name, _fullname, _runstate, _message);
        }

        private string TestID
        {
            get
            {
                return string.IsNullOrEmpty(ID)
                    ? "1"
                    : ID + "-1";
            }
        }
    }

    public class InvalidAssemblyFrameworkDriver :NotRunnableFrameworkDriver
    {
        public InvalidAssemblyFrameworkDriver(string assemblyPath, string message)
            : base(assemblyPath, message)
        {
            _runstate = "NotRunnable";
            _result = "Failed";
            _label = "Invalid";
        }
    }

    public class SkippedAssemblyFrameworkDriver : NotRunnableFrameworkDriver
    {
        public SkippedAssemblyFrameworkDriver(string assemblyPath)
            : base(assemblyPath, "Skipping non-test assembly")
        {
            _runstate = "Runnable";
            _result = "Skipped";
            _label = "NoTests";
        }
    }
}
