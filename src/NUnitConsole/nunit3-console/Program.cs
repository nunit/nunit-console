// ***********************************************************************
// Copyright (c) 2011 Charlie Poole, Rob Prouse
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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Common;
using NUnit.Engine;

#if !NET20
using System.Linq;
#endif

using NUnit.Options;

namespace NUnit.ConsoleRunner
{
    /// <summary>
    /// This class provides the entry point for the console runner.
    /// </summary>
    public class Program
    {
        //static Logger log = InternalTrace.GetLogger(typeof(Runner));
        static ConsoleOptions Options = new ConsoleOptions(new DefaultOptionsProvider(), new FileSystem());
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

                    if (Options.InternalTraceLevel != null)
                        engine.InternalTraceLevel = (InternalTraceLevel)Enum.Parse(typeof(InternalTraceLevel), Options.InternalTraceLevel);

                    try
                    {
                        return new ConsoleRunner(engine, Options, OutWriter).Execute();
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

            var header = $"{versionBlock.ProductName} {versionBlock.ProductVersion}";

#if NET20
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
            OutWriter.WriteLine(ColorStyle.Default, "Runs a set of NUnit tests from the console.");
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
                //writer.WriteLine("Options that take values may use an equal sign, a colon");
                //writer.WriteLine("or a space to separate the option from its value.");
                //writer.WriteLine();
            }
        }

        private static void CancelHandler(object sender, ConsoleCancelEventArgs args)
        {
            Console.ResetColor();
        }
    }
}
