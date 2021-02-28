// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Services
{
    public class ResultService : Service, IResultService
    {
        private readonly string[] BUILT_IN_FORMATS = new string[] { "nunit3", "cases", "user" };
        private IEnumerable<ExtensionNode> _extensionNodes;

        private string[] _formats;
        public string[] Formats
        {
            get
            {
                if (_formats == null)
                {
                    var formatList = new List<string>(BUILT_IN_FORMATS);

                    foreach (var node in _extensionNodes)
                        foreach (var format in node.GetValues("Format"))
                            formatList.Add(format);

                    _formats = formatList.ToArray();
                }

                return _formats;
            }
        }

        /// <summary>
        /// Gets a ResultWriter for a given format and set of arguments.
        /// </summary>
        /// <param name="format">The name of the format to be used</param>
        /// <param name="args">A set of arguments to be used in constructing the writer or null if non arguments are needed</param>
        /// <returns>An IResultWriter</returns>
        public IResultWriter GetResultWriter(string format, object[] args)
        {
            switch (format)
            {
                case "nunit3":
                    return new NUnit3XmlResultWriter();
                case "cases":
                    return new TestCaseResultWriter();
                case "user":
                    return new XmlTransformResultWriter(args);

                default:
                    foreach (var node in _extensionNodes)
                        foreach (var supported in node.GetValues("Format"))
                            if (supported == format)
                                return node.ExtensionObject as IResultWriter;
                    return null;
            }
        }

        public override void StartService()
        {
            try
            {
                var extensionService = ServiceContext.GetService<ExtensionService>();

                if (extensionService != null && extensionService.Status == ServiceStatus.Started)
                    _extensionNodes = extensionService.GetExtensionNodes<IResultWriter>();

                // If there is no extension service, we start anyway using builtin writers
                Status = ServiceStatus.Started;
            }
            catch
            {
                Status = ServiceStatus.Error;
                throw;
            }
        }
    }
}
