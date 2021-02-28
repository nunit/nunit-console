// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using System.IO;

namespace NUnit.Common
{
    using System;

    internal class FileSystem : IFileSystem
    {
        public bool FileExists(string fileName)
        {
            if (fileName == null) throw new ArgumentNullException("fileName");

            return File.Exists(fileName);
        }

        public IEnumerable<string> ReadLines(string fileName)
        {
            if (fileName == null) throw new ArgumentNullException("fileName");

            using (var file = File.OpenText(fileName))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }
}