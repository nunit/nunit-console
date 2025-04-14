// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    internal sealed class WorkItemTracker : ITestEventListener
    {
        /// <summary>
        /// Holds data about recorded test that started.
        /// </summary>
        private sealed class InProgressItem : IComparable<InProgressItem>
        {
            private readonly int _order;

            public InProgressItem(int order, string name, XmlReader reader)
            {
                _order = order;
                Name = name;

                var attributeCount = reader.AttributeCount;
                Properties = new Dictionary<string, string>(attributeCount);
                for (var i = 0; i < attributeCount; i++)
                {
                    reader.MoveToNextAttribute();
                    Properties.Add(reader.Name, reader.Value);
                }
            }

            public string Name { get; }
            public Dictionary<string, string> Properties { get; }

            public int CompareTo(InProgressItem? other)
            {
                // for signaling purposes, return in reverse order
                if (other is null)
                    return -1;

                return _order.CompareTo(other._order) * -1;
            }
        }

        private static readonly Logger log = InternalTrace.GetLogger(nameof(InProgressItem));

        // items are keyed by id
        private readonly Dictionary<string, InProgressItem> _itemsInProcess = new Dictionary<string, InProgressItem>();
        private readonly ManualResetEvent _allItemsComplete = new ManualResetEvent(false);
        private readonly object _trackerLock = new object();

        // incrementing ordering id for work items so we can traverse in correct order
        private int _itemOrderNumberCounter = 1;

        // when sending thousands of cancelled notifications, it makes sense to reuse string builder, used inside a lock
        private readonly StringBuilder _notificationBuilder = new StringBuilder();

        // we want to write just the main element without XML declarations
        private static readonly XmlWriterSettings XmlWriterSettings = new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment };

        public void Clear()
        {
            lock (_trackerLock)
            {
                _itemsInProcess.Clear();
                _allItemsComplete.Reset();
                _itemOrderNumberCounter = 1;
            }
        }

        public bool WaitForCompletion(int millisecondsTimeout)
        {
            return _allItemsComplete.WaitOne(millisecondsTimeout);
        }

        public void SendPendingTestCompletionEvents(ITestEventListener listener)
        {
            lock (_trackerLock)
            {
                // Signal completion of all pending suites, in reverse order
                var toNotify = new List<InProgressItem>(_itemsInProcess.Values);
                toNotify.Sort();

                foreach (var item in toNotify)
                    listener.OnTestEvent(CreateNotification(item));
            }
        }

        private string CreateNotification(InProgressItem item)
        {
            _notificationBuilder.Clear();

            using (var stringWriter = new StringWriter(_notificationBuilder))
            {
                using (var writer = XmlWriter.Create(stringWriter, XmlWriterSettings))
                {
                    bool isSuite = item.Name == "start-suite";
                    writer.WriteStartElement(isSuite ? "test-suite" : "test-case");

                    if (isSuite)
                        writer.WriteAttributeString("type", item.Properties["type"]);

                    writer.WriteAttributeString("id", item.Properties["id"]);
                    writer.WriteAttributeString("name", item.Properties["name"]);
                    writer.WriteAttributeString("fullname", item.Properties["fullname"]);
                    writer.WriteAttributeString("result", "Failed");
                    writer.WriteAttributeString("label", "Cancelled");

                    writer.WriteStartElement("failure");
                    writer.WriteStartElement("message");
                    writer.WriteCData("Test run cancelled by user");
                    writer.WriteEndElement();
                    writer.WriteEndElement();

                    writer.WriteEndElement();
                }

                return stringWriter.ToString();
            }
        }

        void ITestEventListener.OnTestEvent(string report)
        {
            using (var stringReader = new StringReader(report))
            using (var reader = XmlReader.Create(stringReader))
            {
                // go to starting point
                reader.MoveToContent();

                if (reader.NodeType != XmlNodeType.Element)
                    throw new InvalidOperationException("Expected to find root element");

                lock (_trackerLock)
                {
                    var name = reader.Name;
                    switch (name)
                    {
                        case "start-test":
                        case "start-suite":
                            var item = new InProgressItem(_itemOrderNumberCounter++, name, reader);
                            _itemsInProcess.Add(item.Properties["id"], item);
                            break;

                        case "test-case":
                        case "test-suite":
                            string? id = reader.GetAttribute("id");
                            if (id is not null) // TODO: Should we throw if id is null?
                                RemoveItem(id);

                            if (_itemsInProcess.Count == 0)
                                _allItemsComplete.Set();
                            break;
                    }
                }
            }
        }

        private void RemoveItem(string id)
        {
            _itemsInProcess.Remove(id);
        }
    }
}