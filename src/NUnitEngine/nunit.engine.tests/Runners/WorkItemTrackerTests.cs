// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace NUnit.Engine.Runners
{
    public class WorkItemTrackerTests : ITestEventListener
    {
        private WorkItemTracker _tracker;
        private ITestEventListener _listener;
        private List<string> _pendingNotices;

        [SetUp]
        public void CreateTracker()
        {
            _tracker = new WorkItemTracker();
            _listener = _tracker;
            _pendingNotices = [];
        }

        [TestCaseSource(nameof(AllItemsComplete))]
        public void WhenAllItemsComplete_NoAdditionalReportsAreIssued(ReportSequence reports)
        {
            reports.SendTo(_listener);

            _tracker.SendPendingTestCompletionEvents(this);

            Assert.That(_pendingNotices, Is.Empty);
        }

        [TestCaseSource(nameof(SomeItemsIncomplete))]
        public void WhenItemsFailToComplete_ReportsAreIssued(ReportSequence reports, ReportSequence expectedNotices)
        {
            reports.SendTo(_listener);

            _tracker.SendPendingTestCompletionEvents(this);

            Assert.That(_pendingNotices, Is.EqualTo(expectedNotices.Reports));
        }

        public class ReportSequence(params string[] reports)
        {
            public string[] Reports { get; } = reports;

            public void SendTo(ITestEventListener listener)
            {
                foreach (string report in Reports)
                    listener.OnTestEvent(report);
            }
        }

        // HACK: All of this is very fragile, since any white-space or ordering change will break it.
        // TODO: When NUnit has XML testing, rewrite.
        private const string START_ASSEMBLY = "<start-suite type=\"Assembly\" id=\"1\" name=\"test.dll\" fullname=\"/path/to/test.dll\" />";
        private const string START_NS_1 = "<start-suite type=\"TestSuite\" id=\"2\" name=\"My\" fullname=\"My\" />";
        private const string START_NS_2 = "<start-suite type=\"TestSuite\" id=\"3\" name=\"Tests\" fullname=\"My.Tests\" />";
        private const string START_FIXTURE_1 = "<start-suite type=\"TestFixture\" id=\"4\" name=\"Fixture1\" fullname=\"My.Tests.Fixture1\" />";
        private const string START_FIXTURE_2 = "<start-suite type=\"TestFixture\" id=\"5\" name=\"Fixture2\" fullname=\"My.Tests.Fixture2\" />";

        private static readonly string END_ASSEMBLY = START_ASSEMBLY.Replace("start-suite", "test-suite");
        private static readonly string END_NS_1 = START_NS_1.Replace("start-suite", "test-suite");
        private static readonly string END_NS_2 = START_NS_2.Replace("start-suite", "test-suite");
        private static readonly string END_FIXTURE_1 = START_FIXTURE_1.Replace("start-suite", "test-suite");
        private static readonly string END_FIXTURE_2 = START_FIXTURE_2.Replace("start-suite", "test-suite");

        private const string CANCEL_INFO = "result=\"Failed\" label=\"Cancelled\"><failure><message><![CDATA[Test run cancelled by user]]></message></failure></test-suite>";
        private static readonly string CANCEL_ASSEMBLY = END_ASSEMBLY.Replace("/>", CANCEL_INFO);
        private static readonly string CANCEL_NS_1 = END_NS_1.Replace("/>", CANCEL_INFO);
        private static readonly string CANCEL_NS_2 = END_NS_2.Replace("/>", CANCEL_INFO);
        private static readonly string CANCEL_FIXTURE_1 = END_FIXTURE_1.Replace("/>", CANCEL_INFO);
        private static readonly string CANCEL_FIXTURE_2 = END_FIXTURE_2.Replace("/>", CANCEL_INFO);

        private static readonly TestCaseData[] AllItemsComplete =
        [
            new TestCaseData(new ReportSequence(
                START_ASSEMBLY, START_NS_1, START_NS_2,
                START_FIXTURE_1, END_FIXTURE_1,
                START_FIXTURE_2, END_FIXTURE_2,
                END_NS_2, END_NS_1, END_ASSEMBLY)),
            new TestCaseData(new ReportSequence(
                START_ASSEMBLY, START_NS_1, START_NS_2,
                START_FIXTURE_1, START_FIXTURE_2,
                END_FIXTURE_1, END_FIXTURE_2,
                END_NS_2, END_NS_1, END_ASSEMBLY))
        ];

        private static readonly TestCaseData[] SomeItemsIncomplete =
        [
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
                    START_FIXTURE_2), // Fixture 2 hangs!
                new ReportSequence(
                    CANCEL_FIXTURE_2, CANCEL_NS_2, CANCEL_NS_1, CANCEL_ASSEMBLY)),
            new TestCaseData(
                new ReportSequence(
                    START_ASSEMBLY, START_NS_1, START_NS_2,
                    START_FIXTURE_1,
                    START_FIXTURE_2), // Both fixtures hang!
                new ReportSequence(
                    CANCEL_FIXTURE_2, CANCEL_FIXTURE_1, CANCEL_NS_2, CANCEL_NS_1, CANCEL_ASSEMBLY)),
        ];

        void ITestEventListener.OnTestEvent(string report)
        {
            _pendingNotices.Add(report);
        }
    }
}
