// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine.Extensibility;

namespace NUnit.Extensibility.Wrappers
{
    /// <summary>
    /// Wrapper class for project loaders which use the NUnit3 Engine API
    /// </summary>
    public class ProjectLoaderWrapper : ExtensionWrapper, IProjectLoader
    {
        public ProjectLoaderWrapper(object projectLoader) : base(projectLoader) { }

        public bool CanLoadFrom(string path)
        {
            log.Debug("CanLoadFrom");
            return Invoke<bool>(nameof(CanLoadFrom), path);
        }

        public IProject LoadFrom(string path)
        {
            log.Debug("LoadFrom");
            return Invoke<IProject>(nameof(LoadFrom), path);
        }
    }
}
