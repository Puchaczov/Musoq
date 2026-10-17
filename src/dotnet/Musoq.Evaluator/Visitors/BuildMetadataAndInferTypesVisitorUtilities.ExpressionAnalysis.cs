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
                    columnNames.Add(columnNode.ToString());
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
                    if (!groupByColumnNames.Contains(columnNode.ToString()))
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

}
