using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Utility methods extracted from BuildMetadataAndInferTypesVisitor to improve maintainability and testability.
/// </summary>
public static partial class BuildMetadataAndInferTypesVisitorUtilities
{
    internal static bool IsConstantExpression(Node expression)
    {
        return expression is IntegerNode
            or DecimalNode
            or WordNode
            or StringNode
            or NullNode;
    }

    internal static bool ContainsAggregateFunction(Node expression)
    {
        var stack = new Stack<Node>();
        stack.Push(expression);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current is WindowFunctionNode)
                return true;

            if (current is AccessMethodNode methodNode && methodNode.IsAggregateMethod())
                return true;

            switch (current)
            {
                case AccessMethodNode method:
                    foreach (var arg in method.Arguments.Args)
                        stack.Push(arg);
                    if (method.ExtraAggregateArguments != null)
                        foreach (var arg in method.ExtraAggregateArguments.Args)
                            stack.Push(arg);
                    break;
                case BinaryNode binary:
                    stack.Push(binary.Left);
                    stack.Push(binary.Right);
                    break;
                case UnaryNode unary:
                    stack.Push(unary.Expression);
                    break;
                case CastNode cast:
                    stack.Push(cast.Expression);
                    break;
                case FieldNode field:
                    stack.Push(field.Expression);
                    break;
                case CaseNode caseNode:
                    foreach (var whenThen in caseNode.WhenThenPairs)
                    {
                        stack.Push(whenThen.When);
                        stack.Push(whenThen.Then);
                    }

                    if (caseNode.Else != null)
                        stack.Push(caseNode.Else);
                    break;
            }
        }

        return false;
    }

    internal static void CollectColumnNames(Node expression, HashSet<string> columnNames)
    {
        var stack = new Stack<Node>();
        stack.Push(expression);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            switch (current)
            {
                case AccessColumnNode columnNode:
                    columnNames.Add(columnNode.Name);
                    break;
                case BinaryNode binary:
                    stack.Push(binary.Left);
                    stack.Push(binary.Right);
                    break;
                case UnaryNode unary:
                    stack.Push(unary.Expression);
                    break;
                case CastNode cast:
                    stack.Push(cast.Expression);
                    break;
                case FieldNode field:
                    stack.Push(field.Expression);
                    break;
                case AccessMethodNode method:
                    foreach (var arg in method.Arguments.Args)
                        stack.Push(arg);
                    break;
            }
        }
    }

    internal static void FindNonGroupedColumns(
        Node expression,
        HashSet<string> groupByExpressions,
        HashSet<string> groupByColumnNames,
        List<string> nonGroupedColumns)
    {
        var stack = new Stack<Node>();
        stack.Push(expression);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (groupByExpressions.Contains(current.ToString()))
                continue;

            if (current is AccessMethodNode methodNode && methodNode.IsAggregateMethod())
                continue;

            switch (current)
            {
                case AccessColumnNode columnNode:
                    if (!groupByColumnNames.Contains(columnNode.Name))
                        nonGroupedColumns.Add(columnNode.Name);
                    break;
                case AccessMethodNode method:
                    foreach (var arg in method.Arguments.Args)
                        stack.Push(arg);
                    break;
                case BinaryNode binary:
                    stack.Push(binary.Left);
                    stack.Push(binary.Right);
                    break;
                case UnaryNode unary:
                    stack.Push(unary.Expression);
                    break;
                case FieldNode field:
                    stack.Push(field.Expression);
                    break;
                case CaseNode caseNode:
                    foreach (var whenThen in caseNode.WhenThenPairs)
                    {
                        stack.Push(whenThen.When);
                        stack.Push(whenThen.Then);
                    }

                    if (caseNode.Else != null)
                        stack.Push(caseNode.Else);
                    break;
            }
        }
    }

    internal static bool ReferencesConditionalField(Node expression, IReadOnlyList<SchemaFieldNode> contextFields)
    {
        return expression switch
        {
            IdentifierNode id => contextFields.Any(f =>
                f.Name.Equals(id.Name, StringComparison.OrdinalIgnoreCase) && f.IsConditional),
            BinaryNode binary => ReferencesConditionalField(binary.Left, contextFields) ||
                                 ReferencesConditionalField(binary.Right, contextFields),
            _ => false
        };
    }

    internal static Type InferComputedFieldType(Node expression, List<ISchemaColumn> contextColumns)
    {
        if (expression is EqualityNode or IsDistinctFromNode or DiffNode or GreaterNode or GreaterOrEqualNode
            or LessNode or LessOrEqualNode or AndNode or OrNode)
            return typeof(bool);

        if (expression is WordNode)
            return typeof(string);

        if (expression is AccessMethodNode methodNode)
            if (methodNode.Name.Equals("ToString", StringComparison.OrdinalIgnoreCase))
                return typeof(string);

        if (expression is BinaryNode binaryNode)
        {
            var leftType = InferOperandType(binaryNode.Left, contextColumns);
            var rightType = InferOperandType(binaryNode.Right, contextColumns);

            if (expression is AddNode && (leftType == typeof(string) || rightType == typeof(string)))
                return typeof(string);

            if (BinaryOperatorTypeRules.IsNumericType(leftType) &&
                BinaryOperatorTypeRules.IsNumericType(rightType))
                return BinaryOperatorTypeRules.GetWiderNumericType(leftType, rightType);

            return typeof(int);
        }

        return typeof(object);
    }

    internal static Type InferOperandType(Node operand, List<ISchemaColumn> contextColumns)
    {
        if (operand is BinaryNode binaryOp) return InferComputedFieldType(binaryOp, contextColumns);

        if (operand is IdentifierNode identifier)
        {
            var column = contextColumns.FirstOrDefault(c =>
                c.ColumnName.Equals(identifier.Name, StringComparison.OrdinalIgnoreCase));
            return column?.ColumnType ?? typeof(object);
        }

        if (operand is IntegerNode) return typeof(int);

        if (operand is WordNode) return typeof(string);

        if (operand is AccessMethodNode methodNode)
            if (methodNode.Name.Equals("ToString", StringComparison.OrdinalIgnoreCase))
                return typeof(string);

        return typeof(object);
    }
}
