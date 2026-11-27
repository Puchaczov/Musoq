using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private void ValidateBinarySwitchCases(IReadOnlyList<BinarySwitchCaseNode> cases)
    {
        var seenAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var switchCase in cases)
            if (!seenAliases.Add(switchCase.BranchAlias))
                throw new SyntaxException(
                    $"Duplicate switch branch alias '{switchCase.BranchAlias}'.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ4012_DuplicateSwitchBranchAlias,
                    GetBranchAliasSpan(switchCase)
                );
            else if (string.Equals(switchCase.BranchAlias, "Case", StringComparison.OrdinalIgnoreCase))
                throw new SyntaxException(
                    "Switch branch alias 'Case' is reserved for the selected-branch discriminator.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ4012_DuplicateSwitchBranchAlias,
                    GetBranchAliasSpan(switchCase)
                );

        foreach (var switchCase in cases)
            if (switchCase.BranchType is AlignmentNode)
                throw new SyntaxException(
                    $"Switch branch '{switchCase.BranchAlias}' uses unsupported binary type '{switchCase.BranchType}'.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ4013_InvalidSwitchCaseLabel,
                    GetBranchTypeSpan(switchCase)
                );

        for (var i = 0; i < cases.Count; i++)
            if (cases[i].IsDefault && i != cases.Count - 1)
                throw new SyntaxException(
                    "Switch default case '_' must be the last case.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ4013_InvalidSwitchCaseLabel,
                    GetCaseLabelSpan(cases[i])
                );
    }

    private void ValidateBinarySwitchSelectors(IReadOnlyList<SchemaFieldNode> fields, bool hasExtends)
    {
        var precedingTypes = new Dictionary<string, Type?>(StringComparer.Ordinal);

        foreach (var field in fields)
        {
            if (
                field is FieldDefinitionNode { TypeAnnotation: BinarySwitchTypeNode switchType }
                && !hasExtends
                && !precedingTypes.ContainsKey(switchType.SelectorRoot)
                && !(
                    switchType.SelectorExpression is not IdentifierNode
                    && switchType.SelectorRoot.Equals("parent", StringComparison.OrdinalIgnoreCase)
                )
            )
                throw new SyntaxException(
                    $"Switch selector '{switchType.Selector}' must reference a field declared before the switch field.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ4011_SwitchSelectorNotPreviousField,
                    GetSelectorSpan(switchType)
                );

            if (
                field is FieldDefinitionNode { TypeAnnotation: BinarySwitchTypeNode localSwitch }
                && localSwitch.SelectorExpression is IdentifierNode
                && precedingTypes.TryGetValue(localSwitch.Selector, out var selectorType)
                && selectorType is not null
            )
                ValidateSwitchCaseLabels(localSwitch, selectorType);

            // A computed reference receives its type during schema binding.
            precedingTypes[field.Name] =
                field is ComputedFieldNode && field.ReturnType == typeof(object) ? null : field.ReturnType;
        }
    }

    private void ValidateSwitchCaseLabels(BinarySwitchTypeNode switchType, Type? selectorType)
    {
        foreach (var switchCase in switchType.Cases)
            if (
                !switchCase.IsDefault
                && !BinarySchemaScalarLiteral.IsCompatibleSwitchLabel(selectorType, switchCase.CaseValue!)
            )
                throw new SyntaxException(
                    $"Switch case label '{switchCase.CaseValue}' is not compatible with selector '{switchType.Selector}'.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ4013_InvalidSwitchCaseLabel,
                    GetCaseLabelSpan(switchCase)
                );
    }

    private static bool IsSwitchCaseLabel(Node node)
    {
        return node is BooleanNode or StringNode or WordNode || IsNumericLiteral(node);
    }

    private static TextSpan GetSelectorSpan(BinarySwitchTypeNode switchType)
    {
        return switchType.SelectorSpan.IsEmpty ? switchType.Span : switchType.SelectorSpan;
    }

    private static TextSpan GetCaseLabelSpan(BinarySwitchCaseNode switchCase)
    {
        return switchCase.CaseLabelSpan.IsEmpty
            ? switchCase.CaseValue?.Span ?? switchCase.BranchTypeSpan
            : switchCase.CaseLabelSpan;
    }

    private static TextSpan GetBranchAliasSpan(BinarySwitchCaseNode switchCase)
    {
        return switchCase.BranchAliasSpan.IsEmpty ? switchCase.BranchTypeSpan : switchCase.BranchAliasSpan;
    }

    private static TextSpan GetBranchTypeSpan(BinarySwitchCaseNode switchCase)
    {
        return switchCase.BranchTypeSpan.IsEmpty ? switchCase.BranchType.Span : switchCase.BranchTypeSpan;
    }

    private SyntaxException InvalidSwitchCaseLabel(TextSpan span)
    {
        return new SyntaxException(
            "Switch case label must be a constant scalar literal.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ4013_InvalidSwitchCaseLabel,
            span
        );
    }
}
