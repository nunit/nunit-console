// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;

namespace NUnit.Common
{
    public interface IFileSystem
    {
        bool FileExists(string fileName);

        IEnumerable<string> ReadLines(string fileName);
    }
}