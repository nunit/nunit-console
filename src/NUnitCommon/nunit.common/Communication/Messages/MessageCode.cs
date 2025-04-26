// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Communication.Messages
{
    // All messages must be four characters in length
    public class MessageCode
    {
        public const string StartAgent = "STRT"; // not used
        public const string StopAgent = "EXIT";
        public const string CreateRunner = "RUNR";

        public const string LoadCommand = "LOAD";
        public const string ReloadCommand = "RELD";
        public const string UnloadCommand = "UNLD";
        public const string ExploreCommand = "XPLR";
        public const string CountCasesCommand = "CNTC";
        public const string RunCommand = "RSYN";
        public const string RunAsyncCommand = "RASY";
        public const string RequestStopCommand = "STOP";
        public const string ForcedStopCommand = "ABRT";

        public const string ProgressReport = "PROG";
        public const string CommandResult = "RSLT";

        public static string FromCommand(string command)
        {
            switch (command)
            {
                case "CreateRunner":
                    return CreateRunner;
                case "Load":
                    return LoadCommand;
                case "Reload":
                    return ReloadCommand;
                case "Unload":
                    return UnloadCommand;
                case "Explore":
                    return ExploreCommand;
                case "CountTestCases":
                    return CountCasesCommand;
                case "Run":
                    return RunCommand;
                case "RunAsync":
                    return RunAsyncCommand;
                case "Stop":
                    return RequestStopCommand;
                default:
                    throw new ArgumentException("Invalid command", nameof(command));
            }
        }
    }
}
