using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private bool TryBindEnumBinaryOperation(
        Node left,
        Node right,
        BinaryOperatorKind operatorKind,
        Node errorContext,
        out Node boundLeft,
        out Node boundRight)
    {
        boundLeft = left;
        boundRight = right;
        var hasLeftEnum = TryGetEnumExpressionType(left, out var leftEnum);
        var hasRightEnum = TryGetEnumExpressionType(right, out var rightEnum);
        if (!hasLeftEnum && !hasRightEnum)
            return false;

        if (operatorKind is not (BinaryOperatorKind.Equality or BinaryOperatorKind.Inequality))
        {
            var descriptor = hasLeftEnum ? leftEnum : rightEnum;
            ReportEnumSemanticError(
                DiagnosticCode.MQ3110_UnsupportedEnumOperator,
                $"Operator '{GetEnumOperatorDisplayName(errorContext)}' is not supported for enum type '{descriptor.DisplayName}'.",
                errorContext);
            return true;
        }

        if (hasLeftEnum && hasRightEnum)
        {
            if (!leftEnum.Equals(rightEnum))
                ReportEnumIdentityMismatch(leftEnum, rightEnum, errorContext);
            return true;
        }

        var enumType = hasLeftEnum ? leftEnum : rightEnum;
        var other = hasLeftEnum ? right : left;
        if (other is NullNode)
            return true;

        if (TryBindEnumMemberLiteral(enumType, other, out var memberLiteral))
        {
            if (hasLeftEnum)
                boundRight = memberLiteral;
            else
                boundLeft = memberLiteral;
            return true;
        }

        var otherType = other.ReturnType?.Name ?? "unknown";
        ReportEnumSemanticError(
            DiagnosticCode.MQ3110_UnsupportedEnumOperator,
            $"Enum type '{enumType.DisplayName}' can be compared only with the same enum identity, an exact quoted member name, or NULL; received '{otherType}'. Implicit numeric and string conversions are not supported.",
            other);
        return true;
    }

    private bool TryBindEnumMemberLiteral(
        EnumTypeDescriptor enumType,
        Node candidate,
        out Node boundLiteral)
    {
        if (candidate is not ConstantValueNode { ObjValue: string memberName })
        {
            boundLiteral = candidate;
            return false;
        }

        if (!enumType.TryGetValue(memberName, out var value))
        {
            ReportEnumSemanticError(
                DiagnosticCode.MQ3108_UnknownEnumMember,
                $"Enum member '{memberName}' is not defined by enum type '{enumType.DisplayName}'. Member names are case-sensitive.",
                candidate);
            boundLiteral = candidate;
            return true;
        }

        boundLiteral = CreateEnumCarrierLiteral(value, candidate.Span);
        MarkEnumExpression(boundLiteral, enumType);
        return true;
    }

    private bool TryBindEnumCollectionPredicate(
        Node expression,
        ArgsListNode items,
        Node errorContext,
        out ArgsListNode boundItems)
    {
        var hasExpressionEnum = TryGetEnumExpressionType(expression, out var enumType);
        if (!hasExpressionEnum)
        {
            foreach (var item in items.Args)
            {
                if (!TryGetEnumExpressionType(item, out var itemEnum))
                    continue;

                ReportEnumSemanticError(
                    DiagnosticCode.MQ3110_UnsupportedEnumOperator,
                    $"Enum type '{itemEnum.DisplayName}' cannot be compared with a non-enum IN expression.",
                    errorContext);
                boundItems = items;
                return true;
            }

            boundItems = items;
            return false;
        }

        var bound = new Node[items.Args.Length];
        for (var index = 0; index < items.Args.Length; index++)
        {
            var item = items.Args[index];
            if (item is NullNode)
            {
                bound[index] = item;
                continue;
            }

            if (TryGetEnumExpressionType(item, out var itemEnum))
            {
                if (!enumType.Equals(itemEnum))
                    ReportEnumIdentityMismatch(enumType, itemEnum, item);
                bound[index] = item;
                continue;
            }

            if (TryBindEnumMemberLiteral(enumType, item, out var memberLiteral))
            {
                bound[index] = memberLiteral;
                continue;
            }

            ReportEnumSemanticError(
                DiagnosticCode.MQ3110_UnsupportedEnumOperator,
                $"IN items for enum type '{enumType.DisplayName}' must be exact quoted member names, NULL, or expressions of the same enum identity.",
                item);
            bound[index] = item;
        }

        boundItems = new ArgsListNode(bound, items.ArgumentNames, items.Span);
        return true;
    }

    private static IntegerNode CreateEnumCarrierLiteral(EnumScalarValue value, TextSpan span)
    {
        object primitive = value.Kind switch
        {
            EnumUnderlyingKind.Byte => value.AsByte(),
            EnumUnderlyingKind.SByte => value.AsSByte(),
            EnumUnderlyingKind.Int16 => value.AsInt16(),
            EnumUnderlyingKind.UInt16 => value.AsUInt16(),
            EnumUnderlyingKind.Int32 => value.AsInt32(),
            EnumUnderlyingKind.UInt32 => value.AsUInt32(),
            EnumUnderlyingKind.Int64 => value.AsInt64(),
            EnumUnderlyingKind.UInt64 => value.AsUInt64(),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value.Kind, "Unknown enum backing kind.")
        };

        return new IntegerNode(primitive, span);
    }

    private void ReportEnumIdentityMismatch(
        EnumTypeDescriptor left,
        EnumTypeDescriptor right,
        Node errorContext)
    {
        ReportEnumSemanticError(
            DiagnosticCode.MQ3109_EnumIdentityMismatch,
            $"Enum types '{left.DisplayName}' and '{right.DisplayName}' cannot be combined. Enums are nominal even when their backing types match.",
            errorContext);
    }

    private bool TryBindEnumCaseResults(
        List<(Node When, Node Then)> whenThenPairs,
        Node elseNode,
        out List<(Node When, Node Then)> boundWhenThenPairs,
        out Node boundElseNode,
        out EnumTypeDescriptor descriptor)
    {
        descriptor = null!;
        foreach (var pair in whenThenPairs)
        {
            if (TryGetEnumExpressionType(pair.Then, out descriptor))
                break;
        }

        if (descriptor == null && !TryGetEnumExpressionType(elseNode, out descriptor))
        {
            boundWhenThenPairs = whenThenPairs;
            boundElseNode = elseNode;
            return false;
        }

        boundWhenThenPairs = new List<(Node When, Node Then)>(whenThenPairs.Count);
        foreach (var pair in whenThenPairs)
        {
            boundWhenThenPairs.Add((
                pair.When,
                BindEnumCaseResult(pair.Then, descriptor)));
        }

        boundElseNode = BindEnumCaseResult(elseNode, descriptor);
        return true;
    }

    private Node BindEnumCaseResult(Node result, EnumTypeDescriptor descriptor)
    {
        var expression = result switch
        {
            ThenNode then => then.Expression,
            ElseNode @else => @else.Expression,
            _ => result
        };

        if (expression is NullNode)
            return result;

        if (TryGetEnumExpressionType(expression, out var candidateDescriptor))
        {
            if (!descriptor.Equals(candidateDescriptor))
                ReportEnumIdentityMismatch(descriptor, candidateDescriptor, expression);
            return result;
        }

        if (TryBindEnumMemberLiteral(descriptor, expression, out var memberLiteral))
            return WrapEnumCaseResult(result, memberLiteral);

        ReportEnumSemanticError(
            DiagnosticCode.MQ3110_UnsupportedEnumOperator,
            $"CASE results for enum type '{descriptor.DisplayName}' must be NULL, exact quoted member names, or expressions of the same enum identity.",
            expression);
        return result;
    }

    private static Node WrapEnumCaseResult(Node original, Node expression)
    {
        return original switch
        {
            ThenNode => new ThenNode(expression),
            ElseNode => new ElseNode(expression),
            _ => expression
        };
    }

    private bool TryRejectUnsupportedEnumOperator(string operatorName, Node errorContext, params Node[] operands)
    {
        foreach (var operand in operands)
        {
            if (!TryGetEnumExpressionType(operand, out var enumType))
                continue;

            ReportEnumSemanticError(
                DiagnosticCode.MQ3110_UnsupportedEnumOperator,
                $"Operator '{operatorName}' is not supported for enum type '{enumType.DisplayName}'.",
                errorContext);
            return true;
        }

        return false;
    }

    private void ValidateWindowEnumArguments(string normalizedName, Node[] arguments, Node context)
    {
        if (normalizedName is "COUNT" or "COUNTDISTINCT")
            return;

        foreach (var argument in arguments)
        {
            if (!TryGetEnumExpressionType(argument, out var enumType))
                continue;

            ReportEnumSemanticError(
                DiagnosticCode.MQ3110_UnsupportedEnumOperator,
                $"Window function '{normalizedName}' is not supported for enum type '{enumType.DisplayName}'. Enum values may be used as PARTITION BY keys, and COUNT is supported.",
                context);
            return;
        }
    }

    private void ReportEnumSemanticError(DiagnosticCode code, string message, Node node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(code, message, node);
            return;
        }

        throw new VisitorException(
            VisitorName,
            "BindEnumExpression",
            message,
            code,
            node.SpanOrEmpty());
    }

    private static string GetEnumOperatorDisplayName(Node node)
    {
        return node switch
        {
            EqualityNode => "=",
            DiffNode => "<>",
            IsDistinctFromNode { IsNegated: true } => "IS NOT DISTINCT FROM",
            IsDistinctFromNode => "IS DISTINCT FROM",
            GreaterNode => ">",
            GreaterOrEqualNode => ">=",
            LessNode => "<",
            LessOrEqualNode => "<=",
            AddNode => "+",
            HyphenNode => "-",
            StarNode => "*",
            FSlashNode => "/",
            ModuloNode => "%",
            BitwiseAndNode => "&",
            BitwiseOrNode => "|",
            BitwiseXorNode => "^",
            LeftShiftNode => "<<",
            RightShiftNode => ">>",
            _ => node.GetType().Name
        };
    }
}
