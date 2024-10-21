// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Tests
{
    [Extension]
    public class DummyFrameworkDriverExtension : IDriverFactory
    {
#if !NETFRAMEWORK
        public IFrameworkDriver GetDriver(AssemblyName reference)
#else
        public IFrameworkDriver GetDriver(AppDomain domain, AssemblyName reference)
#endif
        {
            throw new NotImplementedException();
        }

        public bool IsSupportedTestFramework(AssemblyName reference)
        {
            throw new NotImplementedException();
        }
    }

    [Extension]
    public class DummyProjectLoaderExtension : IProjectLoader
    {
        public bool CanLoadFrom(string path)
        {
            throw new NotImplementedException();
        }

        public IProject LoadFrom(string path)
        {
            throw new NotImplementedException();
        }
    }

    [Extension]
    public class DummyResultWriterExtension : IResultWriter
    {
        public void CheckWritability(string outputPath)
        {
            throw new NotImplementedException();
        }

        public void WriteResultFile(XmlNode resultNode, TextWriter writer)
        {
            throw new NotImplementedException();
        }

        public void WriteResultFile(XmlNode resultNode, string outputPath)
        {
            throw new NotImplementedException();
        }
    }

    [Extension]
    public class DummyEventListenerExtension : ITestEventListener
    {
        public void OnTestEvent(string report)
        {
            throw new NotImplementedException();
        }
    }

    [Extension]
    public class DummyServiceExtension : IService
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

        public ServiceStatus Status
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void StartService()
        {
            throw new NotImplementedException();
        }

        public void StopService()
        {
            throw new NotImplementedException();
        }
    }

    [Extension]
    public class V2DriverExtension : IFrameworkDriver
    {
        public string ID
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

        public int CountTestCases(string filter)
        {
            throw new NotImplementedException();
        }

        public string Explore(string filter)
        {
            throw new NotImplementedException();
        }

        public string Load(string testAssemblyPath, IDictionary<string, object> settings)
        {
            throw new NotImplementedException();
        }

        public string Run(ITestEventListener listener, string filter)
        {
            throw new NotImplementedException();
        }

        public void StopRun(bool force)
        {
            throw new NotImplementedException();
        }
    }

    [Extension(Enabled=false)]
    public class DummyDisabledExtension : ITestEventListener
    {
        public void OnTestEvent(string report)
        {
            throw new NotImplementedException();
        }
    }
}
