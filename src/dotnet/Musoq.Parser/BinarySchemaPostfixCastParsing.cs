using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private Node ComposeSchemaPostfixCast(Node operand)
    {
        Consume(TokenType.DoubleColon);
        var target = Current;
        if (target.TokenType != TokenType.Identifier && !AllowedKeywordTokenTypes.Contains(target.TokenType))
            throw new SyntaxException("Expected cast target type name after '::'.",
                _lexer.AlreadyResolvedQueryPart,
                Diagnostics.DiagnosticCode.MQ2001_UnexpectedToken, target.Span);
        Consume(target.TokenType);
        var targetName = target.Value;
        if (Current.TokenType == TokenType.QuestionMark)
        {
            targetName += "?";
            Consume(TokenType.QuestionMark);
        }
        return new CastNode(operand, targetName).WithSpan(operand.Span.Through(target.Span));
    }
}
