// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Diagnostics;

namespace NUnit.Engine.Internal
{
    public class ProvidedPathsAssemblyResolver
    {
        static readonly ILogger log = InternalTrace.GetLogger(typeof(ProvidedPathsAssemblyResolver));

        static readonly string THIS_ASSEMBLY_LOCATION = Assembly.GetExecutingAssembly().Location;

        public ProvidedPathsAssemblyResolver()
        {
            _resolutionPaths = new List<string>();
        }

        public void Install()
        {
            Debug.Assert(AppDomain.CurrentDomain.IsDefaultAppDomain());
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

            AddPath(THIS_ASSEMBLY_LOCATION);
        }

        public void AddPath(string dirPath)
        {
            if (!_resolutionPaths.Contains(dirPath))
            {
                _resolutionPaths.Add(dirPath);
                log.Debug("Added path " + dirPath);
            }
        }

        public void AddPathFromFile(string filePath)
        {
            string dirPath = Path.GetDirectoryName(filePath)!;
            AddPath(dirPath);
        }

        public void RemovePath(string dirPath)
        {
            _resolutionPaths.Remove(dirPath);
        }

        public void RemovePathFromFile(string filePath)
        {
            string dirPath = Path.GetDirectoryName(filePath)!;
            RemovePath(dirPath);
        }

        Assembly? AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            foreach (string path in _resolutionPaths)
            {
                string filename = new AssemblyName(args.Name!).Name + ".dll";
                string fullPath = Path.Combine(path, filename);
                try
                {
                    if (File.Exists(fullPath))
                    {
                        return Assembly.LoadFrom(fullPath);
                    }
                }
                catch (Exception)
                {
                    // Resolution at this path failed. Do not interrupt the process; try the next path.
                }
            }

            return null;
        }

        readonly List<string> _resolutionPaths;
    }
}
