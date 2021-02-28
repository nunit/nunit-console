// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Common;

namespace NUnit.Engine.Extensibility
{
    internal static class ExtensionSelector
    {
        /// <summary>
        /// IsDuplicateOf returns true if two assemblies have the same name.
        /// </summary>
        public static bool IsDuplicateOf(this IExtensionAssembly first, IExtensionAssembly second)
        {
            return first.AssemblyName == second.AssemblyName;
        }

        /// <summary>
        /// IsBetterVersion determines whether another assembly is
        /// a better than the current assembly. It first looks at
        /// for the highest assembly version, and then the highest target
        /// framework. With a tie situation, assemblies specified directly
        /// are prefered to those located via wildcards.
        ///
        /// It is only intended to be called if IsDuplicateOf
        /// has already returned true. This method does no work to check if
        /// the target framework found is available under the current engine.
        /// </summary>
        public static bool IsBetterVersionOf(this IExtensionAssembly first, IExtensionAssembly second)
        {
            Guard.OperationValid(first.IsDuplicateOf(second), "IsBetterVersionOf should only be called on duplicate assemblies");

            //Look at assembly version
            var firstVersion = first.AssemblyVersion;
            var secondVersion = second.AssemblyVersion;
            if (firstVersion > secondVersion)
                return true;

            if (firstVersion < secondVersion)
                return false;

#if NETFRAMEWORK
            //Look at target runtime
            var firstTargetRuntime = first.TargetFramework.FrameworkVersion;
            var secondTargetRuntime = second.TargetFramework.FrameworkVersion;
            if (firstTargetRuntime > secondTargetRuntime)
                return true;

            if (firstTargetRuntime < secondTargetRuntime)
                return false;
#endif

            //Everything is equal, override only if this one was specified exactly while the other wasn't
            return !first.FromWildCard && second.FromWildCard;
        }
    }
}
