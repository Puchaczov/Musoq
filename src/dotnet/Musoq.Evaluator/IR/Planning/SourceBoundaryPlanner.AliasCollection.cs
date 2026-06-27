using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using ColumnRefExtractor = Musoq.Evaluator.IR.Expressions.ColumnRefExtractor;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class SourceBoundaryPlanner
{
    private static string[] CollectProducedAliases(LogicalNode node)
    {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddProducedAliases(node, aliases);
        return OrderAliases(aliases);
    }

    private static void AddProducedAliases(LogicalNode node, HashSet<string> aliases)
    {
        switch (node)
        {
            case SchemaScanNode scan:
                AddAlias(aliases, scan.Alias);
                break;
            case InterpretSourceNode interpret:
                AddAlias(aliases, interpret.Alias);
                break;
            case PropertySourceNode propertySource:
                AddAlias(aliases, propertySource.Alias);
                break;
            case AccessMethodSourceNode accessMethod:
                AddAlias(aliases, accessMethod.Alias);
                break;
            case CteRefNode cteRef:
                AddAlias(aliases, cteRef.Alias);
                break;
            case ValuesScanNode values:
                AddAlias(aliases, values.Alias);
                break;
        }

        foreach (var child in node.Children)
            AddProducedAliases(child, aliases);
    }

    private static string[] CollectDependencyAliases(LogicalNode node)
    {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddDependencyAliases(node, aliases);
        return OrderAliases(aliases);
    }

    private static void AddDependencyAliases(LogicalNode node, HashSet<string> aliases)
    {
        switch (node)
        {
            case SchemaScanNode scan:
                AddExpressionAliases(scan.Arguments, aliases);
                break;
            case InterpretSourceNode interpret:
                AddExpressionAliases(interpret.Arguments, aliases);
                break;
            case PropertySourceNode propertySource:
                AddAlias(aliases, propertySource.SourceAlias);
                break;
            case AccessMethodSourceNode accessMethod:
                AddAlias(aliases, accessMethod.SourceAlias);
                AddExpressionAliases(accessMethod.MethodCallExpression, aliases);
                break;
        }

        foreach (var child in node.Children)
            AddDependencyAliases(child, aliases);
    }

    private static string[] CollectExpressionAliases(IReadOnlyList<IrExpression> expressions)
    {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddExpressionAliases(expressions, aliases);
        return OrderAliases(aliases);
    }

    private static void AddExpressionAliases(IReadOnlyList<IrExpression> expressions, HashSet<string> aliases)
    {
        foreach (var expression in expressions)
            AddExpressionAliases(expression, aliases);
    }

    private static void AddExpressionAliases(IrExpression expression, HashSet<string> aliases)
    {
        foreach (var column in ColumnRefExtractor.Extract(expression))
            AddAlias(aliases, column.Alias);
    }

    private static string[] CollectAccessMethodInputAliases(AccessMethodSourceNode accessMethod)
    {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddAlias(aliases, accessMethod.SourceAlias);
        AddExpressionAliases(accessMethod.MethodCallExpression, aliases);
        return OrderAliases(aliases);
    }
}
