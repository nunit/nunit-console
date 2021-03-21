// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using System.Threading;
using System.Xml;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// Test Frameworks, don't always handle cancellation. Those that do may
    /// not always handle it correctly. In fact, even NUnit itself, through 
    /// at least release 3.12, fails to cancel a test that's in an infinite
    /// loop. In addition, it's possible for user code to defeat cancellation,
    /// even forced cancellation or kill.
    /// 
    /// The engine needs to protect itself from such problems by assuring that
    /// any test for which cancellation is requested is fully cancelled, with
    /// nothing left behind. Further, it needs to generate notifications to
    /// tell the runner what happened.
    /// 
    /// WorkItemTracker examines test events and keeps track of those tests
    /// that have started but not yet finished. It implements ITestEventListener
    /// in order to capture events. It allows waiting for all items to complete.
    /// Once the test has been cancelled, it provide notifications to the runner
    /// so the information may be displayed.
    /// </summary>
    internal class WorkItemTracker : ITestEventListener
    {
        private List<XmlNode> _itemsInProcess = new List<XmlNode>();
        private ManualResetEvent _allItemsComplete = new ManualResetEvent(false);
        private object _trackerLock = new object();
        
        public void Clear()
        {
            lock (_trackerLock)
            {
                _itemsInProcess.Clear();
                _allItemsComplete.Reset();
            }
        }

        public bool WaitForCompletion(int millisecondsTimeout)
        {
            return _allItemsComplete.WaitOne(millisecondsTimeout);
        }

        public void IssuePendingNotifications(ITestEventListener listener)
        {
            lock (_trackerLock)
            {
                int count = _itemsInProcess.Count;

                // Signal completion of all pending suites, in reverse order
                while (count > 0)
                    listener.OnTestEvent(CreateNotification(_itemsInProcess[--count]));
            }
        }

        private static string CreateNotification(XmlNode startElement)
        {
            bool isSuite = startElement.Name == "start-suite";

            XmlNode notification = XmlHelper.CreateTopLevelElement(isSuite ? "test-suite" : "test-case");
            if (isSuite)
                notification.AddAttribute("type", startElement.GetAttribute("type"));
            notification.AddAttribute("id", startElement.GetAttribute("id"));
            notification.AddAttribute("name", startElement.GetAttribute("name"));
            notification.AddAttribute("fullname", startElement.GetAttribute("fullname"));
            notification.AddAttribute("result", "Failed");
            notification.AddAttribute("label", "Cancelled");
            XmlNode failure = notification.AddElement("failure");
            XmlNode message = failure.AddElementWithCDataSection("message", "Test run cancelled by user");
            return notification.OuterXml;
        }

        void ITestEventListener.OnTestEvent(string report)
        {
            XmlNode xmlNode = XmlHelper.CreateXmlNode(report);

            lock (_trackerLock)
            {
                switch (xmlNode.Name)
                {
                    case "start-test":
                    case "start-suite":
                        _itemsInProcess.Add(xmlNode);
                        break;

                    case "test-case":
                    case "test-suite":
                        string id = xmlNode.GetAttribute("id");
                        RemoveItem(id);

                        if (_itemsInProcess.Count == 0)
                            _allItemsComplete.Set();
                        break;
                }
            }
        }

        private void RemoveItem(string id)
        {
            foreach (XmlNode item in _itemsInProcess)
            {
                if (item.GetAttribute("id") == id)
                {
                    _itemsInProcess.Remove(item);
                    return;
                }
            }
        }
    }
}
