// ***********************************************************************
// Copyright (c) 2016 Joseph N. Musser II
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

using System.Text;

namespace NUnit.Engine.Internal
{
    public static class ProcessUtils
    {
        private static readonly char[] CharsThatRequireQuoting = { ' ', '"' };
        private static readonly char[] CharsThatRequireEscaping = { '\\', '"' };

        /// <summary>
        /// Escapes arbitrary values so that the process receives the exact string you intend and injection is impossible.
        /// Spec: https://docs.microsoft.com/en-gb/windows/desktop/api/shellapi/nf-shellapi-commandlinetoargvw
        /// </summary>
        public static void EscapeProcessArgument(this StringBuilder builder, string literalValue, bool alwaysQuote = false)
        {
            if (string.IsNullOrEmpty(literalValue))
            {
                builder.Append("\"\"");
                return;
            }

            if (literalValue.IndexOfAny(CharsThatRequireQuoting) == -1) // Happy path
            {
                if (!alwaysQuote)
                {
                    builder.Append(literalValue);
                    return;
                }
                if (literalValue[literalValue.Length - 1] != '\\')
                {
                    builder.Append('"').Append(literalValue).Append('"');
                    return;
                }
            }

            builder.Append('"');

            var nextPosition = 0;
            while (true)
            {
                var nextEscapeChar = literalValue.IndexOfAny(CharsThatRequireEscaping, nextPosition);
                if (nextEscapeChar == -1) break;

                builder.Append(literalValue, nextPosition, nextEscapeChar - nextPosition);
                nextPosition = nextEscapeChar + 1;

                switch (literalValue[nextEscapeChar])
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        var numBackslashes = 1;
                        while (nextPosition < literalValue.Length && literalValue[nextPosition] == '\\')
                        {
                            numBackslashes++;
                            nextPosition++;
                        }
                        if (nextPosition == literalValue.Length || literalValue[nextPosition] == '"')
                            numBackslashes <<= 1;

                        for (; numBackslashes != 0; numBackslashes--)
                            builder.Append('\\');
                        break;
                }
            }

            builder.Append(literalValue, nextPosition, literalValue.Length - nextPosition);
            builder.Append('"');
        }
    }
}
