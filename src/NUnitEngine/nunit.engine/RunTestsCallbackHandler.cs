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
using System.Diagnostics;
using System.Web.UI;

namespace NUnit.Engine
{
    public class RunTestsCallbackHandler : MarshalByRefObject, ICallbackEventHandler
    {
        private ITestEventListener _listener;

        public string Result { get; private set; }

        public RunTestsCallbackHandler(ITestEventListener listener)
        {
            _listener = listener ?? new NullListener();
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

        private void ReportProgress(string state)
        {
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