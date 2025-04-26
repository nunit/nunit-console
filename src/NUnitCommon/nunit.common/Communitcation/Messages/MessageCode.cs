// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

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
    }
}