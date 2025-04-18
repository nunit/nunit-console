﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Framework;

namespace NUnit.Engine.Tests
{
    public class TokenizerTests
    {
        [Test]
        public void NullInputThrowsException()
        {
            Assert.That(() => new Tokenizer(null), Throws.ArgumentNullException);
        }

        [Test]
        public void BlankStringReturnsEof()
        {
            var tokenizer = new Tokenizer("    ");
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Eof)), "First Call");
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Eof)), "Second Call");
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Eof)), "Third Call");
        }

        [Test]
        public void IdentifierTokens()
        {
            var tokenizer = new Tokenizer("  Identifiers x abc123 a1x  ");
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "Identifiers")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "x")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "abc123")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "a1x")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Eof)));
        }

        [Test]
        public void WordsInUnicode()
        {
            var tokenizer = new Tokenizer("method == Здравствуйте");
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "method")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "==")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "Здравствуйте")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Eof)));
        }

        [Test]
        public void WordsWithSpecialCharacters()
        {
            var tokenizer = new Tokenizer("word_with_underscores word-with-dashes word.with.dots");
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "word_with_underscores")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "word-with-dashes")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "word.with.dots")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Eof)));
        }

        private const string WORD_BREAK_CHARS = "=!()&| \t,";
        [Test]
        public void WordBreakCharacters()
        {
            var tokenizer = new Tokenizer("word1==word2!=word3 func(arg1, arg2) this&&that||both");
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "word1")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "==")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "word2")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "!=")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "word3")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "func")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "(")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "arg1")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, ",")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "arg2")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, ")")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "this")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "&&")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "that")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "||")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "both")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Eof)));
        }

        [Test]
        public void StringWithDoubleQuotes()
        {
            var tokenizer = new Tokenizer("\"string at start\" \"may contain ' char\" \"string at end\"");
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.String, "string at start")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.String, "may contain ' char")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.String, "string at end")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Eof)));
        }

        [Test]
        public void StringWithSingleQuotes()
        {
            var tokenizer = new Tokenizer("'string at start' 'may contain \" char' 'string at end'");
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.String, "string at start")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.String, "may contain \" char")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.String, "string at end")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Eof)));
        }

        [Test]
        public void StringWithSlashes()
        {
            var tokenizer = new Tokenizer("/string at start/ /may contain \" char/ /string at end/");
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.String, "string at start")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.String, "may contain \" char")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.String, "string at end")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Eof)));
        }

        [Test]
        public void TestNameWithParameters()
        {
            var tokenizer = new Tokenizer("test=='Issue1510.TestSomething(Option1,\"ABC\")'");
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "test")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "==")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.String, "Issue1510.TestSomething(Option1,\"ABC\")")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Eof)));
        }

        [Test]
        public void StringsMayContainEscapedQuoteChar()
        {
            var tokenizer = new Tokenizer("/abc\\/xyz/   'abc\\'xyz'  \"abc\\\"xyz\"");
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.String, "abc/xyz")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.String, "abc'xyz")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.String, "abc\"xyz")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Eof)));
        }

        [Test]
        public void SymbolTokens_SingleChar()
        {
            var tokenizer = new Tokenizer("=!&|()");
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "=")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "!")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "&")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "|")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "(")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, ")")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Eof)));
        }

        [Test]
        public void SymbolTokens_DoubleChar()
        {
            var tokenizer = new Tokenizer("==&&||!==~!~");
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "==")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "&&")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "||")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "!=")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "=~")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "!~")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Eof)));
        }

        [Test]
        public void MixedTokens_Simple()
        {
            var tokenizer = new Tokenizer("id=123");
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "id")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "=")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "123")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Eof)));
        }

        [Test]
        public void MixedTokens_Complex()
        {
            var tokenizer = new Tokenizer("name =~ '*DataBase*' && (category = Urgent || Priority = High)");
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "name")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "=~")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.String, "*DataBase*")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "&&")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "(")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "category")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "=")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "Urgent")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "||")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "Priority")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "=")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "High")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, ")")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Eof)));
        }
    }
}
