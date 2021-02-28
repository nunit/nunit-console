// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NUnit.Common;

namespace NUnit.Engine.Internal
{
    /// <summary>
    /// DomainDetailsBuilder provides human readable information on
    /// an application domain, to assist with debugging.
    /// </summary>
    internal static class DomainDetailsBuilder
    {
        private static readonly ILogger Log = InternalTrace.GetLogger(nameof(DomainDetailsBuilder));

        /// <summary>
        /// Get human readable string containing details of application domain.
        /// </summary>
        /// <param name="domain">Application domain to get details on.</param>
        /// <param name="errMsg">An optional overall error message.</param>
        public static string DetailsFor(AppDomain domain, string errMsg = null)
        {
            var sb = new StringBuilder();
            if (errMsg != null) sb.AppendLine(errMsg);

            try
            {
                sb.AppendLine($"Application domain name: {domain.FriendlyName}");
                sb.AppendLine($"Application domain BaseDirectory: {domain.BaseDirectory}");

                var reflectionLoadedAssemblies = new List<Assembly>(domain.ReflectionOnlyGetAssemblies());

                if (reflectionLoadedAssemblies.Count != 0)
                {
                    sb.AppendLine("--- Assemblies loaded in current application domain via reflection ---");
                    foreach (var assembly in reflectionLoadedAssemblies)
                        WriteAssemblyInformation(sb, assembly);
                }
            }
            catch (AppDomainUnloadedException ex)
            {
                sb.AppendLine("Application domain was unloaded before all details could be read.");
                Log.Error(ExceptionHelper.BuildMessageAndStackTrace(ex));
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error trying to read application domain details: {ex.Message}");
                Log.Error(ExceptionHelper.BuildMessageAndStackTrace(ex));
            }
            return sb.ToString();
        }

        private static void WriteAssemblyInformation(StringBuilder sb, Assembly assembly)
        {
            sb.AppendLine(assembly.FullName);
            sb.AppendLine(assembly.ImageRuntimeVersion);
            sb.AppendLine(assembly.Location);
            sb.AppendLine("-----------------");
        }
    }
}
#endif