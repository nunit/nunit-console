// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Text;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Abstract base class for services that can use it. Some Services
    /// already inherit from a different class and can't use this, which
    /// is why we define the IService interface as well.
    /// </summary>
    public abstract class Service : IService, IDisposable
    {
        /// <summary>
        /// The ServiceContext
        /// </summary>
        public IServiceLocator ServiceContext { get; set; }

        /// <summary>
        /// Gets the ServiceStatus of this service
        /// </summary>
        public ServiceStatus Status { get; protected set;  }

        /// <summary>
        /// Initialize the Service
        /// </summary>
        public virtual void StartService()
        {
            Status = ServiceStatus.Started;
        }

        /// <summary>
        /// Do any cleanup needed before terminating the service
        /// </summary>
        public virtual void StopService()
        {
            Status = ServiceStatus.Stopped;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected bool _disposed = false;

        protected virtual void Dispose(bool disposing) { }
    }
}
