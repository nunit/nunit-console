// ***********************************************************************
// Copyright (c) 2014 Charlie Poole, Rob Prouse
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

using System.Reflection;

//
// Common Information for the NUnit assemblies
//
[assembly: AssemblyProduct("NUnit 3")]
[assembly: AssemblyTrademark("NUnit is a trademark of NUnit Software")]
[assembly: AssemblyCompany("NUnit Software")]
[assembly: AssemblyCopyright("Copyright (c) 2018 Charlie Poole, Rob Prouse")]

#if PORTABLE
[assembly: AssemblyMetadata("PCL", "True")]
#endif

#if DEBUG
#if NET_4_5
[assembly: AssemblyConfiguration(".NET 4.5 Debug")]
#elif NET_4_0
[assembly: AssemblyConfiguration(".NET 4.0 Debug")]
#elif NET_2_0
[assembly: AssemblyConfiguration(".NET 2.0 Debug")]
#elif PORTABLE
[assembly: AssemblyConfiguration("Portable Debug")]
#elif NETSTANDARD1_3 || NETSTANDARD1_6 || NETCOREAPP1_0
[assembly: AssemblyConfiguration(".NET Standard Debug")]
#else
[assembly: AssemblyConfiguration("Debug")]
#endif
#else
#if NET_4_5
[assembly: AssemblyConfiguration(".NET 4.5")]
#elif NET_4_0
[assembly: AssemblyConfiguration(".NET 4.0")]
#elif NET_2_0
[assembly: AssemblyConfiguration(".NET 2.0")]
#elif PORTABLE
[assembly: AssemblyConfiguration("Portable")]
#elif NETSTANDARD1_3 || NETSTANDARD1_6 || NETCOREAPP1_0
[assembly: AssemblyConfiguration(".NET Standard")]
#else
[assembly: AssemblyConfiguration("")]
#endif
#endif
