// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Engine.Extensibility;
using NUnit.Extensibility;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Summary description for ProjectService.
    /// </summary>
    public class ProjectService : Service, IProjectService
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(ProjectService));

        private IEnumerable<ExtensionNode>? _extensionNodes;
        private ExtensionService? _extensionService;

        public bool CanLoadFrom(string path)
        {
            ExtensionNode? node = GetNodeForPath(path);
            if (node != null && ((IProjectLoader)node.ExtensionObject).CanLoadFrom(path))
            {
                log.Debug($"{node.ExtensionObject.GetType()} can load {path}");
                return true;
            }

            log.Debug($"Cannot load {path}");
            if (node == null)
                log.Debug("    No Extension was found");
            return false;
        }

        private IProject? LoadFrom(string path)
        {
            if (File.Exists(path))
            {
                ExtensionNode? node = GetNodeForPath(path);
                IProjectLoader loader;
                if (node != null && (loader = (IProjectLoader)node.ExtensionObject).CanLoadFrom(path))
                {
                    log.Debug($"Using loader {loader.GetType()}");
                    return loader.LoadFrom(path);
                }
            }

            return null;
        }

        private ExtensionNode? GetNodeForPath(string path)
        {
            var ext = Path.GetExtension(path);

            if (string.IsNullOrEmpty(ext) || _extensionService == null)
                return null;

            if (_extensionNodes == null)
                _extensionNodes = _extensionService.GetExtensionNodes<IProjectLoader>();

            foreach (var node in _extensionNodes)
                foreach (string extNode in node.GetValues("FileExtension"))
                    if (extNode == ext)
                        return node;

            return null;
        }

        /// <summary>
        /// Expands a TestPackage based on a known project format, populating it
        /// with the project contents and any settings the project provides.
        /// Note that the package file path must be checked to ensure that it is
        /// a known project format before calling this method.
        /// </summary>
        /// <param name="package">The TestPackage to be expanded</param>
        public void ExpandProjectPackage(TestPackage package)
        {
            log.Debug($"Expanding package {package.Name}");

            Guard.ArgumentNotNull(package, "package");
            Guard.ArgumentValid(package.SubPackages.Count == 0, "Package is already expanded", "package");

            string path = package.FullName!;
            if (!File.Exists(path))
                return;

            IProject project = LoadFrom(path).ShouldNotBeNull("Unable to load project " + path);
            log.Debug("Got project");

            string? activeConfig = package.GetSetting(EnginePackageSettings.ActiveConfig, (string?)null);
            log.Debug($"Got ActiveConfig setting {activeConfig ?? "<null>"}");
            if (activeConfig == null)
                activeConfig = project.ActiveConfigName;
            else
                Guard.ArgumentValid(project.ConfigNames.Contains(activeConfig), $"Requested configuration {activeConfig} was not found", "package");

            TestPackage tempPackage = project.GetTestPackage(activeConfig);
            log.Debug("Got temp package");

            package.Settings[EnginePackageSettings.ConfigNames] = project.ConfigNames;

            // The original package held overrides, so don't change them, but
            // do apply any settings specified within the project itself.
            foreach (string key in tempPackage.Settings.Keys)
                if (!package.Settings.ContainsKey(key)) // Don't override settings from command line
                    package.Settings[key] = tempPackage.Settings[key];

            foreach (var subPackage in tempPackage.SubPackages)
                package.AddSubPackage(subPackage);

            // If no config file is specified (by user or by the project loader) check
            // to see if one exists in same directory as the package. If so, we
            // use it. If not, each assembly will use its own config, if present.
            if (!tempPackage.Settings.ContainsKey(EnginePackageSettings.ConfigurationFile))
            {
                var packageConfig = Path.ChangeExtension(path, ".config");
                if (File.Exists(packageConfig))
                    package.Settings[EnginePackageSettings.ConfigurationFile] = packageConfig;
            }
        }

        public override void StartService()
        {
            try
            {
                if (ServiceContext == null)
                    throw new InvalidOperationException("Only services that have a ServiceContext can be started.");

                _extensionService = ServiceContext.GetService<ExtensionService>();

                Status = _extensionService == null || _extensionService.Status == ServiceStatus.Started
                    ? ServiceStatus.Started : ServiceStatus.Error;
            }
            catch
            {
                Status = ServiceStatus.Error;
                throw;
            }
        }
    }
}
