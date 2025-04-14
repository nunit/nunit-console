// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace NUnit.Extensibility
{
    // Class representing a single line in an Addins file
    public class AddinsFileEntry
    {
        public int LineNumber { get; }
        public string RawText { get; }
        public string Text { get; }

        public bool IsFullyQualified => PathUtils.IsFullyQualifiedPath(Text);
        public bool IsDirectory => Text.EndsWith("/");
        public bool IsPattern => Text.Contains("*");
        public bool IsValid => PathUtils.IsValidPath(Text.Replace('*', 'X'));

        public string DirectoryName => Path.GetDirectoryName(Text)!;
        public string FileName => Path.GetFileName(Text);

        public AddinsFileEntry(int lineNumber, string rawText)
        {
            LineNumber = lineNumber;
            RawText = rawText;
            Text = rawText.Split(new char[] { '#' })[0].Trim()
                .Replace(Path.DirectorySeparatorChar, '/');
        }

        public override string ToString()
        {
            return $"{LineNumber}: {RawText}";
        }

        public override bool Equals(object? obj)
        {
            var other = obj as AddinsFileEntry;
            if (other is null)
                return false;

            return LineNumber == other.LineNumber && RawText == other.RawText;
        }

        public override int GetHashCode()
        {
            return LineNumber.GetHashCode() ^ RawText.GetHashCode();
        }
    }
}
