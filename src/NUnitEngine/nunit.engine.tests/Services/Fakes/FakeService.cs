// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine.Services.Tests.Fakes
{
    public class FakeService : IService
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
        }

        // Set to true to cause the service to give
        // an error result when started
        public bool FailToStart { get; set; }
    }
}
