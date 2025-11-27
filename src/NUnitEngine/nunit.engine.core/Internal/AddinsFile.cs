// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Engine.Internal.FileSystemAccess;

namespace NUnit.Engine.Internal
{
    internal class AddinsFile : List<AddinsFileEntry>
    {
        static readonly Logger log = InternalTrace.GetLogger(typeof(AddinsFile));

        public static AddinsFile Read(IFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

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
        internal static AddinsFile Read(Stream stream, string fullName = null)
        {
            // Read the whole file first
            var content = new List<string>();
            using (var reader = new StreamReader(stream))
            { 
                while (!reader.EndOfStream)
                    content.Add(reader.ReadLine().Trim());
            }

            // Create an empty AddinsFile, with no entries
            var addinsFile = new AddinsFile();

            // Ensure that this is actually an NUnit .addins file, since
            // the extension is used by others. See, for example,
            // https://github.com/nunit/nunit-console/issues/1761
            // TODO: Consider using an extension specific to NUnit for V4
            if (!IsNUnitAddinsFile(content))
            {
                log.Warning($"Ignoring file {fullName} because it's not an NUnit .addins file");
                return addinsFile;
            }

            // It's our file, so process it
            int lineNumber = 0;
            foreach (var line in content)
            {
                var entry = new AddinsFileEntry(++lineNumber, line);
                if (entry.Text != "" && !entry.IsValid)
                {
                    string msg = $"Invalid Entry in {fullName ?? "addins file"}:\r\n  {entry}";
                    throw new InvalidOperationException(msg);
                }

                addinsFile.Add(entry);
            }

            return addinsFile;
        }

        private AddinsFile() { }

        private static bool IsNUnitAddinsFile(List<string> content)
        {
            foreach (var line in content)
                if (line.Length > 0 && line[0] == '<')
                    return false;

            return true;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("AddinsFile:");
            foreach (var entry in this)
                sb.Append($"  {entry}");
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            var other = obj as AddinsFile;
            if (other == null) return false;

            if (Count != other.Count) return false;

            for (int i = 0; i < Count; i++)
                if (this[i] != other[i]) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
