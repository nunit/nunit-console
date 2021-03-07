// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine
{
    /// <summary>
    /// IServiceLocator allows clients to locate any NUnit services
    /// for which the interface is referenced. In normal use, this
    /// linits it to those services using interfaces defined in the 
    /// nunit.engine.api assembly.
    /// </summary>
    public interface IServiceLocator
    {
        /// <summary>
        /// Return a specified type of service
        /// </summary>
        T GetService<T>() where T : class;

        /// <summary>
        /// Return a specified type of service
        /// </summary>
        object GetService(Type serviceType);
    }
}
