// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Text;
using NUnit.ConsoleRunner.Options;
using NUnit.Engine;
using NUnit.TextDisplay;

namespace NUnit.ConsoleRunner
{
    /// <summary>
    /// This class provides the entry point for the console runner.
    /// </summary>
    public class Program
    {
        //static Logger log = InternalTrace.GetLogger(typeof(Runner));
        private static readonly ConsoleOptions Options = new ConsoleOptions(new FileSystem());
        private static ExtendedTextWriter? _outWriter;

        // This has to be lazy otherwise NoColor command line option is not applied correctly
        private static ExtendedTextWriter OutWriter
        {
            get
            {
                if (_outWriter is null)
                    _outWriter = new ColorConsoleWriter(!Options.NoColor);

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
                ResultReporter.WriteHeader(OutWriter);
                WriteErrorMessage(string.Format(ex.Message, ex.OptionName));
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
                    ResultReporter.WriteHeader(OutWriter);
                    WriteErrorMessage(string.Format("Unsupported Encoding, {0}", error.Message));
                    return ConsoleRunner.INVALID_ARG;
                }
            }

            try
            {
                if (Options.ShowVersion || !Options.NoHeader)
                    ResultReporter.WriteHeader(OutWriter);

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

                using (ITestEngine engine = TestEngineActivator.CreateInstance())
                {
                    if (Options.ErrorMessages.Count > 0)
                    {
                        foreach (string message in Options.ErrorMessages)
                            WriteErrorMessage(message);

                        return ConsoleRunner.INVALID_ARG;
                    }

                    if (Options.WorkDirectory is not null)
                        engine.WorkDirectory = Options.WorkDirectory;

                    engine.InternalTraceLevel = Options.InternalTraceLevel is not null
                        ? (InternalTraceLevel)Enum.Parse(typeof(InternalTraceLevel), Options.InternalTraceLevel)
                        : InternalTraceLevel.Off;

                    try
                    {
                        return new ConsoleRunner(engine, Options, OutWriter).Execute();
                    }
                    catch (TestSelectionParserException ex)
                    {
                        WriteErrorMessage(ex.Message);
                        return ConsoleRunner.INVALID_ARG;
                    }
                    catch (FileNotFoundException ex)
                    {
                        WriteErrorMessage(ex.Message);
                        return ConsoleRunner.INVALID_ASSEMBLY;
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        WriteErrorMessage(ex.Message);
                        return ConsoleRunner.INVALID_ASSEMBLY;
                    }
                    catch (Exception ex)
                    {
                        WriteErrorMessage(ExceptionHelper.BuildMessage(ex));
                        OutWriter.WriteLine();
                        WriteErrorMessage(ExceptionHelper.BuildMessageAndStackTrace(ex));
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

        private static void WriteHelpText()
        {
            OutWriter.WriteLine();
            OutWriter.WriteLine(ColorStyle.Header, "NUNIT4-CONSOLE [inputfiles] [options]");
            OutWriter.WriteLine();
            OutWriter.WriteLine(ColorStyle.SectionHeader, "Operation:");
            using (new ColorConsole(ColorStyle.Default))
            {
                OutWriter.WriteLine("      The NetCore Console Runner runs a set of NUnit tests from the console");
                OutWriter.WriteLine("      command-line. All tests are run in-process and therefore execute under");
                OutWriter.WriteLine("      the same runtime as the runner itself. A number of options supported by");
                OutWriter.WriteLine("      the standard console runner are not available using the NetCore runner.");
                OutWriter.WriteLine("      See \"Limitations\" below for more information.");
            }
            OutWriter.WriteLine();
            OutWriter.WriteLine(ColorStyle.SectionHeader, "InputFiles:");
            OutWriter.WriteLine(ColorStyle.Default, "      One or more assemblies or test projects of a recognized type.");
            OutWriter.WriteLine();
            OutWriter.WriteLine(ColorStyle.SectionHeader, "Options:");
            using (new ColorConsole(ColorStyle.Default))
            {
                Options.WriteOptionDescriptions(Console.Out);
            }
            OutWriter.WriteLine();
            OutWriter.WriteLine(ColorStyle.SectionHeader, "Description:");
            using (new ColorConsole(ColorStyle.Default))
            {
                OutWriter.WriteLine("      By default, this command runs the tests contained in the");
                OutWriter.WriteLine("      assemblies and projects specified. If the --explore option");
                OutWriter.WriteLine("      is used, no tests are executed but a description of the tests");
                OutWriter.WriteLine("      is saved in the specified or default format.");
                OutWriter.WriteLine();
                OutWriter.WriteLine("      The --where option is intended to extend or replace the earlier");
                OutWriter.WriteLine("      --test, --include and --exclude options by use of a selection expression");
                OutWriter.WriteLine("      describing exactly which tests to use. Examples of usage are:");
                OutWriter.WriteLine("          --where:cat==Data");
                OutWriter.WriteLine("          --where \"method =~ /DataTest*/ && cat = Slow\"");
                OutWriter.WriteLine();
                OutWriter.WriteLine("      Care should be taken in combining --where with --test or --testlist.");
                OutWriter.WriteLine("      The test and where specifications are implicitly joined using &&, so");
                OutWriter.WriteLine("      that BOTH sets of criteria must be satisfied in order for a test to run.");
                OutWriter.WriteLine("      See the docs for more information and a full description of the syntax");
                OutWriter.WriteLine("      information and a full description of the syntax.");
                OutWriter.WriteLine();
                OutWriter.WriteLine("      Several options that specify processing of XML output take");
                OutWriter.WriteLine("      an output specification as a value. A SPEC may take one of");
                OutWriter.WriteLine("      the following forms:");
                OutWriter.WriteLine("          --OPTION:filename");
                OutWriter.WriteLine("          --OPTION:filename;format=formatname");
                OutWriter.WriteLine("          --OPTION:filename;transform=xsltfile");
                OutWriter.WriteLine();
                OutWriter.WriteLine("      The --result option may use any of the following formats:");
                OutWriter.WriteLine("          nunit3 - the native XML format for NUnit 3");
                OutWriter.WriteLine("          nunit2 - legacy XML format used by earlier releases of NUnit");
                OutWriter.WriteLine("                   Requires the engine extension NUnitV2ResultWriter.");
                OutWriter.WriteLine();
                OutWriter.WriteLine("      The --explore option may use any of the following formats:");
                OutWriter.WriteLine("          nunit3 - the native XML format for NUnit 3");
                OutWriter.WriteLine("          cases  - a text file listing the full names of all test cases.");
                OutWriter.WriteLine("      If --explore is used without any specification following, a list of");
                OutWriter.WriteLine("      test cases is output to the writer.");
                OutWriter.WriteLine();
                OutWriter.WriteLine("      If none of the options {--result, --explore, --noresult} is used,");
                OutWriter.WriteLine("      NUnit saves the results to TestResult.xml in nunit3 format.");
                OutWriter.WriteLine();
                OutWriter.WriteLine("      Any transforms provided must handle input in the native nunit3 format.");
                OutWriter.WriteLine();
                OutWriter.WriteLine("      To be able to load NUnit projects, file type .nunit, the engine");
                OutWriter.WriteLine("      extension NUnitProjectLoader is required. For Visual Studio projects");
                OutWriter.WriteLine("      and solutions the engine extension VSProjectLoader is required.");
                OutWriter.WriteLine();
                OutWriter.WriteLine(ColorStyle.SectionHeader, "Limitations:");
                OutWriter.WriteLine("      The NetCore Runner is primarily intended for use as a dotnet  tool.");
                OutWriter.WriteLine("      When used in this way, a single assembly is usually being tested and");
                OutWriter.WriteLine("      the assembly must be compatible with execution under the same runtime");
                OutWriter.WriteLine("      as the runner itself, normally .NET 6.0.");
                OutWriter.WriteLine();
                OutWriter.WriteLine("      Using this runner, the following options are not available. A brief");
                OutWriter.WriteLine("      rationale is given for each option excluded.");
                OutWriter.WriteLine("        --configFile              Config of the runner itself is used.");
                OutWriter.WriteLine("        --process                 Not designed to run out of process.");
                OutWriter.WriteLine("        --inprocess               Redundant. We always run in process.");
                OutWriter.WriteLine("        --domain                  Not applicable to .NET Core.");
                OutWriter.WriteLine("        --framework               Runtime of the runner is used.");
                OutWriter.WriteLine("        --x86                     Bitness of the runner is used.");
                OutWriter.WriteLine("        --shadowcopy              Not available.");
                OutWriter.WriteLine("        --loaduserprofile         Not avalable.");
                OutWriter.WriteLine("        --agents                  No agents are used.");
                OutWriter.WriteLine("        --debug                   Debug in process directly.");
                OutWriter.WriteLine("        --pause                   Used for debugging agents.");
                OutWriter.WriteLine("        --set-principal-policy    Not available.");
                OutWriter.WriteLine("        --debug-agent             No agents are used.");
            }
        }

        private static void WriteErrorMessage(string msg)
        {
            OutWriter.WriteLine(ColorStyle.Error, msg);
        }

        private static void CancelHandler(object? sender, ConsoleCancelEventArgs args)
        {
            Console.ResetColor();
        }
    }
}
