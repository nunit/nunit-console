// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Framework;
using NSubstitute;
using NUnit.Engine.Services;
using NUnit.Framework.Internal;
using NSubstitute.Core.Arguments;

namespace NUnit.Engine.Runners
{
    [TestFixtureSource(nameof(TestData))]
    public class MasterTestRunnerTests
    {
        private FakeServiceContext _services;
        private TestPackage _package;

        private ITestEngineRunner _engineRunner;
        private MasterTestRunner _masterTestRunner;

        private static TestFixtureData[] TestData =
        {
            new TestFixtureData(new[] { "test.dll" }),
            new TestFixtureData(new[] { "test1.dll", "test2.dll" })
        };

        public MasterTestRunnerTests(params string[] testFiles)
        {
            _package = new TestPackage(testFiles);
        }

        [SetUp]
        public void Initialize()
        {
            // Fake Context provides  substitutes for all services
            _services = new FakeServiceContext();
            _services.Initialize();

            _engineRunner = Substitute.For<ITestEngineRunner>();
            _services.TestRunnerFactory.MakeTestRunner(_package).ReturnsForAnyArgs(_engineRunner);

            _masterTestRunner = new MasterTestRunner(_services, _package);
        }

        [TearDown]
        public void Cleanup()
        {
            _engineRunner.Dispose();
            _masterTestRunner.Dispose();
        }

        //[Test]
        //public void Load()
        //{
        //    _engineRunner.Load().Returns(new TestEngineResult());
        //    _masterTestRunner.Load();
        //    _engineRunner.Received().Load();
        //}

#if NETFRAMEWORK
        //[Test]
        //public void Reload()
        //{
        //    _engineRunner.Reload().Returns(new TestEngineResult());
        //    _masterTestRunner.Reload();
        //    _engineRunner.Received().Reload();
        //}

        [Test]
        public void Explore()
        {
            var filter = TestFilter.Empty;
            _engineRunner.Explore(filter).Returns(new TestEngineResult());
            _masterTestRunner.Explore(filter);
            _engineRunner.Received().Explore(filter);
        }

        [Test]
        public void Run()
        {
            var listener = Substitute.For<ITestEventListener>();
            var filter = TestFilter.Empty;
            _engineRunner.Run(Arg.Any<TestEventDispatcher>(), filter).Returns(new TestEngineResult());
            _masterTestRunner.Run(listener, filter);
            _engineRunner.Received().Run(Arg.Any<TestEventDispatcher>(), filter);
        }

        [Test]
        public void RunAsync()
        {
            var listener = Substitute.For<ITestEventListener>();
            var filter = TestFilter.Empty;
            _engineRunner.Run(Arg.Any<TestEventDispatcher>(), filter).Returns(new TestEngineResult());
            _masterTestRunner.Run(listener, filter);
            _engineRunner.Received().Run(Arg.Any<TestEventDispatcher>(), filter);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StopRun(bool force)
        {
            _masterTestRunner.GetEngineRunner();
            _masterTestRunner.StopRun(force);
            if (force)
                _engineRunner.Received().ForcedStop();
            else
                _engineRunner.Received().RequestStop();
        }
#endif
    }
}
