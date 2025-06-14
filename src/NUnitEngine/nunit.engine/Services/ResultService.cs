﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NUnit.Engine.Extensibility;
using NUnit.Extensibility;

namespace NUnit.Engine.Services
{
    public class ResultService : Service, IResultService
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(ResultService));

        private readonly string[] BUILT_IN_FORMATS = new string[] { "nunit3", "cases", "user" };
        private IEnumerable<ExtensionNode>? _extensionNodes;

        private string[]? _formats;

        [MemberNotNull(nameof(_formats))]
        public string[] Formats
        {
            get
            {
                if (_formats is null)
                {
                    var formatList = new List<string>(BUILT_IN_FORMATS);

                    if (_extensionNodes is not null)
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
        public IResultWriter GetResultWriter(string format, params object?[]? args)
        {
            log.Debug($"GetResultWriter for format {format}");

            switch (format)
            {
                case "nunit3":
                    log.Debug("  Returning NUnit3XmlResultWriter");
                    return new NUnit3XmlResultWriter();
                case "cases":
                    log.Debug("  Returning TestCaseResultWriter");
                    return new TestCaseResultWriter();
                case "user":
                    log.Debug("  Returning XmlTransformResultWriter");
                    return new XmlTransformResultWriter(args!);

                default:
                    if (_extensionNodes is not null)
                        foreach (var node in _extensionNodes)
                            foreach (var supported in node.GetValues("Format"))
                                if (supported == format && node is ExtensionNode)
                                {
                                    log.Debug($"  Returning {node.TypeName}");
                                    return (IResultWriter)node.ExtensionObject;
                                }
                    throw new NUnitEngineException("ResultWriter not found for format: " + format);
            }
        }

        public override void StartService()
        {
            try
            {
                if (ServiceContext is null)
                    throw new InvalidOperationException("Only services that have a ServiceContext can be started.");

                var extensionService = ServiceContext.GetService<ExtensionService>();

                if (extensionService is not null && extensionService.Status == ServiceStatus.Started)
                    _extensionNodes = extensionService.GetExtensionNodes<IResultWriter>();

                // If there is no extension service, we start anyway using built-in writers
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
