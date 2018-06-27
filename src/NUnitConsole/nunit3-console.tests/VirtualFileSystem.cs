// ***********************************************************************
// Copyright (c) 2011 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

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
