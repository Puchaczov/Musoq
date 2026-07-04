using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal static class LogicalSubqueryOwnershipFactCollector
{
    public static IReadOnlyList<LogicalSubqueryOwnershipFact> Collect(RootNode root)
    {
        var facts = new List<LogicalSubqueryOwnershipFact>();
        Visit(root, facts);
        return facts;
    }

    private static void Visit(Node? node, List<LogicalSubqueryOwnershipFact> facts)
    {
        switch (node)
        {
            case null:
                return;

            case RootNode root:
                Visit(root.Expression, facts);
                return;

            case StatementsArrayNode statements:
                foreach (var statement in statements.Statements)
                    Visit(statement, facts);
                return;

            case StatementNode statement:
                Visit(statement.Node, facts);
                return;

            case CteExpressionNode cte:
                foreach (var expression in cte.InnerExpression)
                {
                    Record(expression, facts);
                    Visit(expression.Value, facts);
                }

                Visit(cte.OuterExpression, facts);
                return;

            case SingleSetNode singleSet:
                Visit(singleSet.Query, facts);
                return;

            case SetOperatorNode setOperator:
                Visit(setOperator.Left, facts);
                Visit(setOperator.Right, facts);
                return;
        }
    }

    private static void Record(CteInnerExpressionNode expression, List<LogicalSubqueryOwnershipFact> facts)
    {
        if (!IsGeneratedSubqueryName(expression.Name))
            return;

        var outputColumns = CollectOutputColumns(expression.Value);
        var kind = Classify(expression.Name, outputColumns);
        facts.Add(new LogicalSubqueryOwnershipFact(
            expression.Name,
            kind,
            outputColumns.Any(GeneratedSubqueryContract.IsCorrelationColumnName),
            outputColumns,
            CreateReason(expression.Name, kind)));
    }

    private static bool IsGeneratedSubqueryName(string name)
    {
        return GeneratedSubqueryContract.IsGeneratedSubqueryCteName(name) ||
               name.StartsWith(GeneratedSubqueryContract.ScalarMaterializationPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static LogicalSubqueryFormKind Classify(string cteName, IReadOnlyList<string> outputColumns)
    {
        if (GeneratedSubqueryContract.IsDerivedTableCteName(cteName))
            return LogicalSubqueryFormKind.DerivedTable;

        if (cteName.StartsWith(GeneratedSubqueryContract.ScalarMaterializationPrefix, StringComparison.OrdinalIgnoreCase))
            return LogicalSubqueryFormKind.ScalarMaterialization;

        if (outputColumns.Any(column => GeneratedSubqueryContract.IsValueColumnForCte(column, cteName)))
            return LogicalSubqueryFormKind.Scalar;

        return GeneratedSubqueryContract.IsSubqueryCteName(cteName)
            ? LogicalSubqueryFormKind.Predicate
            : LogicalSubqueryFormKind.Unknown;
    }

    private static string[] CollectOutputColumns(Node node)
    {
        var query = GetLeftmostQuery(node);
        return query?.Select.Fields
            .Select(static field => field.FieldName)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .ToArray() ?? [];
    }

    private static QueryNode? GetLeftmostQuery(Node node)
    {
        var current = node;
        while (current is SingleSetNode singleSet)
            current = singleSet.Query;
        while (current is SetOperatorNode setOperator)
            current = setOperator.Left;
        return current as QueryNode;
    }

    private static string CreateReason(string cteName, LogicalSubqueryFormKind kind)
    {
        return $"{cteName} is a generated {kind} subquery form prepared for future logical optimizer ownership.";
    }
}

