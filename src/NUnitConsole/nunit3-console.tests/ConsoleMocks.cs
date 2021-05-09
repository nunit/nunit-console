// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Text;
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

        public class ExtendedTextWriter  : NUnit.ConsoleRunner.ExtendedTextWriter
        {
            public override Encoding Encoding { get; }
            public override void Write(ColorStyle style, string value) { }

            public override void WriteLine(ColorStyle style, string value) { }

            public override void WriteLabel(string label, object option) { }

            public override void WriteLabel(string label, object option, ColorStyle valueStyle) { }

            public override void WriteLabelLine(string label, object option) { }

            public override void WriteLabelLine(string label, object option, ColorStyle valueStyle) { }
        }
    }
}
