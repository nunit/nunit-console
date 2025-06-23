// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;

// Missing XML Docs
#pragma warning disable 1591

namespace NUnit.Engine
{
    /// <summary>
    /// TestSelectionParser parses the content of a where clause in TSL
    /// (Test Selection Language) and produces the XML representation
    /// of the required TestFilter.
    /// </summary>
    /// <remarks>
    /// This is a simple recursive descent parser, with a single level
    /// of look-ahead. It makes uses of a separate Tokenizer to extract
    /// individual tokens from the input. The original parser produced
    /// output directly but sometimes incorporated invalid XML. To fix
    /// that problem, it now produces an intermediate representation of
    /// the filter as a tree of nodes, which is then walked to produce
    /// final output using an XmlWriter.
    /// </remarks>
    public class TestSelectionParser
    {
        private readonly Tokenizer _tokenizer;

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

        public static string Parse(string input)
        {
            var parser = new TestSelectionParser(new Tokenizer(input));

            StringBuilder filter = new StringBuilder();
            var xmlWriter = XmlWriter.Create(filter, new XmlWriterSettings { OmitXmlDeclaration = true });
            parser.Parse().Emit(xmlWriter);
            xmlWriter.Close();
            return filter.ToString();
        }

        public static void Parse(string input, XmlWriter xmlWriter)
        {
            new TestSelectionParser(new Tokenizer(input)).Parse().Emit(xmlWriter);
        }

        private TestSelectionParser(Tokenizer tokenizer)
        {
            _tokenizer = tokenizer;
        }

        private FilterNode Parse()
        {
            if (_tokenizer.LookAhead == EOF)
                throw new TestSelectionParserException("No input provided for test selection.");

            var result = ParseFilterExpression();

            //Expect(EOF);
            return result;
        }

        /// <summary>
        /// Parse a single term or an or expression, returning the xml
        /// </summary>
        private FilterNode ParseFilterExpression()
        {
            // <FilterExpression> ::= <FilterTerm> | <FilterExpression> "or" <FilterTerm>
            var terms = new List<FilterNode>();
            terms.Add(ParseFilterTerm());

            while (LookingAt(OR_OPS))
            {
                NextToken();
                terms.Add(ParseFilterTerm());
            }

            if (terms.Count == 1)
                return terms[0];

            return new FilterExpression(terms.ToArray());
        }

        /// <summary>
        /// Parse a single element or an and expression and return the xml
        /// </summary>
        private FilterNode ParseFilterTerm()
        {
            // <FilterTerm> ::= <FilterElement> | <FilterTerm> "and" <FilterElement>
            var elements = new List<FilterNode>();
            elements.Add(ParseFilterElement());

            while (LookingAt(AND_OPS))
            {
                NextToken();
                elements.Add(ParseFilterElement());
            }

            if (elements.Count == 1)
                return elements[0];

            return new AndOp(elements.ToArray());
        }

        /// <summary>
        /// Parse a single filter element such as a category expression
        /// and return the xml representation of the filter.
        /// </summary>
        private FilterNode ParseFilterElement()
        {
            Token token = NextToken();
            if (token == NOT_OP)
                return new NegatedElement(ParseFilterElement());

            if (token == LPAREN)
            {
                FilterNode result = ParseFilterExpression();
                Expect(RPAREN);
                return result;
            }

            if (token.Kind != TokenKind.Word)
                InvalidTokenError(token);

            Token op = Expect(REL_OPS);
            bool negated = op == NE_OP || op == NOMATCH_OP;
            if (negated)
                op = op == NE_OP
                    ? EQ_OP1
                    : MATCH_OP;
            string lhs = token.Text;
            string rhs = op == MATCH_OP && lhs == "test"
                ? Expect(TokenKind.String, TokenKind.Word).Text
                : GetTestName().Text;

            FilterNode element = new FilterElement(op, lhs, rhs);

            if (negated)
                element = new NegatedElement(element);

            return element;
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
                    inQuotes = (!inQuotes && ch == '"') || (inQuotes && ch != '"') || (inQuotes && ch == '"' && inEscape);
                    inEscape = inQuotes && !inEscape && ch == '\\';
                }
            }
            else
            {
                // Word Token - check to see if it's followed by a left parenthesis
                if (_tokenizer.LookAhead != LPAREN)
                    return result;

                // We have a "Word" token followed by a left parenthesis
                // This may be a testname entered without quotes or one
                // using double quotes, which were removed by the shell.

                sb = new StringBuilder(result.Text);
                var token = NextToken();

                while (token != EOF)
                {
                    bool isString = token.Kind == TokenKind.String;

                    if (isString)
                        sb.Append('"');
                    sb.Append(token.Text);
                    if (isString)
                        sb.Append('"');

                    token = NextToken();
                }
            }

            return new Token(TokenKind.String, sb.ToString());
        }

        private abstract class FilterNode
        {
            public abstract void Emit(XmlWriter xmlWriter);
        }

        private class FilterExpression : FilterNode
        {
            public FilterNode[] Terms;

            public FilterExpression(FilterNode[] terms)
            {
                Terms = terms;
            }

            public override void Emit(XmlWriter xmlWriter)
            {
                xmlWriter.WriteStartElement("or");
                foreach (var element in Terms)
                    element.Emit(xmlWriter);
                xmlWriter.WriteEndElement();
            }
        }

        private class AndOp : FilterNode
        {
            public FilterNode[] Elements;

            public AndOp(FilterNode[] elements)
            {
                Elements = elements;
            }

            public override void Emit(XmlWriter xmlWriter)
            {
                xmlWriter.WriteStartElement("and");
                foreach (var element in Elements)
                    element.Emit(xmlWriter);
                xmlWriter.WriteEndElement();
            }
        }

        private class NegatedElement : FilterNode
        {
            public FilterNode Arg;

            public NegatedElement(FilterNode arg)
            {
                Arg = arg;
            }

            public override void Emit(XmlWriter xmlWriter)
            {
                xmlWriter.WriteStartElement("not");
                Arg.Emit(xmlWriter);
                xmlWriter.WriteEndElement();
            }
        }

        private class FilterElement : FilterNode
        {
            private static readonly string[] KNOWN_LHS_NAMES =
            {
                "test", "cat", "method", "class", "name", "namespace", "partition", "id"
            };

            public Token OpCode;
            public string LHS;
            public string RHS;

            public FilterElement(Token opCode, string lhs, string rhs)
            {
                Guard.ArgumentValid(opCode == EQ_OP1 || opCode == EQ_OP2 || opCode == MATCH_OP,
                    $"Invalid OpCode: {opCode}", nameof(opCode));

                LHS = lhs;
                OpCode = opCode;
                RHS = rhs;
            }

            public override void Emit(XmlWriter xmlWriter)
            {
                bool isProperty = !KNOWN_LHS_NAMES.Contains(LHS);

                if (isProperty)
                {
                    xmlWriter.WriteStartElement("prop");
                    xmlWriter.WriteAttributeString("name", LHS);
                }
                else
                    xmlWriter.WriteStartElement(LHS);

                if (OpCode == MATCH_OP)
                    xmlWriter.WriteAttributeString("re", "1");
                xmlWriter.WriteCData(RHS);
                xmlWriter.WriteEndElement();
            }
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

        private static TestSelectionParserException InvalidTokenError(Token token)
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
