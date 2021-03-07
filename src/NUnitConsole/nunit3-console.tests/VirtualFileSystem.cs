// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using NUnit.Common;

namespace NUnit.ConsoleRunner.Tests
{
    using System.IO;

    internal class VirtualFileSystem: IFileSystem
    {
        private readonly Dictionary<string, IEnumerable<string>> files = new Dictionary<string, IEnumerable<string>>();

        public bool FileExists(string fileName)
        {
            return files.ContainsKey(fileName);
        }

        public IEnumerable<string> ReadLines(string fileName)
        {
            IEnumerable<string> lines;
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

        internal void SetupFiles(string files)
        {
            foreach (var file in files.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var fileParts = file.Split(':');
                var lines = fileParts[1].Replace("\r\n", "\n").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                SetupFile(fileParts[0], lines);
            }
        }
    }
}
