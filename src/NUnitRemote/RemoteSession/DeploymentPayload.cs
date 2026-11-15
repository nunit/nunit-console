// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NUnit.Remote.Deployment
{
    /// <summary>
    /// Adapts the prototype's before/copy/after digest verification and deterministic
    /// relative-name manifest. No execution command or provider wire message is added.
    /// </summary>
    internal sealed class DeploymentPayload
    {
        private const long MaximumBytes = 512L * 1024 * 1024;
        private const int MaximumFiles = 8192;
        private static readonly string[] PrivateExtensions = { ".snk", ".pfx", ".key", ".private", ".clixml" };
        private readonly string _root;
        private readonly List<DeploymentFile> _files = new List<DeploymentFile>();
        private readonly HashSet<string> _names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private long _bytes;

        internal DeploymentPayload(string root) => _root = root;

        /// <summary>Gets an immutable view of the source-relative payload inventory.</summary>
        internal IReadOnlyList<DeploymentFile> Files => _files.AsReadOnly();

        /// <summary>Uses the prototype's ordered name:digest manifest format.</summary>
        internal string Digest => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            string.Join("\n", _files.OrderBy(file => file.Name, StringComparer.Ordinal)
                .Select(file => file.Name + ":" + file.Sha256))))).ToLowerInvariant();

        /// <summary>Copies an ordinary tree, excluding only explicitly regenerated addins definitions.</summary>
        internal void CopyTree(string source, string component, bool regenerateAddins = false)
        {
            source = DeploymentPaths.Root(source);
            if (DeploymentPaths.Contains(source, _root) || DeploymentPaths.Contains(_root, source))
                throw new IOException("Payload sources and the deployment must be separate.");
            foreach (string file in DeploymentPaths.Files(source))
            {
                string extension = Path.GetExtension(file);
                if (regenerateAddins && extension.Equals(".addins", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (PrivateExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    throw new IOException("Private key or settings exports are not deployment assets.");
                string name = component + "/" + Path.GetRelativePath(source, file).Replace('\\', '/');
                long length = new FileInfo(file).Length;
                string target = Reserve(name, length);
                string digest = Hash(file);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, false);
                if (new FileInfo(target).Length != length || Hash(target) != digest || Hash(file) != digest)
                    throw new IOException("The copied payload or source changed during staging: " + name);
                _files.Add(new DeploymentFile(name, length, digest));
            }
        }

        /// <summary>Writes a generated portable configuration as an ordinary hashed payload asset.</summary>
        internal void Write(string name, string content)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            string path = Reserve(name, bytes.LongLength);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                file.Write(bytes);
            _files.Add(new DeploymentFile(name, bytes.LongLength, Hash(path)));
        }

        /// <summary>Rejects missing, changed or additional files before launching any process.</summary>
        internal void Verify()
        {
            string[] actual = DeploymentPaths.Files(_root)
                .Select(file => Path.GetRelativePath(_root, file).Replace('\\', '/')).ToArray();
            if (actual.Length != _files.Count || actual.Any(name => !_names.Contains(name)))
                throw new IOException("The payload inventory changed after staging.");
            foreach (var file in _files)
            {
                string path = DeploymentPaths.Child(_root, file.Name);
                if (!File.Exists(path) || new FileInfo(path).Length != file.Bytes || Hash(path) != file.Sha256)
                    throw new IOException("The staged payload no longer matches its manifest: " + file.Name);
            }
        }

        private string Reserve(string name, long bytes)
        {
            string target = DeploymentPaths.Child(_root, name);
            if (_files.Count >= MaximumFiles || bytes < 0 || bytes > MaximumBytes - _bytes)
                throw new IOException("The session payload exceeds its size limit.");
            if (!_names.Add(name) || File.Exists(target) || Directory.Exists(target))
                throw new IOException("Duplicate or conflicting payload path: " + name);
            _bytes += bytes;
            return target;
        }

        private static string Hash(string path)
        {
            using var file = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant();
        }
    }
}
