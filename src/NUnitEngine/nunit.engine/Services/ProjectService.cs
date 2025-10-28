// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine.Extensibility;
using NUnit.Extensibility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Summary description for ProjectService.
    /// </summary>
    public class ProjectService : Service, IProjectService
    {
        private readonly Dictionary<string, ExtensionNode> _extensionIndex = new Dictionary<string, ExtensionNode>();

        public bool CanLoadFrom(string path)
        {
            var node = GetNodeForPath(path);
            if (node is not null)
            {
                var loader = node.ExtensionObject as IProjectLoader;
                if (loader is not null && loader.CanLoadFrom(path))
                    return true;
            }

            return false;
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
            Guard.ArgumentNotNull(package, nameof(package));
            Guard.ArgumentValid(package.SubPackages.Count == 0, "Package is already expanded", nameof(package));

            string path = package.FullName!;
            if (!File.Exists(path))
                return;

            IProject project = LoadFrom(path).ShouldNotBeNull("Unable to load project " + path);

            string? activeConfig = package.Settings.GetValueOrDefault(SettingDefinitions.ActiveConfig);
            if (activeConfig is null)
                activeConfig = project.ActiveConfigName;
            else
                Guard.ArgumentValid(project.ConfigNames.Contains(activeConfig), $"Requested configuration {activeConfig} was not found", nameof(package));

            TestPackage tempPackage = project.GetTestPackage(activeConfig);

            // Add info about the configurations to the project package
            tempPackage.AddSetting(SettingDefinitions.ActiveConfig.WithValue(activeConfig));
            tempPackage.AddSetting(SettingDefinitions.ConfigNames.WithValue(new List<string>(project.ConfigNames).ToArray()));

            // The original package held overrides, so don't change them, but
            // do apply any settings specified within the project itself.
            foreach (var setting in tempPackage.Settings)
            {
                if (package.Settings.HasSetting(setting.Name)) // Don't override settings from command line
                    continue;

                package.Settings.Add(setting);
            }

            foreach (var subPackage in tempPackage.SubPackages)
                package.AddSubPackage(subPackage);

            // If no config file is specified (by user or by the project loader) check
            // to see if one exists in same directory as the package. If so, we
            // use it. If not, each assembly will use its own config, if present.
            if (!package.Settings.HasSetting(SettingDefinitions.ConfigurationFile))
            {
                var packageConfig = Path.ChangeExtension(path, ".config");
                if (File.Exists(packageConfig))
                    package.Settings.Add(SettingDefinitions.ConfigurationFile.WithValue(packageConfig));
            }
        }

        public override void StartService()
        {
                base.StartService();

                try
                {
                    var extensionService = ServiceContext.GetService<ExtensionService>();

                    if (extensionService is null)
                        Status = ServiceStatus.Started;
                    else if (extensionService.Status != ServiceStatus.Started)
                        Status = ServiceStatus.Error;
                    else
                    {
                        Status = ServiceStatus.Started;

                        var extensionNodes = new List<ExtensionNode>();
                        extensionNodes.AddRange(extensionService.GetExtensionNodes<IProjectLoader>());

                        foreach (var node in extensionNodes)
                        {
                            foreach (string ext in node.GetValues("FileExtension"))
                            {
                                if (ext is not null)
                                {
                                    if (_extensionIndex.ContainsKey(ext))
                                        throw new NUnitEngineException($"ProjectLoader extension {ext} is already handled by another extension.");

                                    _extensionIndex.Add(ext, node);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    Status = ServiceStatus.Error;
                    throw;
                }
        }

        private IProject? LoadFrom(string path)
        {
            if (File.Exists(path))
            {
                ExtensionNode? node = GetNodeForPath(path);
                if (node is not null)
                {
                    var loader = node.ExtensionObject as IProjectLoader;
                    if (loader is not null && loader.CanLoadFrom(path))
                        return loader.LoadFrom(path);
                }
            }

            return null;
        }

        private ExtensionNode? GetNodeForPath(string path)
        {
            var ext = Path.GetExtension(path);
            if (string.IsNullOrEmpty(ext))
                return null;

            if (_extensionIndex.TryGetValue(ext, out ExtensionNode? node))
                return node;

            return null;
        }
    }
}
