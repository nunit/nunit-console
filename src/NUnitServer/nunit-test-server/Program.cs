// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Engine;
using NUnit.Engine.Services;

namespace NUnit.Remote.Hosting
{
    /// <summary>
    /// Owns one explicitly started provider host. The launching process keeps standard
    /// input open for the session and closes it to request orderly shutdown.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Starts the engine through its public API and publishes readiness only after
        /// normal extension discovery has completed and the loopback listener is active.
        /// </summary>
        /// <param name="args">An absolute readiness filename and optional staged launcher root.</param>
        /// <returns>Zero after confirmed shutdown; one on startup or cleanup failure.</returns>
        private static int Main(string[] args)
        {
            try
            {
                if (args.Length is < 1 or > 2 || !Path.IsPathFullyQualified(args[0])
                    || (args.Length == 2 && !Path.IsPathFullyQualified(args[1])))
                    throw new ArgumentException("Supply an absolute readiness filename and optional staged launcher root.");

                string receipt = args[0];
                if (File.Exists(receipt) || File.Exists(receipt + ".tmp"))
                    throw new IOException("The readiness receipt must not already exist.");

                using ITestEngine engine = new TestEngine
                {
                    WorkDirectory = Environment.CurrentDirectory,
                    InternalTraceLevel = InternalTraceLevel.Off
                };
                var extensions = engine.Services.GetService<IExtensionService>();
                extensions.FindExtensionAssemblies(AppContext.BaseDirectory);
                extensions.InstallExtensions();
                var installed = extensions.Extensions.ToArray();
                if (args.Length == 2)
                {
                    string allowed = Path.TrimEndingDirectorySeparator(Path.GetFullPath(args[1])) + Path.DirectorySeparatorChar;
                    var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                    if (installed.Any(extension => !Path.GetFullPath(extension.AssemblyPath).StartsWith(allowed, comparison)))
                        throw new InvalidOperationException("An extension was discovered outside the staged launcher root.");
                }
                var provider = engine.Services.GetService<ITestAgentProvider>()
                    ?? throw new InvalidOperationException("The engine did not supply its agent provider.");
                using (var server = new RemoteProviderServer(provider))
                {
                    server.Start();
                    var ready = new XElement("ready",
                        new XAttribute("port", server.EndPoint.Port),
                        new XAttribute("pid", Environment.ProcessId),
                        new XAttribute("agents", provider.AvailableAgents.Count),
                        new XAttribute("engine", typeof(TestEngine).Assembly.Location),
                        installed.Select(extension => new XElement("extension", new XAttribute("assembly", extension.AssemblyPath))));
                    File.WriteAllText(receipt + ".tmp", ready.ToString());
                    File.Move(receipt + ".tmp", receipt);
                    // Only EOF owns orderly shutdown; an incidental input line must not stop the server.
                    Console.In.ReadToEnd();
                }
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }
    }
}
