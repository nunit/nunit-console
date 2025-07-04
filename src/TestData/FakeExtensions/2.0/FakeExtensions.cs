﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;
using NUnit.Engine.Extensibility;
using NUnit.Extensibility;

namespace NUnit.Engine.Fakes
{
    [Extension]
    public class FakeFrameworkDriverExtension : IDriverFactory
    {
#if NETFRAMEWORK
        public IFrameworkDriver GetDriver(AppDomain domain, string id, AssemblyName reference) => throw new NotImplementedException();
#else
        public IFrameworkDriver GetDriver(string id, AssemblyName reference) => throw new NotImplementedException();
#endif

        public bool IsSupportedTestFramework(AssemblyName reference) => throw new NotImplementedException();
    }

    [Extension]
    public class FakeProjectLoaderExtension : IProjectLoader
    {
        public bool CanLoadFrom(string path) => throw new NotImplementedException();

        public IProject LoadFrom(string path) => throw new NotImplementedException();
    }

    [Extension]
    public class FakeResultWriterExtension : IResultWriter
    {
        public void CheckWritability(string outputPath) => throw new NotImplementedException();

        public void WriteResultFile(XmlNode resultNode, TextWriter writer) => throw new NotImplementedException();

        public void WriteResultFile(XmlNode resultNode, string outputPath) => throw new NotImplementedException();
    }

    [Extension]
    public class FakeEventListenerExtension : ITestEventListener
    {
        public void OnTestEvent(string report)
        {
            if (report.Length > 63)
                report = report.Substring(0, 60) + "...";
            Console.WriteLine($"EventListener: {report}");
        }
    }

    [Extension]
    public class FakeServiceExtension : IService
    {
        public IServiceLocator ServiceContext
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public ServiceStatus Status => throw new NotImplementedException();

        public void StartService() => throw new NotImplementedException();

        public void StopService() => throw new NotImplementedException();
    }

    // TODO: Determine whether we support the V2 driver and, if so, how
    //[Extension(Path= "/NUnit/Engine/NUnitV2Driver")]
    //public class V2DriverExtension : IFrameworkDriver
    //{
    //    public string ID
    //    {
    //        get
    //        {
    //            throw new NotImplementedException();
    //        }

    //        set
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    //    public int CountTestCases(string filter)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public string Explore(string filter)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public string Load(string testAssemblyPath, IDictionary<string, object> settings)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public string Run(ITestEventListener? listener, string filter)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void StopRun(bool force)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    [Extension(Enabled=false)]
    public class FakeDisabledExtension : ITestEventListener
    {
        public void OnTestEvent(string report) => throw new NotImplementedException();
    }

    [Extension]
    public class FakeAgentLauncherExtension : IAgentLauncher
    {
        public TestAgentInfo AgentInfo => throw new NotImplementedException();

        public bool CanCreateAgent(TestPackage package) => throw new NotImplementedException();

        public Process CreateAgent(Guid agentId, string agencyUrl, TestPackage package) => throw new NotImplementedException();
    }
}
