// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using System.IO;
using System;

namespace NUnit.ConsoleRunner
{
    internal class FileSystem : IFileSystem
    {
        public bool FileExists(string fileName)
        {
            if (fileName is null)
                throw new ArgumentNullException(nameof(fileName));

            return File.Exists(fileName);
        }

        public IEnumerable<string> ReadLines(string fileName)
        {
            if (fileName is null)
                throw new ArgumentNullException(nameof(fileName));

            using (var file = File.OpenText(fileName))
            {
                string? line;
                while ((line = file.ReadLine()) is not null)
                {
                    yield return line;
                }
            }
        }
    }
}