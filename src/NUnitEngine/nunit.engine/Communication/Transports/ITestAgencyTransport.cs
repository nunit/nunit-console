// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine.Communication.Transports
{
    public interface ITestAgencyTransport : ITransport
    {
        string ServerUrl { get; }
    }
}
