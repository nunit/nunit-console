// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;

namespace NUnit.Engine.Internal
{
    public delegate bool TestPackageSelectorDelegate(ITestPackage p);

    /// <summary>
    /// Extension methods for use with TestPackages
    /// </summary>
    public static class TestPackageExtensions
    {
        public static bool IsAssemblyPackage(this ITestPackage package)
        {
            return package.FullName != null && PathUtils.IsAssemblyFileType(package.FullName);
        }

        public static bool HasSubPackages(this ITestPackage package)
        {
            return package.SubPackages.Count > 0;
        }

        public static IList<ITestPackage> Select(this ITestPackage package, TestPackageSelectorDelegate selector)
        {
            var selection = new List<ITestPackage>();

            AccumulatePackages(package, selection, selector);

            return selection;
        }

        private static void AccumulatePackages(ITestPackage package, IList<ITestPackage> selection, TestPackageSelectorDelegate selector)
        {
            if (selector(package))
                selection.Add(package);

            foreach (var subPackage in package.SubPackages)
                AccumulatePackages(subPackage, selection, selector);
        }
    }
}
