// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Engine;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Drivers
{
    /// <summary>
    /// NUnitFrameworkDriver is used by the test-runner to load and run
    /// tests using the NUnit framework assembly, versions 3 and up.
    /// </summary>
    public class NUnitFrameworkDriver : IFrameworkDriver
    {
        private static readonly Version MINIMUM_NUNIT_VERSION = new(3, 2, 0);
        private static readonly Logger log = InternalTrace.GetLogger(nameof(NUnitFrameworkDriver));

        private readonly NUnitFrameworkApi _api;

#if NETFRAMEWORK
        /// <summary>
        /// Construct an NUnitFrameworkDriver
        /// </summary>
        /// <param name="testDomain">The application domain in which to create the FrameworkController</param>
        /// <param name="nunitRef">An AssemblyName referring to the test framework.</param>
        public NUnitFrameworkDriver(AppDomain testDomain, string id, AssemblyName nunitRef)
        {
            Guard.ArgumentNotNull(testDomain, nameof(testDomain));
            Guard.ArgumentNotNullOrEmpty(id, nameof(id));
            Guard.ArgumentNotNull(nunitRef, nameof(nunitRef));

            ID = id;

            if (nunitRef.Version >= MINIMUM_NUNIT_VERSION)
            {
                API = "2018";
                _api = (NUnitFrameworkApi)testDomain.CreateInstanceFromAndUnwrap(
                    Assembly.GetExecutingAssembly().Location,
                    "NUnit.Engine.Drivers.NUnitFrameworkApi2018",
                    false,
                    0,
                    null,
                    new object[] { ID, nunitRef },
                    null,
                    null).ShouldNotBeNull();
            }
            else
            {
                API = "2009";
                _api = new NUnitFrameworkApi2009(testDomain, ID, nunitRef);
            }
        }

        /// <summary>
        /// Internal generic constructor used by our tests.
        /// </summary>
        /// <param name="testDomain">The application domain in which to create the FrameworkController</param>
        /// <param name="nunitRef">An AssemblyName referring to the test framework.</param>
        internal NUnitFrameworkDriver(AppDomain testDomain, string api, string id, AssemblyName nunitRef)
        {
            Guard.ArgumentNotNull(testDomain, nameof(testDomain));
            Guard.ArgumentNotNull(api, nameof(api));
            Guard.ArgumentValid(api == "2009" || api == "2018", $"Invalid API specified: {api}", nameof(api));
            Guard.ArgumentNotNullOrEmpty(id, nameof(id));
            Guard.ArgumentNotNull(nunitRef, nameof(nunitRef));

            ID = id;
            API = api;

            _api = api == "2018"
                ? (NUnitFrameworkApi)testDomain.CreateInstanceFromAndUnwrap(
                    Assembly.GetExecutingAssembly().Location,
                    typeof(NUnitFrameworkApi2018).FullName!,
                    false,
                    0,
                    null,
                    new object[] { ID, nunitRef },
                    null,
                    null).ShouldNotBeNull()
                : new NUnitFrameworkApi2009(testDomain, ID, nunitRef);
        }
#else
        /// <summary>
        /// Construct an NUnitFrameworkDriver
        /// </summary>
        /// <param name="reference">An AssemblyName referring to the test framework.</param>
        public NUnitFrameworkDriver(string id, AssemblyName nunitRef)
        {
            Guard.ArgumentNotNullOrEmpty(id, nameof(id));
            Guard.ArgumentNotNull(nunitRef, nameof(nunitRef));

            ID = id;
            API = "2018";

            _api = new NUnitFrameworkApi2018(ID, nunitRef);
        }
#endif

        /// <summary>
        /// String naming the API in use, for use by tests
        /// </summary>
        internal string API { get; } = string.Empty;

        /// <summary>
        /// An id prefix that will be passed to the test framework and used as part of the
        /// test ids created.
        /// </summary>
        public string ID { get; }

        /// <summary>
        /// Loads the tests in an assembly.
        /// </summary>
        /// <param name="testAssemblyPath">The path to the test assembly</param>
        /// <param name="settings">The test settings</param>
        /// <returns>An XML string representing the loaded test</returns>
        public string Load(string testAssemblyPath, IDictionary<string, object> settings)
            => _api.Load(testAssemblyPath, settings);

        /// <summary>
        /// Counts the number of test cases for the loaded test assembly
        /// </summary>
        /// <param name="filter">The XML test filter</param>
        /// <returns>The number of test cases</returns>
        public int CountTestCases(string filter) => _api.CountTestCases(filter);

        /// <summary>
        /// Executes the tests in an assembly.
        /// </summary>
        /// <param name="listener">An ITestEventHandler that receives progress notices</param>
        /// <param name="filter">A filter that controls which tests are executed</param>
        /// <returns>An Xml string representing the result</returns>
        public string Run(ITestEventListener? listener, string filter)
        {
            return _api.Run(listener != null ? new EventInterceptor(listener) : null, filter);
        }

        /// <summary>
        /// Executes the tests in an assembly asynchronously.
        /// </summary>
        /// <param name="callback">A callback that receives XML progress notices</param>
        /// <param name="filter">A filter that controls which tests are executed</param>
        public void RunAsync(Action<string> callback, string filter) => _api.RunAsync(callback, filter);

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public void StopRun(bool force) => _api.StopRun(force);

        /// <summary>
        /// Returns information about the tests in an assembly.
        /// </summary>
        /// <param name="filter">A filter indicating which tests to include</param>
        /// <returns>An Xml string representing the tests</returns>
        public string Explore(string filter) => _api.Explore(filter);

        /// <summary>
        /// Nested class used to intercept progress reports received from the test
        /// framework and resend them to the actual listener, normally located in
        /// the runner that is using the engine.
        /// </summary>
        /// <remarks>
        /// This class is absolutely needed when the 2018 NUnit API is used under
        /// the .NET Framework using Windows Remoting as a communication protocol.
        /// In particular, the MarshalByRef object implementing the listener must
        /// be convertible to ITestEventListener via the IConvertible interface.
        ///
        /// In other cases, the interceptor is not essential, but we use it anyway
        /// for several reasons:
        ///
        /// 1. We have no control over the implementation of runners using the engine.
        ///    They may not implement IConvertible and may not even derive from
        ///    MarshaByRefObject.
        ///
        /// 2. The interceptor provides a point of control for checking what events
        ///    are received from the framework and for possible future modifications to
        ///    the events before they are forwarded.
        /// </remarks>
        public class EventInterceptor : MarshalByRefObject, ITestEventListener, IConvertible
        {
            private ITestEventListener _listener;

            public EventInterceptor(ITestEventListener listener)
            {
                _listener = listener;
            }

            #region ITestEventListener and IConvertible Implementations

            void ITestEventListener.OnTestEvent(string report)
            {
                _listener.OnTestEvent(report);
            }

            // Conversion to ITestEventListener is the only one that makes sense
            object IConvertible.ToType(Type conversionType, IFormatProvider? provider) =>
                conversionType == typeof(ITestEventListener) ? this : InvalidCast(conversionType);

            TypeCode IConvertible.GetTypeCode() => TypeCode.Object;
            bool IConvertible.ToBoolean(IFormatProvider? provider) => InvalidCast<bool>();
            char IConvertible.ToChar(IFormatProvider? provider) => InvalidCast<char>();
            sbyte IConvertible.ToSByte(IFormatProvider? provider) => InvalidCast<sbyte>();
            byte IConvertible.ToByte(IFormatProvider? provider) => InvalidCast<byte>();
            short IConvertible.ToInt16(IFormatProvider? provider) => InvalidCast<short>();
            ushort IConvertible.ToUInt16(IFormatProvider? provider) => InvalidCast<ushort>();
            int IConvertible.ToInt32(IFormatProvider? provider) => InvalidCast<int>();
            uint IConvertible.ToUInt32(IFormatProvider? provider) => InvalidCast<uint>();
            long IConvertible.ToInt64(IFormatProvider? provider) => InvalidCast<long>();
            ulong IConvertible.ToUInt64(IFormatProvider? provider) => InvalidCast<ulong>();
            float IConvertible.ToSingle(IFormatProvider? provider) => InvalidCast<float>();
            double IConvertible.ToDouble(IFormatProvider? provider) => InvalidCast<double>();
            decimal IConvertible.ToDecimal(IFormatProvider? provider) => InvalidCast<decimal>();
            DateTime IConvertible.ToDateTime(IFormatProvider? provider) => InvalidCast<DateTime>();
            string IConvertible.ToString(IFormatProvider? provider) => InvalidCast<string>();

            private static T InvalidCast<T>() => (T)InvalidCast(typeof(T));

            private static object InvalidCast(Type type) =>
                throw new InvalidCastException($"{nameof(EventInterceptor)} is not convertible to {nameof(type)}");

            #endregion
        }
    }
}
