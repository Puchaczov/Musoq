using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private (string? TableName, string? ProfileName) ComposeCoupleOptions()
    {
        string? tableName = null;
        string? profileName = null;

        while (true)
        {
            if (Current.TokenType == TokenType.Table)
            {
                if (tableName != null)
                    throw new SyntaxException(
                        "Duplicate table option in couple statement.",
                        _lexer.AlreadyResolvedQueryPart,
                        DiagnosticCode.MQ2001_UnexpectedToken,
                        Current.Span);

                Consume(TokenType.Table);
                tableName = ComposeCoupleOptionName("table");
            }
            else if (IsSettingsOption())
            {
                if (profileName != null)
                    throw new SyntaxException(
                        "Duplicate settings option in couple statement.",
                        _lexer.AlreadyResolvedQueryPart,
                        DiagnosticCode.MQ2001_UnexpectedToken,
                        Current.Span);

                Consume(Current.TokenType);
                profileName = ComposeCoupleOptionName("settings");
            }
            else
                throw new SyntaxException(
                    "Expected table or settings option in couple statement.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2001_UnexpectedToken,
                    Current.Span);

            if (Current.TokenType != TokenType.And)
                break;

            Consume(TokenType.And);
        }

        return (tableName, profileName);
    }

    private string ComposeCoupleOptionName(string optionName)
    {
        if (Current.TokenType != TokenType.Identifier)
            throw new SyntaxException(
                $"Expected {optionName} name in couple statement.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);

        var name = Current.Value;
        Consume(Current.TokenType);
        return name;
    }

    private bool IsSettingsOption()
    {
        return string.Equals(Current.Value, "settings", StringComparison.OrdinalIgnoreCase);
    }
}
