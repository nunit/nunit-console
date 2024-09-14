// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// TestEventDispatcher is used to send test events to a number of listeners
    /// </summary>
    public class TestEventDispatcher : MarshalByRefObject, ITestEventListener
    {
        private object _eventLock = new object();

        public TestEventDispatcher()
        {
            Listeners = new List<ITestEventListener>();
        }

        public IList<ITestEventListener>Listeners { get; private set; }

        public void OnTestEvent(string report)
        {
            const string badChar = "\xffff";

            lock (_eventLock)
            {
                // TODO: Review whether we need more checks
                report = report.Replace(badChar, "?");

                foreach (var listener in Listeners)
                    listener.OnTestEvent(report);
            }
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
