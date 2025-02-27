// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using NUnit.Common;
using NUnit.ConsoleRunner.Options;
using NUnit.Engine;

namespace NUnit.ConsoleRunner
{
    /// <summary>
    /// This class provides the entry point for the console runner.
    /// </summary>
    public class Program
    {
        //static Logger log = InternalTrace.GetLogger(typeof(Runner));
        static readonly ConsoleOptions Options = new ConsoleOptions(new FileSystem());
        private static ExtendedTextWriter _outWriter;

        // This has to be lazy otherwise NoColor command line option is not applied correctly
        private static ExtendedTextWriter OutWriter
        {
            get
            {
                if (_outWriter == null) _outWriter = new ColorConsoleWriter(!Options.NoColor);

                return _outWriter;
            }
        }

        [STAThread]
        public static int Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelHandler);

            try
            {
                Options.Parse(Options.PreParse(args));
            }
            catch (OptionException ex)
            {
                WriteHeader();
                OutWriter.WriteLine(ColorStyle.Error, string.Format(ex.Message, ex.OptionName));
                return ConsoleRunner.INVALID_ARG;
            }

            if (!string.IsNullOrEmpty(Options.ConsoleEncoding))
            {
                try
                {
                    Console.OutputEncoding = Encoding.GetEncoding(Options.ConsoleEncoding);
                }
                catch (Exception error)
                {
                    WriteHeader();
                    OutWriter.WriteLine(ColorStyle.Error, string.Format("Unsupported Encoding, {0}", error.Message));
                    return ConsoleRunner.INVALID_ARG;
                }
            }

            try
            {
                if (Options.ShowVersion || !Options.NoHeader)
                    WriteHeader();

                if (Options.ShowHelp || args.Length == 0)
                {
                    WriteHelpText();
                    return ConsoleRunner.OK;
                }

                // We already showed version as a part of the header
                if (Options.ShowVersion)
                    return ConsoleRunner.OK;

                if (Options.WarningMessages.Count != 0)
                {
                    foreach (string message in Options.WarningMessages)
                        OutWriter.WriteLine(ColorStyle.Warning, message);

                    OutWriter.WriteLine();
                }

                if (!Options.Validate())
                {
                    using (new ColorConsole(ColorStyle.Error))
                    {
                        foreach (string message in Options.ErrorMessages)
                            Console.Error.WriteLine(message);
                    }

                    return ConsoleRunner.INVALID_ARG;
                }

                using (ITestEngine engine = TestEngineActivator.CreateInstance())
                {
                    if (Options.WorkDirectory != null)
                        engine.WorkDirectory = Options.WorkDirectory;

                    //if (Options.InternalTraceLevel != null)
                    //    engine.InternalTraceLevel = (InternalTraceLevel)Enum.Parse(typeof(InternalTraceLevel), Options.InternalTraceLevel);
                    // See PR 1114  https://github.com/nunit/nunit-console/pull/1214/files  
                    engine.InternalTraceLevel = Options.InternalTraceLevel != null
                        ? (InternalTraceLevel)Enum.Parse(typeof(InternalTraceLevel), Options.InternalTraceLevel)
                        : InternalTraceLevel.Off;

                    try
                    {
                        return new ConsoleRunner(engine, Options, OutWriter).Execute();
                    }
                    catch (RequiredExtensionException ex)
                    {
                        OutWriter.WriteLine(ColorStyle.Error, ex.Message);
                        return ConsoleRunner.INVALID_ARG;
                    }
                    catch (TestSelectionParserException ex)
                    {
                        OutWriter.WriteLine(ColorStyle.Error, ex.Message);
                        return ConsoleRunner.INVALID_ARG;
                    }
                    catch (FileNotFoundException ex)
                    {
                        OutWriter.WriteLine(ColorStyle.Error, ex.Message);
                        return ConsoleRunner.INVALID_ASSEMBLY;
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        OutWriter.WriteLine(ColorStyle.Error, ex.Message);
                        return ConsoleRunner.INVALID_ASSEMBLY;
                    }
                    catch (Exception ex)
                    {
                        OutWriter.WriteLine(ColorStyle.Error, ExceptionHelper.BuildMessage(ex));
                        OutWriter.WriteLine();
                        OutWriter.WriteLine(ColorStyle.Error, ExceptionHelper.BuildMessageAndStackTrace(ex));
                        return ConsoleRunner.UNEXPECTED_ERROR;
                    }
                    finally
                    {
                        if (Options.WaitBeforeExit)
                        {
                            using (new ColorConsole(ColorStyle.Warning))
                            {
                                Console.Out.WriteLine("\nPress any key to continue . . .");
                                Console.ReadKey(true);
                            }
                        }
                    }
                }
            }
            finally
            {
                Console.ResetColor();
            }
        }

        private static void WriteHeader()
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            var versionBlock = FileVersionInfo.GetVersionInfo(entryAssembly.ManifestModule.FullyQualifiedName);

            var version = versionBlock.ProductVersion;
            int plus = version.IndexOf('+');
            if (plus > 0) version = version.Substring(0, plus);
            var header = $"{versionBlock.ProductName} {version}";

#if NETFRAMEWORK
            object[] configurationAttributes = entryAssembly.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);
#else
            var configurationAttributes = entryAssembly.GetCustomAttributes<AssemblyConfigurationAttribute>().ToArray();
#endif

            if (configurationAttributes.Length > 0)
            {
                string configuration = ((AssemblyConfigurationAttribute)configurationAttributes[0]).Configuration;
                if (!string.IsNullOrEmpty(configuration)) header += $" ({configuration})";
            }

            OutWriter.WriteLine(ColorStyle.Header, header);
            OutWriter.WriteLine(ColorStyle.SubHeader, versionBlock.LegalCopyright);
            OutWriter.WriteLine(ColorStyle.SubHeader, DateTime.Now.ToString(CultureInfo.CurrentCulture.DateTimeFormat.FullDateTimePattern));
            OutWriter.WriteLine();
        }

        private static void WriteHelpText()
        {
            OutWriter.WriteLine();
            OutWriter.WriteLine(ColorStyle.Header, "NUNIT3-CONSOLE [inputfiles] [options]");
            OutWriter.WriteLine();
            OutWriter.WriteLine(ColorStyle.SectionHeader, "Description:");
#if NETFRAMEWORK
            OutWriter.WriteLine(ColorStyle.Default, """
                  The standard NUnit Console Runner runs a set of NUnit tests from the
                  console command-line. By default, all tests are run using separate agents
                  for each test assembly. This allows each assembly to run independently
                  and allows each assembly to run under the appropriate target runtime.

            """);
#else
            OutWriter.WriteLine(ColorStyle.Default, """
                  The NetCore Console Runner runs a set of NUnit tests from the console
                  command-line. All tests are run in-process and therefore execute under
                  the same runtime as the runner itself. A number of options supported by
                  the standard console runner are not available using the NetCore runner.
                  See \"Limitations\" below for more information.

            """);
#endif
            OutWriter.WriteLine(ColorStyle.SectionHeader, "InputFiles:");
            OutWriter.WriteLine(ColorStyle.Default, "      One or more assemblies or test projects of a recognized type.");
            OutWriter.WriteLine();
            OutWriter.WriteLine(ColorStyle.SectionHeader, "Options:");
            using (new ColorConsole(ColorStyle.Default))
            {
                OutWriter.WriteLine("""
                      @FILE                  Specifies the name(or path) of a FILE containing
                                             additional command-line arguments to be inserted
                                             at the point where the @FILE expression appears.
                                             Each line in the file represents one argument
                                             to the console runner. If an option takes a value,
                                             that value must appear on the  same line.
                
                """);

                Options.WriteOptionDescriptions(Console.Out);
            }
            OutWriter.WriteLine();
            OutWriter.WriteLine(ColorStyle.SectionHeader, "Operation:");
            OutWriter.WriteLine(ColorStyle.Default, """
                    By default, this command runs the tests contained in the");
                    assemblies and projects specified. If the --explore option");
                    is used, no tests are executed but a description of the tests");
                    is saved in the specified or default format.

                    The --where option is intended to extend or replace the earlier
                    --test, --include and --exclude options by use of a selection expression
                    describing exactly which tests to use. Examples of usage are:
                        --where:cat==Data
                        --where \"method =~ /DataTest*/ && cat = Slow\"
                
                    Care should be taken in combining --where with --test or --testlist.
                    The test and where specifications are implicitly joined using &&, so
                    that BOTH sets of criteria must be satisfied in order for a test to run.
                    See the docs for more information and a full description of the syntax
                    information and a full description of the syntax.
                
                    Several options that specify processing of XML output take
                    an output specification as a value. A SPEC may take one of
                    the following forms:
                        --OPTION:filename
                        --OPTION:filename;format=formatname
                        --OPTION:filename;transform=xsltfile
                
                    The --result option may use any of the following formats
                        nunit3 - the native XML format for NUnit 3
                        nunit2 - legacy XML format used by earlier releases of NUnit
                                Requires the engine extension NUnitV2ResultWriter.
                
                    The --explore option may use any of the following formats:
                        nunit3 - the native XML format for NUnit 3
                        cases  - a text file listing the full names of all test cases.
                    If --explore is used without any specification following, a list of
                    test cases is output to the writer.
                
                    If none of the options {--result, --explore, --noresult} is used,
                    NUnit saves the results to TestResult.xml in nunit3 format.
                
                    Any transforms provided must handle input in the native nunit3 format.
                
                    To be able to load NUnit projects, file type .nunit, the engine
                    extension NUnitProjectLoader is required. For Visual Studio projects
                    and solutions the engine extension VSProjectLoader is required.
            """);
#if NETCOREAPP
            OutWriter.WriteLine();
            OutWriter.WriteLine(ColorStyle.SectionHeader, "Limitations:");
            OutWriter.WriteLine(ColorStyle.Default, """
                    The NetCore Runner is primarily intended for use as a dotnet  tool.
                    When used in this way, a single assembly is usually being tested and
                    the assembly must be compatible with execution under the same runtime
                    as the runner itself, normally .NET 6.0.
                
                    Using this runner, the following options are not available. A brief
                    rationale is given for each option excluded.
                    --configFile              Config of the runner itself is used.
                    --process                 Not designed to run out of process.
                    --inprocess               Redundant. We always run in process.
                    --domain                  Not applicable to .NET Core.
                    --framework               Runtime of the runner is used.
                    --x86                     Bitness of the runner is used.
                    --shadowcopy              Not available.
                    --loaduserprofile         Not available.
                    --agents                  No agents are used.
                    --debug                   Debug in process directly.
                    --pause                   Used for debugging agents.
                    --set-principal-policy    Not available.
                    --debug-agent             No agents are used.
            """);
#endif
        }

        private static void CancelHandler(object sender, ConsoleCancelEventArgs args)
        {
            Console.ResetColor();
        }
    }
}
