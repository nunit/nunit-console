// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine.Communication.Transports
{
    /// <summary>
    /// The ITestAgencyTransport interface is implemented by a
    /// class providing communication for a TestAgency.
    /// </summary>
    public interface ITestAgencyTransport
    {
        string ServerUrl { get; }
        bool Start();
        void Stop();
    }
}
