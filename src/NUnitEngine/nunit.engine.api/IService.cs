// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine
{
    /// <summary>
    /// Enumeration representing the status of a service
    /// </summary>
    public enum ServiceStatus
    {
        /// <summary>Service was never started or has been stopped</summary>
        Stopped,
        /// <summary>Started successfully</summary>
        Started,
        /// <summary>Service failed to start and is unavailable</summary>
        Error
    }

    /// <summary>
    /// The IService interface is implemented by all Services. Although it
    /// is extensible, it does not reside in the Extensibility namespace
    /// because it is so widely used by the engine.
    /// </summary>
    [TypeExtensionPoint(
        Description="Provides a service within the engine and possibly externally as well.")]
    public interface IService
    {
        /// <summary>
        /// The ServiceContext
        /// </summary>
        IServiceLocator ServiceContext { get; set; }

        /// <summary>
        /// Gets the ServiceStatus of this service
        /// </summary>
        ServiceStatus Status { get;  }

        /// <summary>
        /// Initialize the Service
        /// </summary>
        void StartService();

        /// <summary>
        /// Do any cleanup needed before terminating the service
        /// </summary>
        void StopService();
    }
}
