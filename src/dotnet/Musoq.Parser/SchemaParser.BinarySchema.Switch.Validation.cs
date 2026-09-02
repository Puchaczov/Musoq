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
                    GetBranchAliasSpan(switchCase));
            else if (string.Equals(switchCase.BranchAlias, "Case", StringComparison.OrdinalIgnoreCase))
                throw new SyntaxException(
                    "Switch branch alias 'Case' is reserved for the selected-branch discriminator.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ4012_DuplicateSwitchBranchAlias,
                    GetBranchAliasSpan(switchCase));

        foreach (var switchCase in cases)
            if (switchCase.BranchType is not (PrimitiveTypeNode or ByteArrayTypeNode or SchemaReferenceTypeNode))
                throw new SyntaxException(
                    $"Switch branch '{switchCase.BranchAlias}' uses unsupported binary type '{switchCase.BranchType}'.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ4013_InvalidSwitchCaseLabel,
                    GetBranchTypeSpan(switchCase));

        for (var i = 0; i < cases.Count; i++)
            if (cases[i].IsDefault && i != cases.Count - 1)
                throw new SyntaxException(
                    "Switch default case '_' must be the last case.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ4013_InvalidSwitchCaseLabel,
                    GetCaseLabelSpan(cases[i]));
    }

    private void ValidateBinarySwitchSelectors(IReadOnlyList<SchemaFieldNode> fields, bool hasExtends)
    {
        var precedingTypes = new Dictionary<string, Type?>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields)
        {
            if (field is FieldDefinitionNode { TypeAnnotation: BinarySwitchTypeNode switchType }
                && !hasExtends
                && !precedingTypes.ContainsKey(switchType.Selector))
                throw new SyntaxException(
                    $"Switch selector '{switchType.Selector}' must reference a field declared before the switch field.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ4011_SwitchSelectorNotPreviousField,
                    GetSelectorSpan(switchType));

            if (field is FieldDefinitionNode { TypeAnnotation: BinarySwitchTypeNode localSwitch } &&
                precedingTypes.TryGetValue(localSwitch.Selector, out var selectorType))
                ValidateSwitchCaseLabels(localSwitch, selectorType);

            precedingTypes[field.Name] = field.ReturnType;
        }
    }

    private void ValidateSwitchCaseLabels(BinarySwitchTypeNode switchType, Type? selectorType)
    {
        foreach (var switchCase in switchType.Cases)
            if (!switchCase.IsDefault && !IsSwitchCaseLabelCompatible(selectorType, switchCase.CaseValue!))
                throw new SyntaxException(
                    $"Switch case label '{switchCase.CaseValue}' is not compatible with selector '{switchType.Selector}'.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ4013_InvalidSwitchCaseLabel,
                    GetCaseLabelSpan(switchCase));
    }

    private static bool IsSwitchCaseLabel(Node node)
    {
        return node is BooleanNode or StringNode or WordNode || IsNumericLiteral(node);
    }

    private static bool IsSwitchCaseLabelCompatible(Type? selectorType, Node label)
    {
        var targetType = selectorType is null ? null : Nullable.GetUnderlyingType(selectorType) ?? selectorType;
        if (targetType == typeof(bool))
            return label is BooleanNode;

        if (targetType == typeof(string))
            return label is StringNode or WordNode;

        if (!IsNumericLiteral(label) || !TryGetNumericValue(label, out var numeric))
            return false;

        if (targetType == typeof(float) || targetType == typeof(double) || targetType == typeof(decimal))
            return true;

        if (targetType == typeof(byte))
            return IsIntegralInRange(numeric, byte.MinValue, byte.MaxValue);
        if (targetType == typeof(sbyte))
            return IsIntegralInRange(numeric, sbyte.MinValue, sbyte.MaxValue);
        if (targetType == typeof(short))
            return IsIntegralInRange(numeric, short.MinValue, short.MaxValue);
        if (targetType == typeof(ushort))
            return IsIntegralInRange(numeric, ushort.MinValue, ushort.MaxValue);
        if (targetType == typeof(int))
            return IsIntegralInRange(numeric, int.MinValue, int.MaxValue);
        if (targetType == typeof(uint))
            return IsIntegralInRange(numeric, uint.MinValue, uint.MaxValue);
        if (targetType == typeof(long))
            return IsIntegralInRange(numeric, long.MinValue, long.MaxValue);
        if (targetType == typeof(ulong))
            return IsIntegralInRange(numeric, ulong.MinValue, ulong.MaxValue);

        return false;
    }

    private static bool IsIntegralInRange(decimal value, decimal minimum, decimal maximum)
    {
        return decimal.Truncate(value) == value && value >= minimum && value <= maximum;
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
        return switchCase.BranchAliasSpan.IsEmpty
            ? switchCase.BranchTypeSpan
            : switchCase.BranchAliasSpan;
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
            span);
    }
}
