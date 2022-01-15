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

        public bool FailToStart { get; set; }

        public bool FailedToStop { get; set; }
    }
}
