// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Services.Tests.Fakes
{
    interface IFakeService
    { }

    public class FakeService : IFakeService, IService
    {
        IServiceLocator IService.ServiceContext { get; set; }

        private ServiceStatus _status;
        ServiceStatus IService.Status
        {
            get { return _status; }
        }

        void IService.StartService()
        {
            _status = FailToStart
                ? ServiceStatus.Error
                : ServiceStatus.Started;
        }

        void IService.StopService()
        {
            _status = ServiceStatus.Stopped;
            if (FailedToStop)
                throw new ArgumentException(nameof(FailedToStop));
        }

        // Set to true to cause the service to give
        // an error result when started
        public bool FailToStart { get; set; }

        // Set to true to cause the service to give
        // an error result when stopped
        public bool FailedToStop { get; set; }
    }
}
