// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics.CodeAnalysis;

namespace NUnit.Engine
{
    /// <summary>
    /// IServiceLocator allows clients to locate any NUnit services
    /// for which the interface is referenced. In normal use, this
    /// limits it to those services using interfaces defined in the
    /// nunit.engine.api assembly.
    /// </summary>
    public interface IServiceLocator
    {
        /// <summary>
        /// Return an instance of the specified type of service
        /// </summary>
        /// <exception cref="NUnitEngineException">If service is not available</exception>
        T GetService<T>()
            where T : class;

        /// <summary>
        /// Return true and set service to an instance of the specified
        /// type of service if it is available, otherwise return false;
        /// </summary>
        bool TryGetService<T>([NotNullWhen(true)] out T? service)
            where T : class;
    }
}
