// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NSubstitute;
using NUnit.Common;

namespace NUnit.ConsoleRunner.Tests
{
    internal static class ConsoleMocks
    {
        public static ConsoleOptions Options(params string[] args)
        {
            var mockFileSystem = Substitute.For<IFileSystem>();
            var mockDefaultsProvider = Substitute.For<IDefaultOptionsProvider>();
            return new ConsoleOptions(mockDefaultsProvider, mockFileSystem, args);
        }
    }
}
