// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using NUnit.Engine.Internal;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Summary description for UserSettingsService.
    /// </summary>
    public class SettingsService : SettingsStore, IService
    {
        private const string SETTINGS_FILE = "Nunit30Settings.xml";

        public SettingsService(bool writeable)
            : base(Path.Combine(NUnitConfiguration.ApplicationDirectory, SETTINGS_FILE), writeable) { }

        public IServiceLocator ServiceContext { get; set; }

        public ServiceStatus Status { get; private set; }

        public void StartService()
        {
            try
            {
                LoadSettings();

                Status = ServiceStatus.Started;
            }
            catch
            {
                Status = ServiceStatus.Error;
                throw;
            }
        }

        public void StopService()
        {
            try
            {
                SaveSettings();
            }
            finally
            {
                Status = ServiceStatus.Stopped;
            }
        }
    }
}
