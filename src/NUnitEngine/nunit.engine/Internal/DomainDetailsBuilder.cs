// ***********************************************************************
// Copyright (c) 2017 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NUnit.Engine.Internal
{
    /// <summary>
    /// DomainDetailsBuilder provides human readable information on 
    /// an AppDomain, to assist with debugging.
    /// </summary>
    internal static class DomainDetailsBuilder
    {
        /// <summary>
        /// Get human readable string containing details of AppDomain.
        /// </summary>
        /// <param name="domain">AppDomain to get details on.</param>
        /// <param name="errMsg">An optional overall error message e.g. The messaged from an AppDomainUnload exception.</param>
        public static string DetailsFor(AppDomain domain, string errMsg = null)
        {
            var sb = new StringBuilder();
            if (errMsg != null) sb.AppendLine(errMsg);

            try
            {
                sb.AppendLine($"AppDomain Name: {domain.FriendlyName}");
                sb.AppendLine($"AppDomain Base Directory: {domain.BaseDirectory}");

                var reflectionLoadedAssemblies = new List<Assembly>(domain.ReflectionOnlyGetAssemblies());

                if (reflectionLoadedAssemblies.Count != 0)
                {
                    sb.AppendLine("--- Assemblies loaded in current AppDomain via reflection ---");
                    foreach (var assembly in reflectionLoadedAssemblies)
                        WriteAssemblyInformation(sb, assembly);
                }
            }
            catch (AppDomainUnloadedException)
            {
                sb.AppendLine("App Domain was unloaded before all details could be read.");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error trying to read AppDomain details {ex.Message}");
                sb.AppendLine($"{ex.StackTrace}");
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
