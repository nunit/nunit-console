// ***********************************************************************
// Copyright (c) 2010-2014 Charlie Poole, Rob Prouse
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

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Threading;
using System.Web.UI;
using System.Xml;

namespace NUnit.Engine
{
    public class RunTestsCallbackHandler : MarshalByRefObject, ICallbackEventHandler
    {
        private ITestEventListener _listener;
        private Version _frameworkVersion;

        private static readonly Version NUNIT_3_12 = new Version(3, 12);
        private const int WAIT_FOR_FORCED_TERMINATION = 5000;

        public string Result { get; private set; }

        public RunTestsCallbackHandler(ITestEventListener listener, Version frameworkVersion)
        {
            _listener = listener ?? new NullListener();
            _frameworkVersion = frameworkVersion;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public string GetCallbackResult()
        {
            throw new NotImplementedException();
        }

        public void RaiseCallbackEvent(string eventArgument)
        {
            if (IsFinalResult(eventArgument))
                Result = eventArgument;
            else
                ReportProgress(eventArgument);
        }

        private List<string> _activeSuiteIds = new List<string>();
        private Dictionary<string, XmlNode> _activeSuiteLookup = new Dictionary<string, XmlNode>();

        private const string CANCELLED_SUITE_FORMAT =
            "<test-suite type='{0}' id='{1}' name='{2}' fullname='{3}' testcasecount='0' result='Failed' label='Cancelled' />";

        public void ForcedCancellationCleanUp()
        {
            if (_activeSuiteIds.Count > 0)
            {
                SpinWait.SpinUntil(() => _activeSuiteIds.Count == 0, WAIT_FOR_FORCED_TERMINATION);

                // Notify termination of any remaining in-process suites
                int index = _activeSuiteIds.Count;

                while (index > 0)
                {
                    string id = _activeSuiteIds[--index];
                    XmlNode startNode = _activeSuiteLookup[id];

                    string type = startNode.GetAttribute("type");
                    string name = startNode.GetAttribute("name");
                    string fullname = startNode.GetAttribute("fullname");

                    _listener.OnTestEvent(string.Format(CANCELLED_SUITE_FORMAT, type, id, name, fullname));
                }
            }
        }

        private void ReportProgress(string state)
        {
            // The NUnit framework 3.0 through 3.12 does not give notice of termination of test
            // suites correctly after a user cancellation. Version 3.13 and later contain a fix
            // for this. For earlier versions, the driver does essentially the same thing as the
            // framework fix, in order to compensate for the error.
            //
            // Note: Check for start-suite and test-suite is for efficiency only. It avoids extra
            // overhead of creating an XmlNode for individual test cases.
            if (_frameworkVersion <= NUNIT_3_12 && (state.Contains("start-suite") || state.Contains("test-suite")))
            {
                var xmlNode = XmlHelper.CreateXmlNode(state);
                string id = xmlNode.GetAttribute("id");

                switch (xmlNode.Name)
                {
                    case "start-suite":
                        _activeSuiteIds.Add(id);
                        _activeSuiteLookup.Add(id, xmlNode);
                        break;
                    case "test-suite":
                        _activeSuiteIds.Remove(id);
                        _activeSuiteLookup.Remove(id);
                        break;
                }
            }

            _listener.OnTestEvent(state);
        }

        private bool IsFinalResult(string eventArgument)
        {
            // TODO: If we add a prefix to the final result in the next framework
            // release, then we can immediately recognize the final result but we
            // would need to continue to examine the non-final result in case the
            // the framework in use were an older version. Building in more knowledge
            // of framework versions is probably not a good idea, since it would be
            // potentially fragile as changes were made.

            // Eliminate all events except for test-suite
            if (!eventArgument.StartsWith("<test-suite", StringComparison.Ordinal))
                return false;
            // Return for all test cases, for example, 90% of events

            // Eliminate suites that do not represent an assembly
            if (!eventArgument.Contains("type=\"Assembly\""))
                return false; 
            // Return for all except the assembly result. Remaining code is only
            // executed twice per assembly.

            // Heuristic: only final results may have an environment element
            if (eventArgument.Contains("<environment"))
                return true;
            // For the actual final result, the current framework version returns
            // true at this point. Only older versions need further checking.

            // Heuristic: only final results may have a settings element
            if (eventArgument.Contains("<settings"))
                return true;

            // Heuristic: only final results have nested suites
            return eventArgument.IndexOf("<test-suite", 12, StringComparison.Ordinal) > 0;
        }

        class NullListener : ITestEventListener
        {
            public void OnTestEvent(string report)
            {
            }
        }
    }
}
#endif