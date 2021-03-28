// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine.Internal.FileSystemAccess;
using System;
using System.Collections.Generic;
using System.IO;

namespace NUnit.Engine.Internal
{
    /// <summary>
     /// A reader for NUnit addins-files.
     /// </summary>
     /// <remarks>
     /// The format of an addins-file can be found at https://docs.nunit.org/articles/nunit-engine/extensions/Installing-Extensions.html.
     /// </remarks>
    internal sealed class AddinsFileReader : IAddinsFileReader
    {
        /// <inheritdoc/>
        public IEnumerable<string> Read(IFile file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            using (var reader = new FileStream(file.FullName, FileMode.Open,  FileAccess.Read, FileShare.Read))
            {
                return this.Read(reader);
            }
        }

        /// <summary>
        /// Reads the content of an addins-file from a stream.
        /// </summary>
        /// <param name="stream">Input stream. Must be readable and positioned at the beginning of the file.</param>
        /// <returns>All entries contained in the file.</returns>
        /// <exception cref="System.IO.IOException"><paramref name="stream"/> cannot be read</exception>
        /// <remarks>If the executing system uses backslashes ('\') to separate directories, these will be substituted with slashes ('/').</remarks>
        internal IEnumerable<string> Read(Stream stream)
        {
            var result = new List<string>();
            using (var reader = new StreamReader(stream))
            {
                for(var line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    line = line.Split(new char[] { '#' })[0].Trim();
                    if (line != string.Empty)
                    {
                        result.Add(line.Replace(Path.DirectorySeparatorChar, '/'));
                    }
                }
            }

            return result;
        }
    }
}
