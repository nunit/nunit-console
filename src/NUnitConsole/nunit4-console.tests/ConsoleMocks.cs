// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NSubstitute;

namespace NUnit.ConsoleRunner.Options
{
    internal static class ConsoleMocks
    {
        public static ConsoleOptions Options(params string[] args)
        {
            var mockFileSystem = Substitute.For<IFileSystem>();
            return new ConsoleOptions(mockFileSystem, args);
        }
    }
}
