﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

// Missing XML Docs
#pragma warning disable 1591

namespace NUnit.Engine
{
    public class TestSelectionParser
    {
        private Tokenizer _tokenizer;

        private static readonly Token LPAREN = new Token(TokenKind.Symbol, "(");
        private static readonly Token RPAREN = new Token(TokenKind.Symbol, ")");
        private static readonly Token AND_OP1 = new Token(TokenKind.Symbol, "&");
        private static readonly Token AND_OP2 = new Token(TokenKind.Symbol, "&&");
        private static readonly Token AND_OP3 = new Token(TokenKind.Word, "and");
        private static readonly Token AND_OP4 = new Token(TokenKind.Word, "AND");
        private static readonly Token OR_OP1 = new Token(TokenKind.Symbol, "|");
        private static readonly Token OR_OP2 = new Token(TokenKind.Symbol, "||");
        private static readonly Token OR_OP3 = new Token(TokenKind.Word, "or");
        private static readonly Token OR_OP4 = new Token(TokenKind.Word, "OR");
        private static readonly Token NOT_OP = new Token(TokenKind.Symbol, "!");

        private static readonly Token EQ_OP1 = new Token(TokenKind.Symbol, "=");
        private static readonly Token EQ_OP2 = new Token(TokenKind.Symbol, "==");
        private static readonly Token NE_OP = new Token(TokenKind.Symbol, "!=");
        private static readonly Token MATCH_OP = new Token(TokenKind.Symbol, "=~");
        private static readonly Token NOMATCH_OP = new Token(TokenKind.Symbol, "!~");

        private static readonly Token[] AND_OPS = new Token[] { AND_OP1, AND_OP2, AND_OP3, AND_OP4 };
        private static readonly Token[] OR_OPS = new Token[] { OR_OP1, OR_OP2, OR_OP3, OR_OP4 };
        private static readonly Token[] EQ_OPS = new Token[] { EQ_OP1, EQ_OP2 };
        private static readonly Token[] REL_OPS = new Token[] { EQ_OP1, EQ_OP2, NE_OP, MATCH_OP, NOMATCH_OP };

        private static readonly Token EOF = new Token(TokenKind.Eof);
        private static readonly Token COMMA = new Token(TokenKind.Symbol, ",");

        public string Parse(string input)
        {
            _tokenizer = new Tokenizer(input);

            if (_tokenizer.LookAhead == EOF)
                throw new TestSelectionParserException("No input provided for test selection.");

            var result = ParseFilterExpression();

            Expect(EOF);
            return result;
        }

        /// <summary>
        /// Parse a single term or an or expression, returning the xml
        /// </summary>
        /// <returns></returns>
        public string ParseFilterExpression()
        {
            var terms = new List<string>();
            terms.Add(ParseFilterTerm());

            while (LookingAt(OR_OPS))
            {
                NextToken();
                terms.Add(ParseFilterTerm());
            }

            if (terms.Count == 1)
                return terms[0];

            var sb = new StringBuilder("<or>");

            foreach (string term in terms)
                sb.Append(term);

            sb.Append("</or>");

            return sb.ToString();
        }

        /// <summary>
        /// Parse a single element or an and expression and return the xml
        /// </summary>
        public string ParseFilterTerm()
        {
            var elements = new List<string>();
            elements.Add(ParseFilterElement());

            while (LookingAt(AND_OPS))
            {
                NextToken();
                elements.Add(ParseFilterElement());
            }

            if (elements.Count == 1)
                return elements[0];

            var sb = new StringBuilder("<and>");

            foreach (string element in elements)
                sb.Append(element);

            sb.Append("</and>");

            return sb.ToString();
        }

        /// <summary>
        /// Parse a single filter element such as a category expression
        /// and return the xml representation of the filter.
        /// </summary>
        public string ParseFilterElement()
        {
            if (LookingAt(LPAREN, NOT_OP))
                return ParseExpressionInParentheses();

            Token lhs = Expect(TokenKind.Word);
            Token op;
            Token rhs;

            switch (lhs.Text)
            {
                case "test":
                    op = Expect(REL_OPS);
                    rhs = op == MATCH_OP || op == NOMATCH_OP
                        ? Expect(TokenKind.String, TokenKind.Word)
                        : GetTestName();
                    return EmitFilterElement(lhs, op, rhs);

                case "cat":
                case "method":
                case "class":
                case "name":
                case "namespace":
                case "partition":
                    op = Expect(REL_OPS);
                    rhs = Expect(TokenKind.String, TokenKind.Word);
                    return EmitFilterElement(lhs, op, rhs);

                case "id":
                    op = Expect(EQ_OPS);
                    rhs = Expect(TokenKind.String, TokenKind.Word);
                    return EmitFilterElement(lhs, op, rhs);

                default:
                    // Assume it's a property name
                    op = Expect(REL_OPS);
                    rhs = Expect(TokenKind.String, TokenKind.Word);
                    return EmitPropertyElement(lhs, op, rhs);
                    //throw InvalidTokenError(lhs);
            }
        }

        // TODO: We do extra work for test names due to the fact that
        // Windows drops double quotes from arguments in many situations.
        // It would be better to parse the command-line directly but
        // that will mean a significant rewrite.
        private Token GetTestName()
        {
            var result = Expect(TokenKind.String, TokenKind.Word);
            var sb = new StringBuilder();

            if (result.Kind == TokenKind.String)
            {
                var inQuotes = false;
                var inEscape = false;
                foreach (var ch in result.Text)
                {
                    if (ch == ' ' && !inQuotes)
                        continue;
                    sb.Append(ch);
                    inQuotes = (!inQuotes && ch == '"') || (inQuotes && ch != '"') || (inQuotes  && ch == '"' && inEscape);
                    inEscape = inQuotes && !inEscape && ch == '\\';
                }
            }
            else
            {
                // Word Token - check to see if it's followed by a left parenthesis
                if (_tokenizer.LookAhead != LPAREN) return result;

                // We have a "Word" token followed by a left parenthesis
                // This may be a testname entered without quotes or one
                // using double quotes, which were removed by the shell.

                sb = new StringBuilder(result.Text);
                var token = NextToken();

                while (token != EOF)
                {
                    bool isString = token.Kind == TokenKind.String;

                    if (isString) sb.Append('"');
                    sb.Append(token.Text);
                    if (isString) sb.Append('"');

                    token = NextToken();
                }
            }

            return new Token(TokenKind.String, sb.ToString());
        }

        private static string EmitFilterElement(Token lhs, Token op, Token rhs)
        {
            string fmt = null;

            if (op == EQ_OP1 || op == EQ_OP2)
                fmt = "<{0}>{1}</{0}>";
            else if (op == NE_OP)
                fmt = "<not><{0}>{1}</{0}></not>";
            else if (op == MATCH_OP)
                fmt = "<{0} re='1'>{1}</{0}>";
            else if (op == NOMATCH_OP)
                fmt = "<not><{0} re='1'>{1}</{0}></not>";
            else
                fmt = "<{0} op='" + op.Text + "'>{1}</{0}>";

            return EmitElement(fmt, lhs, rhs);
        }

        private static string EmitPropertyElement(Token lhs, Token op, Token rhs)
        {
            string fmt = null;

            if (op == EQ_OP1 || op == EQ_OP2)
                fmt = "<prop name='{0}'>{1}</prop>";
            else if (op == NE_OP)
                fmt = "<not><prop name='{0}'>{1}</prop></not>";
            else if (op == MATCH_OP)
                fmt = "<prop name='{0}' re='1'>{1}</prop>";
            else if (op == NOMATCH_OP)
                fmt = "<not><prop name='{0}' re='1'>{1}</prop></not>";
            else
                fmt = "<prop name='{0}' op='" + op.Text + "'>{1}</prop>";

            return EmitElement(fmt, lhs, rhs);
        }

        private static string EmitElement(string fmt, Token lhs, Token rhs)
        {
            return string.Format(fmt, lhs.Text, XmlEscape(rhs.Text));
        }

        private string ParseExpressionInParentheses()
        {
            Token op = Expect(LPAREN, NOT_OP);

            if (op == NOT_OP) Expect(LPAREN);

            string result = ParseFilterExpression();

            Expect(RPAREN);

            if (op == NOT_OP)
                result = "<not>" + result + "</not>";

            return result;
        }

        // Require a token of one or more kinds
        private Token Expect(params TokenKind[] kinds)
        {
            Token token = NextToken();

            foreach (TokenKind kind in kinds)
                if (token.Kind == kind)
                    return token;

            throw InvalidTokenError(token);
        }

        // Require a token from a list of tokens
        private Token Expect(params Token[] valid)
        {
            Token token = NextToken();

            foreach (Token item in valid)
                if (token == item)
                    return token;

            throw InvalidTokenError(token);
        }

        private Exception InvalidTokenError(Token token)
        {
            return new TestSelectionParserException(string.Format(
                "Unexpected token '{0}' at position {1} in selection expression.", token.Text, token.Pos));
        }

        private Token LookAhead
        {
            get { return _tokenizer.LookAhead; }
        }

        private bool LookingAt(params Token[] tokens)
        {
            foreach (Token token in tokens)
                if (LookAhead == token)
                    return true;

            return false;
        }

        private Token NextToken()
        {
            return _tokenizer.NextToken();
        }

        private static string XmlEscape(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("'", "&apos;");
        }
    }
}
