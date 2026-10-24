using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Visitors;

internal static class SchemaCoalesceExpression
{
    internal static Type Resolve(CoalesceNode expression, IReadOnlyList<SchemaFieldNode> fields, SchemaRegistry? registry)
    {
        var left = BinarySchemaExpressionTypes.Resolve(expression.Left, fields, registry);
        var right = BinarySchemaExpressionTypes.Resolve(expression.Right, fields, registry);
        if (expression.Left is NullNode)
            return right;
        if (expression.Right is NullNode)
            return left;
        var leftValue = BinarySchemaExpressionTypes.Unwrap(left);
        var rightValue = BinarySchemaExpressionTypes.Unwrap(right);
        var common = leftValue;
        if (leftValue != rightValue)
            common = CallableOverloadPreference.FindCommonNumericType(leftValue, rightValue);
        if (common is null)
            throw new QuerySyntaxException("Operator ?? requires compatible lossless operand types.", expression.SpanOrEmpty(), DiagnosticCode.MQ4006_InvalidFieldConstraint);
        return BinarySchemaExpressionTypes.Lift(common,
            BinarySchemaExpressionTypes.MayBeNull(expression.Left, fields, registry) &&
            BinarySchemaExpressionTypes.MayBeNull(expression.Right, fields, registry));
    }

    internal static string Emit(CoalesceNode expression, IReadOnlyList<SchemaFieldNode> fields, SchemaRegistry registry,
        Func<Node, string> emit, Func<Type, string> typeName)
    {
        var type = Resolve(expression, fields, registry);
        var value = BinarySchemaExpressionTypes.Unwrap(type);
        var target = typeName(value) + (BinarySchemaExpressionTypes.IsNullable(type) ? "?" : string.Empty);
        if (expression.Left is NullNode)
            return $"(({target})({emit(expression.Right)}))";
        var nullableTarget = typeName(value) + (value.IsValueType ? "?" : string.Empty);
        return $"((({nullableTarget})({emit(expression.Left)})) ?? (({target})({emit(expression.Right)})))";
    }
}
