// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace NUnit.Engine.Runners.Tests
{
    public class WorkItemTrackerTests : ITestEventListener
    {
        private WorkItemTracker _tracker;
        private ITestEventListener _listener;
        private List<string> _pendingNotices;

        [SetUp]
        public void CreateTracker()
        {
            _listener = _tracker = new WorkItemTracker();
            _pendingNotices = new List<string>();
        }

        [TestCaseSource(nameof(AllItemsComplete))]
        public void WhenAllItemsComplete_NoAdditionalReportsAreIssued(ReportSequence reports)
        {
            reports.SendTo(_listener);

            _tracker.SendPendingTestCompletionEvents(this);

            Assert.That(_pendingNotices.Count, Is.EqualTo(0));
        }

        [TestCaseSource(nameof(SomeItemsIncomplete))]
        public void WhenItemsFailToComplete_ReportsAreIssued(ReportSequence reports, ReportSequence expectedNotices)
        {
            reports.SendTo(_listener);

            _tracker.SendPendingTestCompletionEvents(this);

            Assert.That(_pendingNotices, Is.EqualTo(expectedNotices.Reports));
        }

        public class ReportSequence
        {
            public ReportSequence(params string[] reports)
            {
                Reports = reports;
            }

            public string[] Reports { get; }

            public void SendTo(ITestEventListener listener)
            {
                foreach (string report in Reports)
                    listener.OnTestEvent(report);
            }
        }

        // HACK: All of this is very fragile, since any white-space or ordering change will break it.
        // TODO: When NUnit has XML testing, rewrite.
        const string START_ASSEMBLY = "<start-suite type=\"Assembly\" id=\"1\" name=\"test.dll\" fullname=\"/path/to/test.dll\" />";
        const string START_NS_1 = "<start-suite type=\"TestSuite\" id=\"2\" name=\"My\" fullname=\"My\" />";
        const string START_NS_2 = "<start-suite type=\"TestSuite\" id=\"3\" name=\"Tests\" fullname=\"My.Tests\" />";
        const string START_FIXTURE_1 = "<start-suite type=\"TestFixture\" id=\"4\" name=\"Fixture1\" fullname=\"My.Tests.Fixture1\" />";
        const string START_FIXTURE_2 = "<start-suite type=\"TestFixture\" id=\"5\" name=\"Fixture2\" fullname=\"My.Tests.Fixture2\" />";

        static readonly string END_ASSEMBLY = START_ASSEMBLY.Replace("start-suite", "test-suite");
        static readonly string END_NS_1 = START_NS_1.Replace("start-suite", "test-suite");
        static readonly string END_NS_2 = START_NS_2.Replace("start-suite", "test-suite");
        static readonly string END_FIXTURE_1 = START_FIXTURE_1.Replace("start-suite", "test-suite");
        static readonly string END_FIXTURE_2 = START_FIXTURE_2.Replace("start-suite", "test-suite");

        const string CANCEL_INFO = "result=\"Failed\" label=\"Cancelled\"><failure><message><![CDATA[Test run cancelled by user]]></message></failure></test-suite>";
        static readonly string CANCEL_ASSEMBLY = END_ASSEMBLY.Replace("/>", CANCEL_INFO);
        static readonly string CANCEL_NS_1 = END_NS_1.Replace("/>", CANCEL_INFO);
        static readonly string CANCEL_NS_2 = END_NS_2.Replace("/>", CANCEL_INFO);
        static readonly string CANCEL_FIXTURE_1 = END_FIXTURE_1.Replace("/>", CANCEL_INFO);
        static readonly string CANCEL_FIXTURE_2 = END_FIXTURE_2.Replace("/>", CANCEL_INFO);

        static TestCaseData[] AllItemsComplete = new TestCaseData[]
        {
            new TestCaseData(new ReportSequence(
                START_ASSEMBLY, START_NS_1, START_NS_2,
                START_FIXTURE_1, END_FIXTURE_1,
                START_FIXTURE_2, END_FIXTURE_2,
                END_NS_2, END_NS_1, END_ASSEMBLY )),
            new TestCaseData(new ReportSequence(
                START_ASSEMBLY, START_NS_1, START_NS_2,
                START_FIXTURE_1, START_FIXTURE_2,
                END_FIXTURE_1, END_FIXTURE_2,
                END_NS_2, END_NS_1, END_ASSEMBLY ))
        };

        static TestCaseData[] SomeItemsIncomplete = new TestCaseData[]
        {
            new TestCaseData(
                new ReportSequence(
                    START_ASSEMBLY, START_NS_1, START_NS_2,
                    START_FIXTURE_1, // Fixture 1 hangs!
                    START_FIXTURE_2, END_FIXTURE_2),
                new ReportSequence(
                    CANCEL_FIXTURE_1, CANCEL_NS_2, CANCEL_NS_1, CANCEL_ASSEMBLY)),
            new TestCaseData(
                new ReportSequence(
                    START_ASSEMBLY, START_NS_1, START_NS_2,
                    START_FIXTURE_1, END_FIXTURE_1,
                    START_FIXTURE_2  ), // Fixture 2 hangs!
                new ReportSequence(
                    CANCEL_FIXTURE_2, CANCEL_NS_2, CANCEL_NS_1, CANCEL_ASSEMBLY)),
            new TestCaseData(
                new ReportSequence(
                    START_ASSEMBLY, START_NS_1, START_NS_2,
                    START_FIXTURE_1,
                    START_FIXTURE_2  ), // Both fixtures hang!
                new ReportSequence(
                    CANCEL_FIXTURE_2, CANCEL_FIXTURE_1, CANCEL_NS_2, CANCEL_NS_1, CANCEL_ASSEMBLY)),
        };

        void ITestEventListener.OnTestEvent(string report)
        {
            _pendingNotices.Add(report);
        }
    }
}
