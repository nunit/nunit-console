// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace NUnit.TestData
{
    namespace Assemblies
    {
        /// <summary>
        /// MockAssembly is intended for those few tests that can only
        /// be made to work by loading an entire assembly. Please don't
        /// add any other entries or use it for other purposes.
        ///
        /// Most tests used as data for NUnit's own tests should be
        /// in the testdata assembly.
        /// </summary>
        public class MockAssembly
        {
            public const int Classes = 9;
            public const int NamespaceSuites = 6; // assembly, NUnit, Tests, Assemblies, Singletons, TestAssembly

            public const int Tests = MockTestFixture.Tests
                        + Singletons.OneTestCase.Tests
                        + TestAssembly.MockTestFixture.Tests
                        + IgnoredFixture.Tests
                        + ExplicitFixture.Tests
                        + BadFixture.Tests
                        + FixtureWithTestCases.Tests
                        + ParameterizedFixture.Tests
                        + GenericFixtureConstants.Tests
                        + AccessesCurrentTestContextDuringDiscovery.Tests
                        + FixtureWithDispose.Tests
                        + FixtureWithOneTimeTearDown.Tests
                        + TestSetUpFixture.SetUpFixture.TestsInNamespace;

            public const int Suites = MockTestFixture.Suites
                        + Singletons.OneTestCase.Suites
                        + TestAssembly.MockTestFixture.Suites
                        + IgnoredFixture.Suites
                        + ExplicitFixture.Suites
                        + BadFixture.Suites
                        + FixtureWithTestCases.Suites
                        + ParameterizedFixture.Suites
                        + GenericFixtureConstants.Suites
                        + AccessesCurrentTestContextDuringDiscovery.Suites
                        + NamespaceSuites
                        + FixtureWithDispose.Suites
                        + FixtureWithOneTimeTearDown.Suites;

            public const int TestStartedEvents = Tests - IgnoredFixture.Tests - BadFixture.Tests - ExplicitFixture.Tests;
            public const int TestFinishedEvents = Tests;

            public const int TestOutputEvents = 1;

            public const int Nodes = Tests + Suites;

            public const int ExplicitFixtures = 1;
            public const int SuitesRun = Suites - ExplicitFixtures;

            public const int Passed = MockTestFixture.Passed
                        + Singletons.OneTestCase.Tests
                        + TestAssembly.MockTestFixture.Tests
                        + FixtureWithTestCases.Tests
                        + ParameterizedFixture.Tests
                        + GenericFixtureConstants.Tests
                        + AccessesCurrentTestContextDuringDiscovery.Tests;

            public const int PassedButFailedInTearDown = FixtureWithDispose.Tests
                        + FixtureWithOneTimeTearDown.Tests
                        + TestSetUpFixture.SetUpFixture.TestsInNamespace;

            public const int Passed_Raw = Passed + PassedButFailedInTearDown;

            public const int Ignored = MockTestFixture.Ignored + IgnoredFixture.Tests;
            public const int Explicit = MockTestFixture.Explicit + ExplicitFixture.Tests;
            public const int Skipped = Ignored + Explicit;

            public const int Failures = MockTestFixture.Failures;
            public const int Errors_Raw = MockTestFixture.Errors;
            public const int Errors = MockTestFixture.Errors + PassedButFailedInTearDown;
            public const int NotRunnable = MockTestFixture.NotRunnable + BadFixture.Tests;
            public const int Failed = Failures + Errors + NotRunnable;
            public const int Failed_Raw = Failures + Errors_Raw + NotRunnable;

            public const int Warnings = MockTestFixture.Warnings;

            public const int Inconclusive = MockTestFixture.Inconclusive;

            public static readonly string AssemblyPath = AssemblyHelper.GetAssemblyPath(typeof(MockAssembly).Assembly);

            // Method run manually to verify that the counts are set up correctly
            public static void DisplayCounts()
            {
                Console.WriteLine($"Test Count:   {Tests}");
                Console.WriteLine($"  Passed:       {Passed}");
                Console.WriteLine($"  Failed:       {Failed}");
                Console.WriteLine($"    Failures:     {Failures}");
                Console.WriteLine($"    Errors:       {Errors}");
                Console.WriteLine($"    Invalid:      {NotRunnable}");
                Console.WriteLine($"  Warnings:     {Warnings}");
                Console.WriteLine($"  Skipped:      {Skipped}");
                Console.WriteLine($"    Explicit:     {Explicit}");
                Console.WriteLine($"    Ignored:      {Ignored}");
                Console.WriteLine($"  Inconclusive: {Inconclusive}");
            }
        }

        [TestFixture(Description="Fake Test Fixture")]
        [Category("FixtureCategory")]
        public class MockTestFixture
        {
            public const int Tests = 9;
            public const int Suites = 1;

            public const int Passed = 1;

            public const int Ignored = 1;
            public const int Explicit = 1;
            public const int Skipped = Ignored + Explicit;

            public const int Failures = 1;
            public const int Errors = 1;
            public const int NotRunnable = 2;
            public const int Failed = Errors + Failures + NotRunnable;

            public const int Warnings = 1;

            public const int Inconclusive = 1;

            [Test(Description="Mock Test #1")]
            [Category("MockCategory")]
            [Property("Severity", "Critical")]
            public void TestWithDescription()
            {
            }

            [Test]
            protected static void NonPublicTest()
            {
            }

            [Test]
            public void FailingTest()
            {
                Console.Error.WriteLine("Immediate Error Message");
                Assert.Fail("Intentional failure");
            }

            [Test]
            public void WarningTest()
            {
                Assert.Warn("Warning Message");
            }

            [Test, Ignore("Ignore Message")]
            public void IgnoreTest()
            {
            }

            [Test, Explicit]
            public void ExplicitTest()
            {
            }

            [Test]
            public void NotRunnableTest(int a, int b)
            {
            }

            [Test]
            public void InconclusiveTest()
            {
                Assert.Inconclusive("No valid data");
            }

            [Test]
            public void TestWithException()
            {
                MethodThrowsException();
            }

            private static void MethodThrowsException()
            {
                throw new Exception("Intentional Exception");
            }
        }
    }

    namespace Singletons
    {
        [TestFixture]
        public class OneTestCase
        {
            public const int Tests = 1;
            public const int Suites = 1;

            [Test]
            public virtual void TestCase()
            {
            }
        }
    }

    namespace TestAssembly
    {
        [TestFixture]
        public class MockTestFixture
        {
            public const int Tests = 1;
            public const int Suites = 1;

            [Test]
            public void MyTest()
            {
            }
        }
    }

    [TestFixture, Ignore("BECAUSE")]
    public class IgnoredFixture
    {
        public const int Tests = 3;
        public const int Suites = 1;

        [Test]
        public void Test1()
        {
        }

        [Test]
        public void Test2()
        {
        }

        [Test]
        public void Test3()
        {
        }
    }

    [TestFixture, Explicit]
    public class ExplicitFixture
    {
        public const int Tests = 2;
        public const int Suites = 1;
        public const int Nodes = Tests + Suites;

        [Test]
        public void Test1()
        {
        }

        [Test]
        public void Test2()
        {
        }
    }

    [TestFixture]
    public class BadFixture
    {
        public const int Tests = 1;
        public const int Suites = 1;

        public BadFixture(int val)
        {
        }

        [Test]
        public void SomeTest()
        {
        }
    }

    [TestFixture]
    public class FixtureWithTestCases
    {
        public const int Tests = 4;
        public const int Suites = 3;

        [TestCase(2, 2, ExpectedResult=4)]
        [TestCase(9, 11, ExpectedResult=20)]
        public int MethodWithParameters(int x, int y)
        {
            return x + y;
        }

        [TestCase(2, 4)]
        [TestCase(9.2, 11.7)]
        public void GenericMethod<T>(T x, T y)
        {
        }
    }

    [TestFixture(5)]
    [TestFixture(42)]
    public class ParameterizedFixture
    {
        public const int Tests = 4;
        public const int Suites = 3;

        public ParameterizedFixture(int num)
        {
        }

        [Test]
        public void Test1()
        {
        }

        [Test]
        public void Test2()
        {
        }
    }

    public class GenericFixtureConstants
    {
        public const int Tests = 4;
        public const int Suites = 3;
    }

    [TestFixture(5)]
    [TestFixture(11.5)]
    public class GenericFixture<T>
    {
        public GenericFixture(T num)
        {
        }

        [Test]
        public void Test1()
        {
        }

        [Test]
        public void Test2()
        {
        }
    }

    [TestFixture]
    public class FixtureWithDispose : IDisposable
    {
        public const int Suites = 1;
        public const int Tests = 2;

        [Test]
        public void Test1()
        {
        }

        [Test]
        public void Test2()
        {
        }

        public void Dispose()
        {
            throw new Exception("Exception in Dispose");
        }
    }

    [TestFixture]
    public class FixtureWithOneTimeTearDown
    {
        public const int Suites = 1;
        public const int Tests = 2;

        [Test]
        public void Test1()
        {
        }

        [Test]
        public void Test2()
        {
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            throw new Exception("Exception in OneTimeTearDown");
        }
    }

    namespace TestSetUpFixture
    {
        [SetUpFixture]
        public class SetUpFixture
        {
            public const int SuitesInNamespace = 2;
            public const int TestsInNamespace = 2;

            [OneTimeTearDown]
            public void OneTimeTearDown()
            {
                throw new Exception("Exception in SetUpFixture.OneTimeTearDown");
            }
        }

        [TestFixture]
        public class Fixture1
        {
            [Test]
            public void Test1()
            {
            }
        }

        [TestFixture]
        public class Fixture2
        {
            [Test]
            public void Test1()
            {
            }
        }
    }
}
