// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine.Communication.Transports
{
    /// <summary>
    /// The ITransport interface is implemented by a class
    /// providing a communication interface for another class.
    /// </summary>
    public interface ITransport
    {
        bool Start();
        void Stop();
    }
}
