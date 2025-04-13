// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NUnit
{
    /// <summary>
    /// Static methods for manipulating project paths, including both directories
    /// and files. Some synonyms for System.Path methods are included as well.
    /// </summary> 
    public class PathUtils
    {
        public const uint FILE_ATTRIBUTE_DIRECTORY  = 0x00000010;  
        public const uint FILE_ATTRIBUTE_NORMAL     = 0x00000080;  
        public const int MAX_PATH = 256;

        protected static char DirectorySeparatorChar = Path.DirectorySeparatorChar;
        protected static char AltDirectorySeparatorChar = Path.AltDirectorySeparatorChar;

        private static readonly char[] DirectorySeparators = [DirectorySeparatorChar, AltDirectorySeparatorChar];

        /// <summary>
        /// Returns a boolean indicating whether the specified path
        /// is that of an assembly - that is a dll or exe file.
        /// </summary>
        /// <param name="path">Path to a file.</param>
        /// <returns>True if the file extension is dll or exe, otherwise false.</returns>
        public static bool IsAssemblyFileType(string path)
        {
            string extension = Path.GetExtension(path).ToLower();
            return extension == ".dll" || extension == ".exe";
        }

        /// <summary>
        /// Returns the relative path from a base directory to another
        /// directory or file.
        /// </summary>
        public static string? RelativePath( string from, string to )
        {
            if (from == null)
                throw new ArgumentNullException (from);
            if (to == null)
                throw new ArgumentNullException (to);

            string? toPathRoot = Path.GetPathRoot(to);
            if (toPathRoot == null || toPathRoot == string.Empty)
                return to;
            string? fromPathRoot = Path.GetPathRoot(from);
            if (fromPathRoot == null || fromPathRoot == string.Empty)
                return null;

            if (!PathsEqual(toPathRoot, fromPathRoot))
                return null;

            string fromNoRoot = from.Substring(fromPathRoot.Length);
            string toNoRoot = to.Substring(toPathRoot.Length);

            string[] _from = SplitPath(fromNoRoot);
            string[] _to = SplitPath(toNoRoot);

            StringBuilder sb = new StringBuilder (Math.Max (from.Length, to.Length));

            int last_common, min = Math.Min (_from.Length, _to.Length);
            for (last_common = 0; last_common < min;  ++last_common) 
            {
                if (!PathsEqual(_from[last_common], _to[last_common]))
                    break;
            }

            if (last_common < _from.Length)
                sb.Append ("..");
            for (int i = last_common + 1; i < _from.Length; ++i) 
            {
                sb.Append (PathUtils.DirectorySeparatorChar).Append ("..");
            }

            if (sb.Length > 0)
                sb.Append (PathUtils.DirectorySeparatorChar);
            if (last_common < _to.Length)
                sb.Append (_to [last_common]);
            for (int i = last_common + 1; i < _to.Length; ++i) 
            {
                sb.Append (PathUtils.DirectorySeparatorChar).Append (_to [i]);
            }

            return sb.ToString ();
        }

        /// <summary>
        /// Return the canonical form of a path.
        /// </summary>
        public static string Canonicalize( string path )
        {
            List<string> parts = new List<string>(
                path.Split( DirectorySeparators ) );

            for( int index = 0; index < parts.Count; )
            {
                string part = parts[index];
        
                switch( part )
                {
                    case ".":
                        parts.RemoveAt( index );
                        break;
                
                    case "..":
                        parts.RemoveAt( index );
                        if ( index > 0 )
                            parts.RemoveAt( --index );
                        break;
                    default:
                        index++;
                        break;
                }
            }

            // Trailing separator removal
            if (parts.Count > 1 && path.Length > 1 && parts[parts.Count - 1] == "")
                parts.RemoveAt(parts.Count - 1);

            return String.Join(DirectorySeparatorChar.ToString(), parts.ToArray());
        }

        /// <summary>
        /// True if the two paths are the same or if the second is
        /// directly or indirectly under the first. Note that paths 
        /// using different network shares or drive letters are 
        /// considered unrelated, even if they end up referencing
        /// the same subtrees in the file system.
        /// </summary>
        public static bool SamePathOrUnder( string path1, string path2 )
        {
            path1 = Canonicalize( path1 );
            path2 = Canonicalize( path2 );

            int length1 = path1.Length;
            int length2 = path2.Length;

            // if path1 is longer, then path2 can't be under it
            if ( length1 > length2 )
                return false;

            // if lengths are the same, check for equality
            if ( length1 == length2 )
                return string.Compare( path1, path2, RunningOnWindows ) == 0;

            // path 2 is longer than path 1: see if initial parts match
            if ( string.Compare( path1, path2.Substring( 0, length1 ), RunningOnWindows ) != 0 )
                return false;
            
            // must match through or up to a directory separator boundary
            return	path2[length1-1] == DirectorySeparatorChar ||
                path2[length1] == DirectorySeparatorChar;
        }

        /// <summary>
        /// Combines all the arguments into a single path
        /// </summary>
        public static string Combine(string path1, params string[] morePaths)
        {
            string result = path1;
            foreach (string path in morePaths)
                result = Path.Combine(result, path);
            return result;
        }

        /// <summary>
        /// Returns a value that indicates whether the specified file path is fully qualified.
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns><see langword="true"/> if <paramref name="path"/> is an absolute path; otherwhise, false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/></exception>
        public static bool IsFullyQualifiedPath(string path)
        {
            return RunningOnWindows
                ? IsFullyQualifiedWindowsPath(path)
                : IsFullyQualifiedUnixPath(path);
        }

        /// <summary>
        /// Returns a value that indicates whether the specified file path is absolute or not on Windows operating systems.
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns><see langword="true"/> if <paramref name="path"/> is an absolute or UNC path; otherwhise, false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        public static bool IsFullyQualifiedWindowsPath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.Length > 2)
            {
                return (IsValidDriveSpecifier(path[0]) && path[1] == ':' && IsWindowsDirectorySeparator(path[2]))
                    || (IsWindowsDirectorySeparator(path[0]) && IsWindowsDirectorySeparator(path[1]));
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a value that indicates whether the specified file path is absolute or not on Linux/macOS/Unix.
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns><see langword="true"/> if <paramref name="path"/> is an absolute path; otherwhise, false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/></exception>
        public static bool IsFullyQualifiedUnixPath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return path.Length > 0 && path[0] == '/';
        }

        public static bool IsValidPath(string path)
        {
            try
            {
                var info = GetFileSystemInfo(path);
                var creation = info.CreationTime;
                return true; // Whether it exists or not!
            }
            catch
            {
                return false;
            }
        }

        private static FileSystemInfo GetFileSystemInfo(string path) =>
            path.EndsWith("/") || path.EndsWith(@"\\")
                ? new DirectoryInfo(path) as FileSystemInfo
                : new FileInfo(path) as FileSystemInfo;

        private static bool IsWindowsDirectorySeparator(char c)
        {
            return c == '\\' || c == '/';
        }

        private static bool IsValidDriveSpecifier(char c)
        {
            return ('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z');
        }

        private static bool RunningOnWindows => DirectorySeparatorChar == '\\';

        private static string[] SplitPath(string path)
        {
            string[] trialSplit = path.Split(DirectorySeparators);

            int emptyEntries = 0;
            foreach (string piece in trialSplit)
                if (piece == string.Empty)
                    emptyEntries++;

            if (emptyEntries == 0)
                return trialSplit;

            string[] finalSplit = new string[trialSplit.Length - emptyEntries];
            int index = 0;
            foreach (string piece in trialSplit)
                if (piece != string.Empty)
                    finalSplit[index++] = piece;

            return finalSplit;
        }

        private static bool PathsEqual(string path1, string path2)
        {
            if (PathUtils.RunningOnWindows)
                return path1.ToLower().Equals(path2.ToLower());
            else
                return path1.Equals(path2);
        }
    }
}
