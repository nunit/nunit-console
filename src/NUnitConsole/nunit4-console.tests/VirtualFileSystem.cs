// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;

namespace NUnit.ConsoleRunner
{
    internal class VirtualFileSystem : IFileSystem
    {
        private readonly Dictionary<string, IEnumerable<string>> files = new Dictionary<string, IEnumerable<string>>();

        public bool FileExists(string fileName)
        {
            return files.ContainsKey(fileName);
        }

        public IEnumerable<string> ReadLines(string fileName)
        {
            IEnumerable<string>? lines;
            if (!files.TryGetValue(fileName, out lines))
            {
                throw new FileNotFoundException("File not found", fileName);
            }

            return lines;
        }

        internal void SetupFile(string path, IEnumerable<string> lines)
        {
            files[path] = new List<string>(lines);
        }

        private static readonly char[] CommaSeparator = [','];
        private static readonly char[] ColonSeparator = [':'];
        private static readonly char[] NewLineSeparator = ['\n'];

        internal void SetupFiles(string files)
        {
            foreach (var file in files.Split(CommaSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var fileParts = file.Split(ColonSeparator);
                var lines = fileParts[1].Replace("\r\n", "\n").Split(NewLineSeparator, StringSplitOptions.RemoveEmptyEntries);
                SetupFile(fileParts[0], lines);
            }
        }
    }
}
