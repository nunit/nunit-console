﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Text;

// Missing XML Docs
#pragma warning disable 1591

namespace NUnit.Engine
{
    public enum TokenKind
    {
        Eof,
        Word,
        String,
        Symbol
    }

    public class Token
    {
        public Token(TokenKind kind) : this(kind, string.Empty)
        {
        }

        public Token(TokenKind kind, char ch) : this(kind, ch.ToString())
        {
        }

        public Token(TokenKind kind, string text)
        {
            Kind = kind;
            Text = text;
        }

        public TokenKind Kind { get; private set; }

        public string Text { get; private set; }

        public int Pos { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is Token token && this == token;
        }

        public override int GetHashCode()
        {
            return Text.GetHashCode();
        }

        public override string ToString()
        {
            return Text is not null
                ? Kind.ToString() + ":" + Text
                : Kind.ToString();
        }

        public static bool operator ==(Token? t1, Token? t2)
        {
            if (ReferenceEquals(t1, t2))
                return true;

            if (t1 is null || t2 is null)
                return false;

            return t1.Kind == t2.Kind && t1.Text == t2.Text;
        }

        public static bool operator !=(Token t1, Token t2)
        {
            return !(t1 == t2);
        }
    }

    /// <summary>
    /// Tokenizer class performs lexical analysis for the TestSelectionParser.
    /// It recognizes a very limited set of tokens: words, symbols and
    /// quoted strings. This is sufficient for the simple DSL we use to
    /// select which tests to run.
    /// </summary>
    public class Tokenizer
    {
        private string _input;
        private int _index;

        private const char EOF_CHAR = '\0';
        private const string WORD_BREAK_CHARS = "=!()&|";
        private readonly string[] DOUBLE_CHAR_SYMBOLS = new string[] { "==", "=~", "!=", "!~", "&&", "||" };

        private Token? _lookahead;

        public Tokenizer(string input)
        {
            Guard.ArgumentNotNull(input);

            _input = input;
            _index = 0;
        }

        public Token LookAhead
        {
            get
            {
                if (_lookahead is null)
                    _lookahead = GetNextToken();

                return _lookahead;
            }
        }

        public Token NextToken()
        {
            Token result = _lookahead ?? GetNextToken();
            _lookahead = null;
            return result;
        }

        private Token GetNextToken()
        {
            SkipBlanks();

            var ch = NextChar;
            int pos = _index;

            switch (ch)
            {
                case EOF_CHAR:
                    return new Token(TokenKind.Eof) { Pos = pos };

                // Single char symbols
                case '(':
                case ')':
                    GetChar();
                    return new Token(TokenKind.Symbol, ch) { Pos = pos };

                // Possible double char symbols
                case '&':
                case '|':
                case '=':
                case '!':
                    GetChar();
                    foreach (string dbl in DOUBLE_CHAR_SYMBOLS)
                        if (ch == dbl[0] && NextChar == dbl[1])
                        {
                            GetChar();
                            return new Token(TokenKind.Symbol, dbl) { Pos = pos };
                        }

                    return new Token(TokenKind.Symbol, ch);

                case '"':
                case '\'':
                case '/':
                    return GetString();

                default:
                    return GetWord();
            }
        }

        private static bool IsWordChar(char c)
        {
            if (char.IsWhiteSpace(c) || c == EOF_CHAR)
                return false;

            return WORD_BREAK_CHARS.IndexOf(c) < 0;
        }

        private Token GetWord()
        {
            var sb = new StringBuilder();
            int pos = _index;

            while (IsWordChar(NextChar))
                sb.Append(GetChar());

            return new Token(TokenKind.Word, sb.ToString()) { Pos = pos };
        }

        private Token GetString()
        {
            var sb = new StringBuilder();
            int pos = _index;

            char quote = GetChar(); // Save the initial quote char

            while (NextChar != EOF_CHAR)
            {
                var ch = GetChar();
                if (ch == '\\')
                    ch = GetChar();
                else if (ch == quote)
                    break;
                sb.Append(ch);
            }

            return new Token(TokenKind.String, sb.ToString()) { Pos = pos };
        }

        /// <summary>
        /// Get the next character in the input, consuming it.
        /// </summary>
        /// <returns>The next char</returns>
        private char GetChar()
        {
            return _index < _input.Length ? _input[_index++] : EOF_CHAR;
        }

        /// <summary>
        /// Peek ahead at the next character in input
        /// </summary>
        private char NextChar
        {
            get
            {
                return _index < _input.Length ? _input[_index] : EOF_CHAR;
            }
        }

        private void SkipBlanks()
        {
            while (char.IsWhiteSpace(NextChar))
                _index++;
        }
    }
}
