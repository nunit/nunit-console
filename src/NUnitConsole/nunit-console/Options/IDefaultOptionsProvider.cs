// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.ConsoleRunner.Options
{
    internal interface IDefaultOptionsProvider
    {
        bool TeamCity { get; }
    }
}