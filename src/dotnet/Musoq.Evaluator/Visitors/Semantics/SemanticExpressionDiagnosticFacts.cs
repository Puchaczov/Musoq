using System.Diagnostics.CodeAnalysis;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class SemanticExpressionDiagnosticFacts
{
    public static string CreateComparisonTypeMismatchMessage(Node left, Node right, Type leftType, Type rightType)
    {
        var leftHasParameter = TryFindFirstScriptParameterReference(left, out var leftParameter);
        var rightHasParameter = TryFindFirstScriptParameterReference(right, out var rightParameter);

        if (!leftHasParameter && !rightHasParameter)
            return $"Type mismatch: cannot compare '{leftType.Name}' with '{rightType.Name}'.";

        return $"Type mismatch: cannot compare {DescribeComparedExpression(leftParameter, leftType)} with " +
               $"{DescribeComparedExpression(rightParameter, rightType)}. Script parameters use their declared types; " +
               "use an explicit conversion if needed.";
    }

    public static string CreateBooleanContextTypeMismatchMessage(Node expression, Type expressionType, string context)
    {
        var subject = string.Equals(context, "CASE WHEN", StringComparison.Ordinal)
            ? "CASE WHEN requires a boolean expression"
            : $"{context} clause requires a boolean expression";

        return TryFindFirstScriptParameterReference(expression, out var parameter)
            ? $"{subject}, but script parameter '${parameter.Name}' has type '{FormatTypeName(parameter.ReturnType ?? expressionType)}'."
            : $"{subject}, but got '{expressionType.Name}'.";
    }

    public static string FormatTypeName(Type? type)
    {
        if (type == null)
            return "unknown";

        if (type is NullNode.NullType)
            return "null";

        var nullableType = Nullable.GetUnderlyingType(type);
        return nullableType == null
            ? type.Name
            : $"{nullableType.Name}?";
    }

    public static bool ContainsScriptParameterReference(Node node)
    {
        return TryFindFirstScriptParameterReference(node, out _);
    }

    public static bool TryFindFirstScriptParameterReference(
        Node? node,
        [NotNullWhen(true)] out ParameterReferenceNode? parameter)
    {
        switch (node)
        {
            case null:
                parameter = null;
                return false;
            case ParameterReferenceNode parameterReference:
                parameter = parameterReference;
                return true;
            case BinaryNode binaryNode:
                return TryFindFirstScriptParameterReference(binaryNode.Left, out parameter) ||
                       TryFindFirstScriptParameterReference(binaryNode.Right, out parameter);
            case UnaryNode unaryNode:
                return TryFindFirstScriptParameterReference(unaryNode.Expression, out parameter);
            case ArgsListNode argsListNode:
                foreach (var arg in argsListNode.Args)
                {
                    if (TryFindFirstScriptParameterReference(arg, out parameter))
                        return true;
                }

                parameter = null;
                return false;
            case AccessMethodNode accessMethodNode:
                if (TryFindFirstScriptParameterReference(accessMethodNode.Arguments, out parameter))
                    return true;
                return TryFindFirstScriptParameterReference(accessMethodNode.ExtraAggregateArguments, out parameter);
            case BetweenNode betweenNode:
                return TryFindFirstScriptParameterReference(betweenNode.Expression, out parameter) ||
                       TryFindFirstScriptParameterReference(betweenNode.Min, out parameter) ||
                       TryFindFirstScriptParameterReference(betweenNode.Max, out parameter);
            case CaseNode caseNode:
                foreach (var (when, then) in caseNode.WhenThenPairs)
                {
                    if (TryFindFirstScriptParameterReference(when, out parameter) ||
                        TryFindFirstScriptParameterReference(then, out parameter))
                    {
                        return true;
                    }
                }

                return TryFindFirstScriptParameterReference(caseNode.Else, out parameter);
            case FieldNode fieldNode:
                return TryFindFirstScriptParameterReference(fieldNode.Expression, out parameter);
            case IsNullNode isNullNode:
                return TryFindFirstScriptParameterReference(isNullNode.Expression, out parameter);
            default:
                parameter = null;
                return false;
        }
    }

    private static string DescribeComparedExpression(ParameterReferenceNode? parameter, Type expressionType)
    {
        return parameter != null
            ? $"script parameter '${parameter.Name}' of type '{FormatTypeName(parameter.ReturnType ?? expressionType)}'"
            : $"expression of type '{FormatTypeName(expressionType)}'";
    }
}
