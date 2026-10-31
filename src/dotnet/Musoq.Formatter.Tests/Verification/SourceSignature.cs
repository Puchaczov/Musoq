using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Syntax;

namespace Musoq.Formatter.Tests.Verification;

internal static class SourceSignature
{
    internal static IReadOnlyList<string> Of(string source)
    {
        var tokens = Parser.Parser.ParseSyntax(source).Tokens;
        var identifiers = tokens.Where(token => token.TokenType == Parser.Tokens.TokenType.Identifier).Select(token => token.Span).ToArray();
        var keywords = tokens.Where(t => t.Role == SyntaxTokenRole.Keyword)
            .Select(t => t.Span).ToArray();
        var result = new List<string>();
        var position = 0;
        while (position < source.Length)
        {
            if (char.IsWhiteSpace(source[position]))
            {
                position++;
                continue;
            }
            var start = position;
            var kind = "token:";
            if (Starts(source, position, "--"))
            {
                while (position < source.Length && source[position] is not ('\r' or '\n'))
                    position++;
                kind = "comment:";
            }
            else if (Starts(source, position, "/*"))
            {
                var closing = source.IndexOf("*/", position + 2, StringComparison.Ordinal);
                position = closing < 0 ? source.Length : closing + 2;
                kind = "comment:";
            }
            else if (source[position] == '[' && identifiers.Any(span => span.Start == position))
            {
                var closing = source.IndexOf(']', position + 1);
                position = closing < 0 ? source.Length : closing + 1;
                kind = "identifier:";
            }
            else if (source[position] is '\'' or '"' || IsRaw(source, position))
            {
                position = QuotedEnd(source, position);
                kind = "literal:";
            }
            else if (char.IsLetterOrDigit(source[position]) || source[position] == '_')
            {
                while (position < source.Length && (char.IsLetterOrDigit(source[position]) || source[position] == '_'))
                    position++;
            }
            else
                position++;
            var text = source[start..position];
            if (kind == "token:" && keywords.Any(s => s.Start <= start && start < s.End))
                text = text.ToUpperInvariant();
            result.Add(kind + text);
        }
        return result.AsReadOnly();
    }

    private static int QuotedEnd(string source, int start)
    {
        var raw = IsRaw(source, start);
        var quoteStart = start;
        if (raw)
            quoteStart++;
        var quote = source[quoteStart];
        var position = quoteStart + 1;
        while (position < source.Length)
        {
            if (raw && source[position] == quote && position + 1 < source.Length && source[position + 1] == quote)
            {
                position += 2;
                continue;
            }
            if (!raw && source[position] == '\\')
            {
                position = Math.Min(source.Length, position + 2);
                continue;
            }
            if (source[position] == quote)
                return position + 1;
            position++;
        }
        return source.Length;
    }

    private static bool IsRaw(string source, int position) => position + 1 < source.Length &&
        source[position] is 'r' or 'R' && source[position + 1] is '\'' or '"';

    private static bool Starts(string source, int position, string value) =>
        source.AsSpan(position).StartsWith(value, StringComparison.Ordinal);
}
