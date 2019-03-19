// ***********************************************************************
// Copyright (c) 2019 Charlie Poole, Rob Prouse
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

using System;
using System.Collections.Generic;

namespace NUnit.Engine.Internal
{
    public delegate bool TestPackageSelectorDelegate(TestPackage p);

    /// <summary>
    /// Extension methods for use with TestPackages
    /// </summary>
    public static class TestPackageExtensions
    {
        public static bool IsAssemblyPackage(this TestPackage package)
        {
            return package.FullName != null && PathUtils.IsAssemblyFileType(package.FullName);
        }

        public static bool HasSubPackages(this TestPackage package)
        {
            return package.SubPackages.Count > 0;
        }

        public static IList<TestPackage> Select(this TestPackage package, TestPackageSelectorDelegate selector)
        {
            var selection = new List<TestPackage>();

            AccumulatePackages(package, selection, selector);

            return selection;
        }

        private static void AccumulatePackages(TestPackage package, IList<TestPackage> selection, TestPackageSelectorDelegate selector)
        {
            if (selector(package))
                selection.Add(package);

            foreach (var subPackage in package.SubPackages)
                AccumulatePackages(subPackage, selection, selector);
        }
    }
}
