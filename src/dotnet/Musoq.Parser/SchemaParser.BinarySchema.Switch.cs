using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private TypeAnnotationNode ComposeBinarySwitchType()
    {
        var switchToken = ConsumeAndGetToken(TokenType.Switch);

        var selectorSpan = Current.Span;
        var selector = ComposeIdentifierOrWord();

        Consume(TokenType.LBracket);

        var cases = new List<BinarySwitchCaseNode>();

        while (Current.TokenType != TokenType.RBracket && Current.TokenType != TokenType.EndOfFile)
        {
            cases.Add(ComposeBinarySwitchCase());

            if (Current.TokenType == TokenType.Comma)
                Consume(TokenType.Comma);
        }

        var closingToken = ConsumeAndGetToken(TokenType.RBracket);

        if (cases.Count == 0)
            throw new SyntaxException(
                "A binary switch must declare at least one case.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ4013_InvalidSwitchCaseLabel,
                closingToken.Span);

        ValidateBinarySwitchCases(cases);

        return (BinarySwitchTypeNode)new BinarySwitchTypeNode(selector, cases.ToArray(), selectorSpan)
            .WithSpan(switchToken.Span.Through(closingToken.Span));
    }

    private BinarySwitchCaseNode ComposeBinarySwitchCase()
    {
        var caseLabelStart = Current.Span;
        var caseValue = ComposeBinarySwitchCaseLabel();
        var caseLabelSpan = caseValue?.Span is { IsEmpty: false } valueSpan
            ? valueSpan
            : caseLabelStart;

        Consume(TokenType.FatArrow);

        var branchAliasStart = Current.Span;
        var branchAlias = ComposeIdentifierOrWord();
        var branchAliasSpan = branchAliasStart;
        Consume(TokenType.Colon);

        var branchTypeStart = Current.Span;
        var branchType = ComposeTypeAnnotation();
        var branchTypeSpan = branchType.HasSpan ? branchType.Span : branchTypeStart;

        return new BinarySwitchCaseNode(
            caseValue,
            branchAlias,
            branchType,
            caseLabelSpan,
            branchAliasSpan,
            branchTypeSpan);
    }

    private Node? ComposeBinarySwitchCaseLabel()
    {
        if (Current.TokenType == TokenType.Underscore ||
            ((Current.TokenType is TokenType.Word or TokenType.Identifier) && Current.Value == "_"))
        {
            Consume(Current.TokenType);
            return null;
        }

        if (Current.TokenType is not (TokenType.Integer or TokenType.HexadecimalInteger or
            TokenType.BinaryInteger or TokenType.OctalInteger or TokenType.Decimal or
            TokenType.StringLiteral or TokenType.True or TokenType.False or TokenType.Hyphen))
            throw InvalidSwitchCaseLabel(Current.Span);

        var label = ComposePrimaryExpression();

        if (!IsSwitchCaseLabel(label))
            throw InvalidSwitchCaseLabel(label.Span);

        return label;
    }

}
