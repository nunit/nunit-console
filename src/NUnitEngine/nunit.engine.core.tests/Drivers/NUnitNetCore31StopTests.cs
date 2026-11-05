// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETCOREAPP
using System;
using System.Reflection;
using System.Runtime.Loader;
using NUnit.Framework;

namespace NUnit.Engine.Drivers.Tests
{
    /// <summary>Checks compatibility with the void stop APIs exposed by NUnit 3, 4 and 5.</summary>
    public class NUnitNetCore31StopTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void LegacyBooleanStopPreservesForceArgument(bool force)
        {
            var controller = new LegacyController();
            CreateDriver(controller).StopRun(force);
            Assert.That(controller.Force, Is.EqualTo(force));
        }

        [Test]
        public void CurrentParameterlessStopIsInvoked()
        {
            var controller = new CurrentController();
            CreateDriver(controller).StopRun(false);
            Assert.That(controller.Called, Is.True);
        }

        [Test]
        public void CurrentFrameworkDoesNotPretendToSupportForcedStop()
        {
            var controller = new CurrentController();
            Assert.Throws<NotSupportedException>(() => CreateDriver(controller).StopRun(true));
            Assert.That(controller.Called, Is.False);
        }

        [Test]
        public void MissingStopApiIsReportedAsUnsupportedFramework()
        {
            Assert.Throws<NUnitEngineException>(() => CreateDriver(new object()).StopRun(false));
        }

        [Test]
        public void StopBeforeLoadExplainsTheMissingLoad()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => new NUnitNetCore31Driver().StopRun(false));
            Assert.That(exception.Message, Is.EqualTo("Method called without calling Load first"));
        }

        [Test]
        public void FrameworkStopExceptionIsPreserved()
        {
            var exception = Assert.Throws<TargetInvocationException>(() => CreateDriver(new ThrowingController()).StopRun(false));
            Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(exception.InnerException.Message, Is.EqualTo("stop failed"));
        }

        private static NUnitNetCore31Driver CreateDriver(object controller)
        {
            var driver = new NUnitNetCore31Driver();
            typeof(NUnitNetCore31Driver).GetField("_frameworkController", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(driver, controller);
            typeof(NUnitNetCore31Driver).GetField("_frameworkControllerType", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(driver, controller.GetType());
            typeof(NUnitNetCore31Driver).GetField("_assemblyLoadContext", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(driver, AssemblyLoadContext.Default);
            return driver;
        }

        private sealed class LegacyController
        {
            public bool? Force { get; private set; }

            public void StopRun(bool force) => Force = force;
        }

        private sealed class CurrentController
        {
            public bool Called { get; private set; }

            public void StopRun() => Called = true;
        }

        private sealed class ThrowingController
        {
            public void StopRun() => throw new InvalidOperationException("stop failed");
        }
    }
}
#endif
