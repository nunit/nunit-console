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

        #region MarshalByRefObject Overrides

        public override object InitializeLifetimeService()
        {
            return null;
        }

        #endregion

        #region ICallbackEventHandler Members

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

        #endregion

        #region Helper Methods

        private void ReportProgress(string state)
        {
            _listener.OnTestEvent(state);
        }

        private bool IsFinalResult(string eventArgument)
        {
            // Eliminate all events except for test-suite
            if (!eventArgument.StartsWith("<test-suite"))
                return false;

            // Eliminate suites that do not represent an assembly
            if (!eventArgument.Contains("type=\"Assembly\""))
                return false;

            // Heuristic: only final results may have an environment element
            if (eventArgument.Contains("<environment"))
                return true;

            // Heuristic: only final results may have a settings element
            if (eventArgument.Contains("<settings"))
                return true;

            // Heuristic: only final results have nested suites
            return eventArgument.IndexOf("<test-suite", 12) > 0;
        }

        #endregion

        #region Nested NullListener class
        class NullListener : ITestEventListener
        {
            public void OnTestEvent(string report)
            {
            }
        }
        #endregion
    }
}
#endif