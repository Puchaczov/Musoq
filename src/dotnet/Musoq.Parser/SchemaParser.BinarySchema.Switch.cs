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
        Consume(TokenType.Switch);

        var selector = ComposeIdentifierOrWord();

        Consume(TokenType.LBracket);

        var cases = new List<BinarySwitchCaseNode>();

        while (Current.TokenType != TokenType.RBracket && Current.TokenType != TokenType.EndOfFile)
        {
            cases.Add(ComposeBinarySwitchCase());

            if (Current.TokenType == TokenType.Comma)
                Consume(TokenType.Comma);
        }

        Consume(TokenType.RBracket);

        ValidateBinarySwitchCases(cases);

        return new BinarySwitchTypeNode(selector, cases.ToArray());
    }

    private BinarySwitchCaseNode ComposeBinarySwitchCase()
    {
        var caseValue = ComposeBinarySwitchCaseLabel();

        Consume(TokenType.FatArrow);

        var branchAlias = ComposeIdentifierOrWord();
        Consume(TokenType.Colon);
        var branchType = ComposeTypeAnnotation();

        return new BinarySwitchCaseNode(caseValue, branchAlias, branchType);
    }

    private Node? ComposeBinarySwitchCaseLabel()
    {
        if (Current.TokenType is TokenType.Word or TokenType.Identifier && Current.Value == "_")
        {
            Consume(Current.TokenType);
            return null;
        }

        var label = ComposePrimaryExpression();

        if (label is not ConstantValueNode)
            throw new SyntaxException(
                "Switch case label must be a constant scalar literal.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ4013_InvalidSwitchCaseLabel,
                label.Span);

        return label;
    }

    private void ValidateBinarySwitchCases(IReadOnlyList<BinarySwitchCaseNode> cases)
    {
        var seenAliases = new HashSet<string>();

        foreach (var switchCase in cases)
            if (!seenAliases.Add(switchCase.BranchAlias))
                throw new SyntaxException(
                    $"Duplicate switch branch alias '{switchCase.BranchAlias}'.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ4012_DuplicateSwitchBranchAlias,
                    switchCase.BranchType.Span);

        for (var i = 0; i < cases.Count; i++)
            if (cases[i].IsDefault && i != cases.Count - 1)
                throw new SyntaxException(
                    "Switch default case '_' must be the last case.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ4013_InvalidSwitchCaseLabel,
                    cases[i].BranchType.Span);
    }

    private void ValidateBinarySwitchSelectors(IReadOnlyList<SchemaFieldNode> fields, bool hasExtends)
    {
        var precedingNames = new HashSet<string>();

        foreach (var field in fields)
        {
            if (field is FieldDefinitionNode { TypeAnnotation: BinarySwitchTypeNode switchType }
                && !hasExtends
                && !precedingNames.Contains(switchType.Selector))
                throw new SyntaxException(
                    $"Switch selector '{switchType.Selector}' must reference a field declared before the switch field.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ4011_SwitchSelectorNotPreviousField,
                    switchType.Span);

            precedingNames.Add(field.Name);
        }
    }
}
