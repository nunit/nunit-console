// ***********************************************************************
// Copyright (c) 2018 Charlie Poole, Rob Prouse
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

using System.Collections.Generic;
using NUnit.Common;

namespace NUnit.Engine
{
    internal static class Extensions
    {
        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            Guard.ArgumentNotNull(source, nameof(source));

            using (var en = source.GetEnumerator())
            {
                if (en.MoveNext()) return en.Current;
            }

            return default(TSource);
        }

        public static bool TryFirst<TSource>(this IEnumerable<TSource> source, out TSource value)
        {
            Guard.ArgumentNotNull(source, nameof(source));

            using (var en = source.GetEnumerator())
            {
                if (en.MoveNext())
                {
                    value = en.Current;
                    return true;
                }
            }

            value = default(TSource);
            return false;
        }

        public static bool TryFirst<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out TSource value)
        {
            Guard.ArgumentNotNull(source, nameof(source));
            Guard.ArgumentNotNull(predicate, nameof(predicate));

            foreach (var item in source)
            {
                if (!predicate.Invoke(item)) continue;
                value = item;
                return true;
            }

            value = default(TSource);
            return false;
        }
    }
}
