// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine
{
    /// <summary>
    /// InternalEngineSettings contains constant values that
    /// are used as keys in setting up a TestPackage. 
    /// Values here are set/used internally within the engine. 
    /// Setting values may be a string, int or bool.
    /// </summary>
    public static class InternalEnginePackageSettings
    {
        /// <summary>
        /// If the package represents an assembly, then this is the CLR version
        /// stored in the assembly image. If it represents a project or other
        /// group of assemblies, it is the maximum version for all the assemblies.
        /// </summary>
        public const string ImageRuntimeVersion = "ImageRuntimeVersion";

        /// <summary>
        /// True if any assembly in the package requires running as a 32-bit
        /// process when on a 64-bit system.
        /// </summary>
        public const string ImageRequiresX86 = "ImageRequiresX86";

        /// <summary>
        /// True if any assembly in the package requires a special assembly resolution hook
        /// in the default application domain in order to find dependent assemblies.
        /// </summary>
        public const string ImageRequiresDefaultAppDomainAssemblyResolver = "ImageRequiresDefaultAppDomainAssemblyResolver";

        /// <summary>
        /// The FrameworkName specified on a TargetFrameworkAttribute for the assembly
        /// </summary>
        public const string ImageTargetFrameworkName = "ImageTargetFrameworkName";
    }
}
