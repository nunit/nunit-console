// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace NUnit.Engine.Services.Tests
{
    public class SettingsServiceTests
    {
        private SettingsService _settingsService;

        [SetUp]
        public void CreateServiceContext()
        {
            var services = new ServiceContext();
            _settingsService = new SettingsService(false);
            services.Add(_settingsService);
            services.ServiceManager.StartServices();
        }

        [Test]
        public void ServiceIsStarted()
        {
            Assert.That(_settingsService.Status, Is.EqualTo(ServiceStatus.Started));
        }
    }
}
