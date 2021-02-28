// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace NUnit.Engine.Services.Tests
{
    public class ProjectServiceTests
    {
        private ProjectService _projectService;

        [SetUp]
        public void CreateServiceContext()
        {
            var services = new ServiceContext();
            services.Add(new ExtensionService());
            _projectService = new ProjectService();
            services.Add(_projectService);
            services.ServiceManager.StartServices();
        }

        [Test]
        public void ServiceIsStarted()
        {
            Assert.That(_projectService.Status, Is.EqualTo(ServiceStatus.Started));
        }
    }
}
