// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Engine.Services;

namespace NUnit.Engine
{
    /// <summary>
    /// The ServiceContext is used by services, runners and
    /// external clients to locate the services they need through
    /// the IServiceLocator interface.
    /// </summary>
    public class ServiceContext : IServiceLocator
    {
        public ServiceContext()
        {
            ServiceManager = new ServiceManager();
        }

        public ServiceManager ServiceManager { get; private set; }

        public int ServiceCount { get { return ServiceManager.ServiceCount; } }

        public void Add(IService service)
        {
            ServiceManager.AddService(service);
            service.ServiceContext = this;
        }

        public T GetService<T>() where T : class
        {
            return ServiceManager.GetService(typeof(T)) as T;
        }

        public object GetService(Type serviceType)
        {
            return ServiceManager.GetService(serviceType);
        }
    }
}
