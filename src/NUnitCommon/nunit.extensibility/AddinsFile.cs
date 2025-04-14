// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.FileSystemAccess;

namespace NUnit.Extensibility
{
    public class AddinsFile : List<AddinsFileEntry>
    {
        public static AddinsFile Read(IFile file)
        {
            Guard.ArgumentNotNull(file);

            using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Read(stream, file.FullName);
            }
        }

        /// <summary>
        /// Reads the content of an addins-file from a stream.
        /// </summary>
        /// <param name="stream">Input stream. Must be readable and positioned at the beginning of the file.</param>
        /// <returns>All entries contained in the file.</returns>
        /// <exception cref="System.IO.IOException"><paramref name="stream"/> cannot be read</exception>
        /// <remarks>If the executing system uses backslashes ('\') to separate directories, these will be substituted with slashes ('/').</remarks>
        public static AddinsFile Read(Stream stream, string fullName = "addins file")
        {
            using (var reader = new StreamReader(stream))
            {
                var addinsFile = new AddinsFile();

                int lineNumber = 0;
                string? line;
                while ((line = reader.ReadLine()) is not null)
                {
                    var entry = new AddinsFileEntry(++lineNumber, line);
                    if (entry.Text != string.Empty && !entry.IsValid)
                    {
                        string msg = $"Invalid Entry in {fullName}:\r\n  {entry}";
                        throw new InvalidOperationException(msg);
                    }

                    addinsFile.Add(entry);
                }

                return addinsFile;
            }
        }

        private AddinsFile()
        {
        }

        public override string ToString()
        {
            var sb = new StringBuilder("AddinsFile:");
            foreach (var entry in this)
                sb.Append($"  {entry}");
            return sb.ToString();
        }

        public override bool Equals(object? obj)
        {
            var other = obj as AddinsFile;
            if (other is null)
                return false;

            if (Count != other.Count)
                return false;

            for (int i = 0; i < Count; i++)
                if (this[i] != other[i])
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
