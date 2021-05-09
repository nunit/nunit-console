// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NET20 || NET35
using System;
using System.Diagnostics;
using NUnit.Common;

// NOTE: Since the .NET 4.5 engine refers to this assembly, we can't define
// FrameworkName in the System.Runtime.Versioning namespace.
namespace NUnit.Engine.Compatibility
{
    /// <summary>
    /// Compatible implementation of FrameworkName, based on the corefx implementation
    /// </summary>
    public sealed class FrameworkName : IEquatable<FrameworkName>
    {
        private const string FRAMEWORK_NAME_INVALID = "Invalid FrameworkName";
        private const string FRAMEWORK_NAME_VERSION_REQUIRED = "FramweworkName must include Version";
        private const string FRAMEWORK_NAME_VERSION_INVALID = "The specified Version is invalid";
        private const string FRAMEWORK_NAME_COMPONENT_COUNT = "FrameworkName must specify either two or three components";

        private readonly string _identifier;
        private readonly Version _version = null;
        private readonly string _profile;
        private string _fullName;

        private const char COMPONENT_SEPARATOR = ',';
        private const char KEY_VALUE_SEPARATOR = '=';
        private const char VERSION_PREFIX = 'v';
        private const string VERSION_KEY = "Version";
        private const string PROFILE_KEY = "Profile";

        private static readonly char[] COMPONENT_SPLIT_SEPARATOR = { COMPONENT_SEPARATOR };

        public string Identifier
        {
            get
            {
                Debug.Assert(_identifier != null);
                return _identifier;
            }
        }

        public Version Version
        {
            get
            {
                Debug.Assert(_version != null);
                return _version;
            }
        }

        public string Profile
        {
            get
            {
                Debug.Assert(_profile != null);
                return _profile;
            }
        }

        public string FullName
        {
            get
            {
                if (_fullName == null)
                {
                    if (string.IsNullOrEmpty(Profile))
                    {
                        _fullName =
                            Identifier +
                            COMPONENT_SEPARATOR + VERSION_KEY + KEY_VALUE_SEPARATOR + VERSION_PREFIX +
                            Version.ToString();
                    }
                    else
                    {
                        _fullName =
                            Identifier +
                            COMPONENT_SEPARATOR + VERSION_KEY + KEY_VALUE_SEPARATOR + VERSION_PREFIX +
                            Version.ToString() +
                            COMPONENT_SEPARATOR + PROFILE_KEY + KEY_VALUE_SEPARATOR +
                            Profile;
                    }
                }

                Debug.Assert(_fullName != null);
                return _fullName;
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FrameworkName);
        }

        public bool Equals(FrameworkName other)
        {
            if (other is null)
            {
                return false;
            }

            return Identifier == other.Identifier &&
                Version == other.Version &&
                Profile == other.Profile;
        }

        public override int GetHashCode()
        {
            return Identifier.GetHashCode() ^ Version.GetHashCode() ^ Profile.GetHashCode();
        }

        public override string ToString()
        {
            return FullName;
        }

        public FrameworkName(string identifier, Version version)
            : this(identifier, version, null)
        {
        }

        public FrameworkName(string identifier, Version version, string profile=null)
        {
            Guard.ArgumentNotNull(identifier, nameof(identifier));
            Guard.ArgumentNotNull(version, nameof(version));

            identifier = identifier.Trim();
            Guard.ArgumentNotNullOrEmpty(identifier, nameof(identifier));

            _identifier = identifier;
            _version = version;
            _profile = (profile == null) ? string.Empty : profile.Trim();
        }

        // Parses strings in the following format: "<identifier>, Version=[v|V]<version>, Profile=<profile>"
        //  - The identifier and version is required, profile is optional
        //  - Only three components are allowed.
        //  - The version string must be in the System.Version format; an optional "v" or "V" prefix is allowed
        public FrameworkName(string frameworkName)
        {
            Guard.ArgumentNotNullOrEmpty(frameworkName, nameof(frameworkName));

            string[] components = frameworkName.Split(COMPONENT_SPLIT_SEPARATOR);

            // Identifier and Version are required, Profile is optional.
            Guard.ArgumentValid(components.Length == 2 || components.Length == 3, 
                FRAMEWORK_NAME_COMPONENT_COUNT, nameof(frameworkName));

            //
            // 1) Parse the "Identifier", which must come first. Trim any whitespace
            //
            _identifier = components[0].Trim();

            Guard.ArgumentValid(_identifier.Length > 0, FRAMEWORK_NAME_INVALID, nameof(frameworkName));

            bool versionFound = false;
            _profile = string.Empty;

            //
            // The required "Version" and optional "Profile" component can be in any order
            //
            for (int i = 1; i < components.Length; i++)
            {
                // Get the key/value pair separated by '='
                string component = components[i];
                int separatorIndex = component.IndexOf(KEY_VALUE_SEPARATOR);

                Guard.ArgumentValid(separatorIndex >= 0 && separatorIndex == component.LastIndexOf(KEY_VALUE_SEPARATOR),
                    FRAMEWORK_NAME_INVALID, nameof(frameworkName));

                // Get the key and value, trimming any whitespace
                string key = component.Substring(0, separatorIndex).Trim();
                string value = component.Substring(separatorIndex + 1).Trim();

                //
                // 2) Parse the required "Version" key value
                //
                if (key.Equals(VERSION_KEY, StringComparison.OrdinalIgnoreCase))
                {
                    versionFound = true;

                    // Allow the version to include a 'v' or 'V' prefix...
                    if (value.Length > 0 && (value[0] == VERSION_PREFIX || value[0] == 'V'))
                        value = value.Substring(1);

                    try
                    {
                        _version = new Version(value);
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException(FRAMEWORK_NAME_VERSION_INVALID, nameof(frameworkName), e);
                    }
                }
                //
                // 3) Parse the optional "Profile" key value
                //
                else if (key.Equals(PROFILE_KEY, StringComparison.OrdinalIgnoreCase))
                {
                    if (value.Length > 0)
                    {
                        _profile = value.ToString();
                    }
                }
                else
                {
                    throw new ArgumentException(FRAMEWORK_NAME_INVALID, nameof(frameworkName));
                }
            }

            if (!versionFound)
                throw new ArgumentException(FRAMEWORK_NAME_VERSION_REQUIRED, nameof(frameworkName));
        }

        public static bool operator ==(FrameworkName left, FrameworkName right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(FrameworkName left, FrameworkName right)
        {
            return !(left == right);
        }
    }
}
#endif
