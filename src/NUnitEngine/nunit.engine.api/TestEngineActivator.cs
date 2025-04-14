// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Reflection;

namespace NUnit.Engine
{
    /// <summary>
    /// TestEngineActivator creates an instance of the test engine and returns an ITestEngine interface.
    /// </summary>
    public static class TestEngineActivator
    {
        private const string DEFAULT_ENGINE_ASSEMBLY = "nunit.engine.dll";
        internal const string DEFAULT_ENGINE_TYPE = "NUnit.Engine.TestEngine";

        /// <summary>
        /// Create an instance of the test engine.
        /// </summary>
        /// <returns>An <see cref="NUnit.Engine.ITestEngine"/></returns>
        public static ITestEngine CreateInstance()
        {
            var apiLocation = typeof(TestEngineActivator).Assembly.Location;
            var directoryName = Path.GetDirectoryName(apiLocation);
            var enginePath = directoryName is null ? DEFAULT_ENGINE_ASSEMBLY : Path.Combine(directoryName, DEFAULT_ENGINE_ASSEMBLY);
            var assembly = Assembly.LoadFrom(enginePath);
            var engineType = assembly.GetType(DEFAULT_ENGINE_TYPE, throwOnError: true)!;
            return (ITestEngine)Activator.CreateInstance(engineType)!;
        }
    }
}
