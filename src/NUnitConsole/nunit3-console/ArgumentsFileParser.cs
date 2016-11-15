// ***********************************************************************
// Copyright (c) 2011 Charlie Poole
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
using System.Text.RegularExpressions;

namespace NUnit.ConsoleRunner
{
    internal class ArgumentsFileParser : IConverter<IEnumerable<string>, IEnumerable<string>>
    {
        private static readonly Regex ArgsRegex = new Regex(@"\G(""((""""|[^""])+)""|(\S+)) *", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public IEnumerable<string> Convert(IEnumerable<string> src)
        {
            if (src == null) throw new ArgumentNullException("src");

            foreach (var line in src)
            {
                foreach (Match argMatch in ArgsRegex.Matches(line))
                {
                    if (!argMatch.Success)
                    {
                        continue;
                    }

                    yield return Regex.Replace(argMatch.Groups[2].Success ? argMatch.Groups[2].Value : argMatch.Groups[4].Value, @"""""", @"""");
                }
            }
        }
    }
}
