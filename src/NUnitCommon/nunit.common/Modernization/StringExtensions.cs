// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if !NETSTANDARD2_1 && !NETCOREAPP2_0_OR_GREATER

namespace NUnit.Common
{
    public static class StringExtensions
    {
        public static bool EndsWith(this string s, char c)
        {
            return s.Length > 0 && s[s.Length - 1] == c;
        }

        public static bool StartsWith(this string s, char c)
        {
            return s.Length > 0 && s[0] == c;
        }

        public static bool Contains(this string s, char c)
        {
            return s.IndexOf(c) >= 0;
        }
    }
}

#endif
