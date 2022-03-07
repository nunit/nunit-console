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

        public abstract Version GetClrVersionForFramework(Version frameworkVersion);

        #endregion

        #region Nested Runtime Classes

        private class NetFrameworkRuntime : Runtime
        {
            public override string DisplayName => ".NET";
            public override string FrameworkIdentifier => FrameworkIdentifiers.NetFramework;

            public override string ToString() => "Net";
            public override bool Matches(Runtime targetRuntime) => targetRuntime is NetFrameworkRuntime;

            public override Version GetClrVersionForFramework(Version frameworkVersion)
            {
                switch (frameworkVersion.Major)
                {
                    case 1:
                        switch (frameworkVersion.Minor)
                        {
                            case 0:
                                return new Version(1, 0, 3705);
                            case 1:
                                return new Version(1, 1, 4322);
                        }
                        break;
                    case 2:
                    case 3:
                        return new Version(2, 0, 50727);
                    case 4:
                        return new Version(4, 0, 30319);
                }

                throw new ArgumentException($"Unknown version for .NET Framework: {frameworkVersion}", "version");
            }
        }

        private class MonoRuntime : NetFrameworkRuntime
        {
            public override string DisplayName => "Mono";

            public override string ToString() => "Mono";

            public override Version GetClrVersionForFramework(Version frameworkVersion)
            {
                switch (frameworkVersion.Major)
                {
                    case 1:
                        return new Version(1, 1, 4322);
                    case 2:
                    case 3:
                        return new Version(2, 0, 50727);
                    case 4:
                        return new Version(4, 0, 30319);
                }

                throw new ArgumentException($"Unknown version for Mono runtime: {frameworkVersion}", "version");
            }
        }

        private class NetCoreRuntime : Runtime
        {
            public override string DisplayName => ".NETCore";
            public override string FrameworkIdentifier => FrameworkIdentifiers.NetCoreApp;

            public override string ToString() => "NetCore";
            public override bool Matches(Runtime targetRuntime) => targetRuntime is NetCoreRuntime;

            public override Version GetClrVersionForFramework(Version frameworkVersion)
            {
                switch(frameworkVersion.Major)
                {
                    case 1:
                    case 2:
                        return new Version(4, 0, 30319);
                    case 3:
                        return new Version(3, 1, 10);
                    case 5:
                        return new Version(5, 0, 1);
                    case 6:
                        return new Version(6, 0, 0);
                }

                throw new ArgumentException($"Unknown .NET Core version: {frameworkVersion}", "version");
            }
        }

        #endregion
    }
}
