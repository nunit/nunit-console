// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NUnit.TestData.SessionHostProbe
{
    /// <summary>
    /// Supplies controlled host-lifetime signals for deployment acceptance tests.
    /// It never opens a listener, loads an agent or executes a test package.
    /// </summary>
    internal static class Program
    {
        /// <summary>Publishes readiness as requested and exits after the owner's lifetime input closes.</summary>
        /// <param name="args">The readiness path and unused staged launcher-root argument.</param>
        /// <returns>Zero for a confirmed normal exit, or a deliberately selected failure code.</returns>
        private static async Task<int> Main(string[] args)
        {
            if (args.Length != 2 || !Path.IsPathFullyQualified(args[0]))
                throw new ArgumentException("The probe requires an absolute readiness path and launcher root.");
            string mode = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "probe-mode.txt"));
            if (mode is not ("exit-without-ready" or "wait-before-ready" or "ready-success" or "nonzero-on-eof" or "ignore-eof"))
                throw new ArgumentException("Unknown lifecycle probe mode.");
            string control = Path.GetDirectoryName(args[0])!;
            Console.WriteLine("Lifecycle probe: " + mode);
            if (mode == "exit-without-ready")
                return 0;
            if (mode != "wait-before-ready")
            {
                new XElement("ready", new XAttribute("pid", Environment.ProcessId),
                    new XAttribute("port", 1), new XAttribute("agents", 1)).Save(args[0] + ".tmp");
                File.Move(args[0] + ".tmp", args[0]);
            }
            await Console.In.ReadToEndAsync();
            int exit = mode == "nonzero-on-eof" ? 17 : 0;
            if (mode == "ignore-eof")
            {
                // A test can release the retained child explicitly; the independent bound
                // also prevents this fault probe from waiting indefinitely after a failed test.
                string release = Path.Combine(control, "release.txt");
                var deadline = Stopwatch.StartNew();
                while (!File.Exists(release) && deadline.Elapsed < TimeSpan.FromSeconds(20))
                    await Task.Delay(20);
                if (!File.Exists(release))
                    exit = 19;
            }
            File.WriteAllText(Path.Combine(control, "probe-exit.txt"), exit.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (exit != 0)
                Console.Error.WriteLine("Controlled lifecycle probe exit: " + exit);
            return exit;
        }
    }
}
