// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NUnit.Engine.Runners
{
    public abstract class NotRunnableTestRunner : ITestEngineRunner
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

        public NotRunnableTestRunner(string assemblyPath, string message)
        {
            _name = Escape(Path.GetFileName(assemblyPath));
            _fullname = Escape(Path.GetFullPath(assemblyPath));
            _message = Escape(message);
            _type = new List<string> { ".dll", ".exe" }.Contains(Path.GetExtension(assemblyPath)) ? "Assembly" : "Unknown";
        }

        public string ID { get; set; }

        TestEngineResult ITestEngineRunner.Load()
        {
            return GetLoadResult();
        }

        void ITestEngineRunner.Unload()
        {
        }

        TestEngineResult ITestEngineRunner.Reload()
        {
            return GetLoadResult();
        }

        int ITestEngineRunner.CountTestCases(TestFilter filter)
        {
            return 0;
        }

        TestEngineResult ITestEngineRunner.Run(ITestEventListener listener, TestFilter filter)
        {
            return new TestEngineResult(string.Format(RUN_RESULT_FORMAT,
                _type, TestID, _name, _fullname, _runstate, _result, _label, _message));
        }

        AsyncTestEngineResult ITestEngineRunner.RunAsync(ITestEventListener listener, TestFilter filter)
        {
            throw new NotImplementedException();
        }

        void ITestEngineRunner.StopRun(bool force)
        {
        }

        TestEngineResult ITestEngineRunner.Explore(TestFilter filter)
        {
            return GetLoadResult();
        }

        void IDisposable.Dispose()
        {
            // Nothing to do here
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

        private TestEngineResult GetLoadResult()
        {
            return new TestEngineResult(string.Format(
                LOAD_RESULT_FORMAT,
                _type, TestID, _name, _fullname, _runstate, _message));
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

    public class UnmanagedExecutableTestRunner : NotRunnableTestRunner
    {
        public UnmanagedExecutableTestRunner(string assemblyPath)
            : base(assemblyPath, "Unmanaged libraries or applications are not supported")
        { }
    }
}
