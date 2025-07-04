﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using System.IO;

namespace NUnit.ConsoleRunner
{
    internal class FileSystem : IFileSystem
    {
        public bool FileExists(string fileName)
        {
            Guard.ArgumentNotNull(fileName);

            return File.Exists(fileName);
        }

        public IEnumerable<string> ReadLines(string fileName)
        {
            Guard.ArgumentNotNull(fileName);

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