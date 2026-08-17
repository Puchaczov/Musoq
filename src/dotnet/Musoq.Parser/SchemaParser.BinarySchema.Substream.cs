using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private TypeAnnotationNode ComposeSubstreamType(string? fieldName)
    {
        Consume(TokenType.Substream);
        Consume(TokenType.LeftSquareBracket);
        var sizeExpr = ComposeSizeExpression();
        Consume(TokenType.RightSquareBracket);

        if (IsNegativeConstantSizeExpression(sizeExpr, out var negativeValue))
            throw new SyntaxException(
                $"substream size must be non-negative, but got {negativeValue}.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ4001_InvalidBinarySchemaField,
                sizeExpr.Span);

        if (Current.TokenType == TokenType.As)
            return ComposeStructuredSubstream(sizeExpr, fieldName);

        if (IsSubstreamModeWord("raw"))
        {
            Consume(Current.TokenType);
            return new SubstreamTypeNode(sizeExpr, SubstreamMode.Raw, null);
        }

        throw new SyntaxException(
            "Substream requires 'raw' or 'as <type>' after the size.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ4014_InvalidSubstreamModifier,
            Current.Span);
    }

    private SubstreamTypeNode ComposeStructuredSubstream(Node sizeExpr, string? fieldName)
    {
        Consume(TokenType.As);

        if (Current.TokenType is TokenType.Comma or TokenType.RBracket or TokenType.EndOfFile)
            throw new SyntaxException(
                "Substream 'as' requires a target type.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ4015_InvalidSubstreamTarget,
                Current.Span);

        var target = ComposeTypeAnnotation(fieldName);

        if (Current.TokenType == TokenType.Repeat)
        {
            if (fieldName == null)
                throw new SyntaxException(
                    "A repeat-until substream target requires a named field.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ4015_InvalidSubstreamTarget,
                    Current.Span);

            target = ComposeRepeatUntilType(target, fieldName);
        }

        var mode = ComposeSubstreamMode();
        return new SubstreamTypeNode(sizeExpr, mode, target);
    }

    private SubstreamMode ComposeSubstreamMode()
    {
        if (Current.TokenType is not (TokenType.Identifier or TokenType.Word))
            return SubstreamMode.Exact;

        var value = Current.Value;

        if (value.Equals("exact", StringComparison.OrdinalIgnoreCase))
        {
            Consume(Current.TokenType);
            return SubstreamMode.Exact;
        }

        if (value.Equals("lax", StringComparison.OrdinalIgnoreCase))
        {
            Consume(Current.TokenType);
            return SubstreamMode.Lax;
        }

        throw new SyntaxException(
            $"Invalid substream mode '{value}'. Use 'exact' or 'lax'.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ4014_InvalidSubstreamModifier,
            Current.Span);
    }

    private bool IsSubstreamModeWord(string word)
    {
        return Current.TokenType is TokenType.Identifier or TokenType.Word
               && Current.Value.Equals(word, StringComparison.OrdinalIgnoreCase);
    }
}
