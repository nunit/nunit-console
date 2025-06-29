// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics.CodeAnalysis;
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

        public ServiceManager ServiceManager { get; }

        public int ServiceCount
        {
            get { return ServiceManager.ServiceCount; }
        }

        public void Add(IService service)
        {
            ServiceManager.AddService(service);
            service.ServiceContext = this;
        }

        public T GetService<T>()
            where T : class
        {
            T? service = (T?)ServiceManager.GetServiceOrNull(typeof(T));
            if (service is not null)
                return service;

            throw new NUnitEngineException($"Unable to acquire {typeof(T).Name}");
        }

        public bool TryGetService<T>([NotNullWhen(true)] out T? service)
            where T : class
        {
            return (service = (T?)ServiceManager.GetServiceOrNull(typeof(T))) is not null;
        }
    }
}
