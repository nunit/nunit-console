// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine
{
    /// <summary>
    /// Runtime class represents a specific Runtime, which may be
    /// available in one or more versions. To define new Runtimes,
    /// add a new member to the RuntimeType enum and then update
    /// the SetProperties method in this class.
    /// </summary>
    public abstract class Runtime
    {
        #region Static Properties and Methods

        // NOTE: The following are the only instances, which should
        // ever exist, since the nested classes are private.

        /// <summary>Microsoft .NET Framework</summary>
        public static Runtime Net { get; } = new NetFrameworkRuntime();

        /// <summary>Mono</summary>
        public static Runtime Mono { get; } = new MonoRuntime();

        /// <summary>NetCore</summary>
        public static Runtime NetCore { get; } = new NetCoreRuntime();

        public static Runtime Parse(string s)
        {
            switch (s.ToLower())
            {
                case "net":
                    return Runtime.Net;
                case "mono":
                    return Runtime.Mono;
                case "netcore":
                    return Runtime.NetCore;
                default:
                    throw new NUnitEngineException($"Invalid runtime specified: {s}");
            }
        }

        public static Runtime FromFrameworkIdentifier(string s)
        {
            switch (s)
            {
                case FrameworkIdentifiers.NetFramework:
                    return Runtime.Net;
                case FrameworkIdentifiers.NetCoreApp:
                    return Runtime.NetCore;
                case FrameworkIdentifiers.NetStandard:
                    throw new NUnitEngineException(
                        "Test assemblies must target a specific platform, rather than .NETStandard.");
            }

            throw new NUnitEngineException("Unrecognized Target Framework Identifier: " + s);
        }

        #endregion

        #region Absract Properties and Methods

        public abstract string DisplayName { get; }

        public abstract string FrameworkIdentifier { get; }

        public abstract bool Matches(Runtime targetRuntime);

        public virtual bool Supports(Version runtime, Version target)
        {
            // We assume that Major versions must match.
            return runtime.Major == target.Major && runtime.Minor >= target.Minor;
        }

        public abstract override string ToString();

        #endregion

        #region Nested Runtime Classes

        private class NetFrameworkRuntime : Runtime
        {
            public override string DisplayName => ".NET";
            public override string FrameworkIdentifier => FrameworkIdentifiers.NetFramework;

            public override string ToString() => "Net";
            public override bool Matches(Runtime targetRuntime) => targetRuntime is NetFrameworkRuntime;

            public override bool Supports(Version runtime, Version target)
            {
                // Runtime 3 supports runtime 2.
                return base.Supports(runtime, target) ||
                    (runtime.Major == 3 && (target.Major == 2 || target.Major == 3) &&
                    runtime.Minor >= target.Minor);
            }
        }

        private class MonoRuntime : NetFrameworkRuntime
        {
            public override string DisplayName => "Mono";

            public override string ToString() => "Mono";

            public override bool Supports(Version runtime, Version target)
            {
                return base.Supports(runtime, target) || runtime.Major >= 4 && target.Major == 4;
            }
        }

        private class NetCoreRuntime : Runtime
        {
            public override string DisplayName => ".NETCore";
            public override string FrameworkIdentifier => FrameworkIdentifiers.NetCoreApp;

            public override string ToString() => "NetCore";

            public override bool Matches(Runtime targetRuntime) => targetRuntime is NetCoreRuntime;

            public override bool Supports(Version runtime, Version target)
            {
                // We assume that all later versions support all previous version.
                return runtime.Major > target.Major ||
                    (runtime.Major == target.Major && runtime.Minor >= target.Minor);
            }
        }

#endregion
    }
}
