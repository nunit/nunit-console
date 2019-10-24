// ***********************************************************************
// Copyright (c) 2016-2018 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

#if !NETSTANDARD1_6
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

#if !NETSTANDARD2_0
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
#endif