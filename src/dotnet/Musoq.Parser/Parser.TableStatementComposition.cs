using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private CreateTableNode ComposeTable()
    {
        var tableToken = ConsumeAndGetToken(Current.TokenType);
        var tableName = Current.Value;
        Consume(TokenType.Identifier);
        Consume(TokenType.LBracket);

        var columns = ComposeTableColumns();

        var closingToken = ConsumeAndGetToken(Current.TokenType);

        return (CreateTableNode)new CreateTableNode(tableName, columns)
            .WithSpan(tableToken.Span.Through(closingToken.Span));
    }

    private (string TypeName, TextSpan Span) ComposeTableColumnTypeName()
    {
        var typeToken = ConsumeAndGetToken(TokenType.Identifier);
        var typeName = typeToken.Value;
        var typeEndSpan = typeToken.Span;

        while (Current.TokenType == TokenType.Dot)
        {
            Consume(TokenType.Dot);
            var segmentToken = ConsumeTableColumnTypeNameSegment();
            typeName += $".{segmentToken.Value}";
            typeEndSpan = segmentToken.Span;
        }

        return (typeName, typeEndSpan);
    }

    private Token ConsumeTableColumnTypeNameSegment()
    {
        if (Current.TokenType is TokenType.Identifier or TokenType.Property)
            return ConsumeAndGetToken(Current.TokenType);

        return ConsumeAndGetToken(TokenType.Identifier);
    }
}
