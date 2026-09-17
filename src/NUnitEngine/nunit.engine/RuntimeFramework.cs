// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Common;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;

namespace NUnit.Engine
{
    /// <summary>
    /// RuntimeFramework represents a particular version
    /// of a common language runtime implementation.
    /// </summary>
    [Serializable]
    public sealed class RuntimeFramework : IRuntimeFramework
    {
        private Runtime _runtime;

        #region Construction

        /// <summary>
        /// Construct from a FrameworkName.
        /// </summary>
        /// <param name="frameworkName">A FrameworkName</param>
        public RuntimeFramework(FrameworkName frameworkName)
        {
            _runtime = Runtime.FromFrameworkIdentifier(frameworkName.Identifier);
            FrameworkName = frameworkName;
            DisplayName = $"{_runtime.DisplayName} {FrameworkVersion}";
            if (!string.IsNullOrEmpty(Profile) && Profile != "Full")
                DisplayName += " - " + Profile;
        }

        public RuntimeFramework(string frameworkName)
            : this(new FrameworkName(frameworkName))
        {
        }

        public RuntimeFramework(string identifier, Version version, string? profile = null)
            : this(new FrameworkName(identifier, version, profile))
        {
        }

        public static RuntimeFramework FromTFM(string tfm)
        {
            Guard.ArgumentNotNullOrEmpty(tfm, nameof(tfm));

            int digit = tfm.IndexOfAny(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9']);
            Guard.ArgumentValid(digit >= 0, $"Invalid or unsupported TFM: {tfm}", nameof(tfm));

            string prefix = tfm.Substring(0, digit);
            string vpart = tfm.Substring(digit);

            switch (prefix)
            {
                case "net":
                    if (vpart.Contains('.'))
                        return new RuntimeFramework(FrameworkIdentifiers.NetCoreApp, new Version(vpart));
                    else
                    {
                        if (vpart.Length == 2)
                            vpart = vpart.Insert(1, ".");
                        else if (vpart.Length == 3)
                            vpart = vpart.Insert(1, ".").Insert(3, ".");
                        else
                            throw new ArgumentException($"Invalid or unsupported TFM: {tfm}", nameof(tfm));

                        return new RuntimeFramework(FrameworkIdentifiers.NetFramework, new Version(vpart));
                    }
                case "netcoreapp":
                    return new RuntimeFramework(FrameworkIdentifiers.NetCoreApp, new Version(vpart));
                default:
                    throw new ArgumentException($"Invalid or unsupported TFM: {tfm}");
            }
        }

        #endregion

        #region IRuntimeFramework Implementation

        private static readonly char[] RuntimeFrameworkSeparator = ['-'];

        /// <summary>
        /// Gets the unique Id for this runtime, such as "net-4.6.2"
        /// </summary>
        public string Id => _runtime.ToString().ToLower() + "-" + FrameworkVersion.ToString();

        /// <summary>
        /// Returns the Display name for this framework
        /// </summary>
        // TODO: Determine if we can remove this property.
        public string DisplayName { get; }

        /// <summary>
        /// The framework version for this runtime framework
        /// </summary>
        public Version FrameworkVersion => FrameworkName.Version;

        /// <summary>
        /// The Profile for this framework, where relevant.
        /// May be the empty string and will have different
        /// sets of values for each Runtime.
        /// </summary>
        public string Profile => FrameworkName.Profile;

        #endregion

        #region Implementation of new IRuntimeFramework interface

        /// <summary>
        /// Gets the Target Framework Moniker (TFM) for this runtime, such as "net462"
        /// </summary>
        public string TFM => _runtime.GetTFM(FrameworkVersion);

        /// <summary>
        /// Gets the FrameworkName for this runtime, such as ".NETFramework,Version=v4.6.2"
        /// </summary>
        public FrameworkName FrameworkName { get; }

        #endregion

        /// <summary>
        /// Returns true if the current framework matches the
        /// one supplied as an argument. Both the RuntimeType
        /// and the version must match.
        ///
        /// Two RuntimeTypes match if they are equal, if either one
        /// is RuntimeType.Any or if one is RuntimeType.Net and
        /// the other is RuntimeType.Mono.
        /// </summary>
        /// <param name="target">The RuntimeFramework to be matched.</param>
        /// <returns><c>true</c> on match, otherwise <c>false</c></returns>
        public bool Supports(RuntimeFramework target)
        {
            if (!_runtime.Matches(target._runtime))
                return false;

            return _runtime.Supports(this.FrameworkVersion, target.FrameworkVersion);
        }

        public bool CanLoad(IRuntimeFramework requested)
        {
            return FrameworkVersion >= requested.FrameworkName.Version;
        }
    }
}
