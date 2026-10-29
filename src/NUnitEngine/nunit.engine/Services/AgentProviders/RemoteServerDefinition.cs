// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Describes an explicitly selected, already-running provider server.
    /// This candidate uses trusted loopback connections, including an externally established tunnel.
    /// </summary>
    /// <remarks>
    /// The XML schema is provisional. This class does not discover servers, open tunnels,
    /// transfer packages or grant trust to non-loopback endpoints. Relative package paths
    /// and authentication over a direct network connection are outside this initial slice.
    /// </remarks>
    internal sealed class RemoteServerDefinition
    {
        private RemoteServerDefinition(string name, IPEndPoint endpoint, int connectTimeout, int operationTimeout, int runReadTimeout)
        {
            Name = name;
            Endpoint = endpoint;
            ConnectTimeout = connectTimeout;
            OperationTimeout = operationTimeout;
            RunReadTimeout = runReadTimeout;
        }

        internal string Name { get; }
        internal IPEndPoint Endpoint { get; }
        internal int ConnectTimeout { get; }
        internal int OperationTimeout { get; }
        internal int RunReadTimeout { get; }

        /// <summary>Compares immutable connection settings without retargeting existing agent references.</summary>
        internal bool Matches(RemoteServerDefinition other) =>
            Name == other.Name && Endpoint.Equals(other.Endpoint) && ConnectTimeout == other.ConnectTimeout
                && OperationTimeout == other.OperationTimeout && RunReadTimeout == other.RunReadTimeout;

        /// <summary>
        /// Reads a bounded definition with DTD processing disabled. Only a literal loopback
        /// TCP address is accepted; DNS resolution cannot redirect the connection elsewhere.
        /// </summary>
        internal static RemoteServerDefinition Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
                throw new ArgumentException("RemoteServerDefinition requires an absolute file path.", nameof(path));

            using var input = File.OpenRead(path);
            if (input.Length > 16384)
                throw new InvalidDataException("The server definition exceeds its size limit.");
            using var reader = XmlReader.Create(input, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 16384
            });
            XElement root = XElement.Load(reader);
            string[] attributes = { "name", "endpoint", "connectTimeout", "operationTimeout", "runReadTimeout" };
            if (root.Name != "nunit-remote-server" || root.HasElements || !string.IsNullOrWhiteSpace(root.Value)
                || root.Attributes().Any(attribute => !attributes.Contains(attribute.Name.ToString())))
                throw new InvalidDataException("Unsupported server definition content.");

            string? name = (string?)root.Attribute("name");
            if (string.IsNullOrWhiteSpace(name) || name.Length > 80
                || name.Any(character => !char.IsLetterOrDigit(character) && character != '-' && character != '_' && character != '.'))
                throw new InvalidDataException("A server name must contain only letters, digits, hyphens, underscores or periods.");
            string? location = (string?)root.Attribute("endpoint");
            if (!Uri.TryCreate(location, UriKind.Absolute, out var uri) || uri.Scheme != "tcp"
                || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)
                || (uri.AbsolutePath.Length != 0 && uri.AbsolutePath != "/") || uri.Port < 1 || uri.Port > 65535
                || !IPAddress.TryParse(uri.DnsSafeHost.Trim('[', ']'), out var address) || !IPAddress.IsLoopback(address))
                throw new InvalidDataException("This candidate requires a literal loopback TCP endpoint with an explicit port.");

            return new RemoteServerDefinition(name!, new IPEndPoint(address, uri.Port),
                ReadTimeout(root, "connectTimeout", 10000, false),
                ReadTimeout(root, "operationTimeout", 30000, false),
                ReadTimeout(root, "runReadTimeout", 120000, true));
        }

        /// <summary>
        /// Reads millisecond socket deadlines. Zero disables the run read timeout only;
        /// it does not change framework test timeouts or cancellation semantics.
        /// </summary>
        private static int ReadTimeout(XElement root, string name, int fallback, bool allowZero)
        {
            string? text = (string?)root.Attribute(name);
            if (text is null)
                return fallback;
            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value)
                || value < (allowZero ? 0 : 1))
                throw new InvalidDataException("Invalid millisecond timeout: " + name);
            return value;
        }
    }
}
