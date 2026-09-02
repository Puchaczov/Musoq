using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private bool TryBindEnumIntrinsic(AccessMethodNode node)
    {
        if (!TryResolveEnumIntrinsicKind(node.Name, out var kind))
            return false;

        var arguments = GetAndValidateArgs(node);
        DiscardUnsupportedIntrinsicFilter(node);

        var validCount = kind is EnumIntrinsicKind.HasAnyFlags or EnumIntrinsicKind.HasAllFlags
            ? arguments.Args.Length >= 1
            : arguments.Args.Length == 1;
        if (!validCount || node.IsDistinct || node.HasFilter || !string.IsNullOrWhiteSpace(node.Alias))
        {
            ReportEnumSemanticError(
                DiagnosticCode.MQ3111_InvalidEnumHelper,
                $"Enum helper '{node.Name}' must be unqualified and use its documented argument count without DISTINCT or FILTER.",
                node);
            PushEnumIntrinsicRecovery(kind, node);
            return true;
        }

        var operand = arguments.Args[0];
        if (!TryGetEnumExpressionType(operand, out var descriptor))
        {
            ReportEnumSemanticError(
                DiagnosticCode.MQ3111_InvalidEnumHelper,
                $"Enum helper '{node.Name}' requires an enum expression as its first argument.",
                operand);
            PushEnumIntrinsicRecovery(kind, node);
            return true;
        }

        Node[] boundArguments;
        if (kind is EnumIntrinsicKind.HasAnyFlags or EnumIntrinsicKind.HasAllFlags)
        {
            if (!descriptor.IsFlags)
            {
                ReportEnumSemanticError(
                    DiagnosticCode.MQ3111_InvalidEnumHelper,
                    $"Enum helper '{node.Name}' requires a flags enum; '{descriptor.DisplayName}' is not declared as flags.",
                    operand);
                PushEnumIntrinsicRecovery(kind, node);
                return true;
            }

            var mask = BindEnumFlagsMask(node, arguments, descriptor);
            boundArguments = [operand, CreateEnumCarrierLiteral(mask, node.Span)];
        }
        else
        {
            boundArguments = [operand];
        }

        var marker = EnumIntrinsicMethodFacts.Bind(kind, operand.ReturnType ??
            throw new InvalidOperationException($"Enum helper '{node.Name}' operand has no inferred type."));
        var result = (AccessMethodNode)new AccessMethodNode(
                node.FunctionToken,
                new ArgsListNode(boundArguments),
                null,
                false,
                marker,
                string.Empty,
                node.Span)
            .WithFullSpan(node.FullSpan);
        PushSemanticNode(result);
        return true;
    }

    private bool TryRejectUnsupportedEnumMethod(AccessMethodNode node)
    {
        if (PeekSemanticNode("Inspect enum method arguments") is not ArgsListNode arguments ||
            !arguments.Args.Any(argument => TryGetEnumExpressionType(argument, out _)))
            return false;

        if (node.Name.Equals("Count", StringComparison.OrdinalIgnoreCase) ||
            node.Name.Equals("CountDistinct", StringComparison.OrdinalIgnoreCase))
            return false;

        _ = GetAndValidateArgs(node);
        if (node.FilterExpression != null)
            _ = PopSemanticNode("Discard unsupported enum method filter");
        var descriptor = arguments.Args
            .Select(argument => TryGetEnumExpressionType(argument, out var candidate) ? candidate : null)
            .First(static candidate => candidate != null)!;
        ReportEnumSemanticError(
            DiagnosticCode.MQ3110_UnsupportedEnumOperator,
            $"Method '{node.Name}' is not supported for enum type '{descriptor.DisplayName}'. Use the explicit enum helpers; Count and CountDistinct are the only aggregate consumers in v1.",
            node);
        PushSemanticNode(new NullNode(typeof(object), node.Span));
        return true;
    }

    private EnumScalarValue BindEnumFlagsMask(
        AccessMethodNode node,
        ArgsListNode arguments,
        EnumTypeDescriptor descriptor)
    {
        var rawMask = 0UL;
        for (var index = 1; index < arguments.Args.Length; index++)
        {
            var argument = arguments.Args[index];
            if (argument is not ConstantValueNode { ObjValue: string memberName })
            {
                ReportEnumSemanticError(
                    DiagnosticCode.MQ3111_InvalidEnumHelper,
                    $"Enum helper '{node.Name}' accepts only exact quoted member names after its enum operand.",
                    argument);
                continue;
            }

            if (!descriptor.TryGetValue(memberName, out var value))
            {
                ReportEnumSemanticError(
                    DiagnosticCode.MQ3108_UnknownEnumMember,
                    $"Enum member '{memberName}' is not defined by enum type '{descriptor.DisplayName}'. Member names are case-sensitive.",
                    argument);
                continue;
            }

            rawMask |= value.RawValue;
        }

        return EnumScalarValue.FromRaw(descriptor.UnderlyingKind, rawMask);
    }

    private void DiscardUnsupportedIntrinsicFilter(AccessMethodNode node)
    {
        if (node.FilterExpression != null)
            _ = PopSemanticNode("Discard enum intrinsic filter");
    }

    private void PushEnumIntrinsicRecovery(EnumIntrinsicKind kind, AccessMethodNode node)
    {
        PushSemanticNode(kind switch
        {
            EnumIntrinsicKind.EnumName => new NullNode(typeof(string), node.Span),
            EnumIntrinsicKind.EnumValue => new NullNode(typeof(object), node.Span),
            _ => new BooleanNode(false, node.Span)
        });
    }

    private static bool TryResolveEnumIntrinsicKind(string name, out EnumIntrinsicKind kind)
    {
        foreach (var candidate in Enum.GetValues<EnumIntrinsicKind>())
        {
            if (!name.Equals(candidate.ToString(), StringComparison.OrdinalIgnoreCase))
                continue;

            kind = candidate;
            return true;
        }

        kind = default;
        return false;
    }
}
