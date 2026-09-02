using System.Collections.Generic;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

public sealed partial class Lexer
{
    private static List<Token> LexInnerExpression(string content, int basePosition)
    {
        var tokens = new List<Token>();
        var pos = 0;

        while (pos < content.Length)
        {
            while (pos < content.Length && char.IsWhiteSpace(content[pos])) pos++;
            if (pos >= content.Length) break;

            var ch = content[pos];
            var spanStart = basePosition + pos;

            switch (ch)
            {
                case '+':
                    tokens.Add(new PlusToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                case '-':
                    tokens.Add(new HyphenToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                case '*':
                    tokens.Add(new StarToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                case '/':
                    tokens.Add(new FSlashToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                case '%':
                    tokens.Add(new ModuloToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                case '(':
                    tokens.Add(new LeftParenthesisToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                case ')':
                    tokens.Add(new RightParenthesisToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                default:
                    if (char.IsDigit(ch))
                    {
                        var start = pos;
                        if (ch == '0' && pos + 1 < content.Length && content[pos + 1] is ('x' or 'X'))
                        {
                            pos += 2;
                            while (pos < content.Length && IsHexDigit(content[pos])) pos++;
                            tokens.Add(new HexIntegerToken(content[start..pos], new TextSpan(spanStart, pos - start)));
                        }
                        else if (ch == '0' && pos + 1 < content.Length && content[pos + 1] is ('b' or 'B'))
                        {
                            pos += 2;
                            while (pos < content.Length && content[pos] is ('0' or '1')) pos++;
                            tokens.Add(new BinaryIntegerToken(content[start..pos], new TextSpan(spanStart, pos - start)));
                        }
                        else if (ch == '0' && pos + 1 < content.Length && content[pos + 1] is ('o' or 'O'))
                        {
                            pos += 2;
                            while (pos < content.Length && content[pos] is >= '0' and <= '7') pos++;
                            tokens.Add(new OctalIntegerToken(content[start..pos], new TextSpan(spanStart, pos - start)));
                        }
                        else
                        {
                            while (pos < content.Length && char.IsDigit(content[pos])) pos++;
                            if (pos < content.Length && content[pos] == '.' &&
                                pos + 1 < content.Length && char.IsDigit(content[pos + 1]))
                            {
                                pos++;
                                while (pos < content.Length && char.IsDigit(content[pos])) pos++;
                                tokens.Add(new DecimalToken(content[start..pos], new TextSpan(spanStart, pos - start)));
                            }
                            else
                            {
                                tokens.Add(new IntegerToken(content[start..pos], new TextSpan(spanStart, pos - start), "i"));
                            }
                        }
                    }
                    else if (char.IsLetter(ch) || ch == '_')
                    {
                        var start = pos;
                        while (pos < content.Length &&
                               (char.IsLetterOrDigit(content[pos]) || content[pos] == '_')) pos++;
                        tokens.Add(new WordToken(content[start..pos], new TextSpan(spanStart, pos - start)));
                    }
                    else
                    {
                        pos++;
                    }

                    break;
            }
        }

        return tokens;
    }
}
