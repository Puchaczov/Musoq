using System.Diagnostics.CodeAnalysis;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;


namespace Musoq.Evaluator.IR.Physical;

public sealed partial class PhysicalPlanBuilder
{
    private static bool TryResolveKey(
        string keyName,
        Bindings.ProjectedField[]? projectFields,
        Bindings.ColumnSchema[] columns,
        out int index,
        [NotNullWhen(true)] out Type? type)
    {
        if (projectFields is not null)
        {
            foreach (var projectField in projectFields)
            {
                if (MatchesProjectField(projectField, keyName))
                {
                    index = projectField.OutputIndex;
                    type = projectField.Expression.ReturnType;
                    if (type is null)
                        break;
                    return true;
                }
            }
        }

        var column = ResolveKeyColumn(columns, keyName);
        if (column is not null)
        {
            index = column.Index;
            type = column.Type;
            return true;
        }

        index = 0;
        type = null;
        return false;
    }

    private static bool MatchesProjectField(Bindings.ProjectedField field, string keyName)
    {
        if (MatchesNameOrSuffix(field.OutputName, keyName))
            return true;

        var expressionText = IrExpressionPrinter.Print(field.Expression);
        return MatchesNameOrSuffix(expressionText, keyName);
    }

    private static bool MatchesNameOrSuffix(string candidate, string keyName)
    {
        if (string.IsNullOrEmpty(candidate))
            return false;

        if (string.Equals(candidate, keyName, StringComparison.OrdinalIgnoreCase))
            return true;

        return candidate.EndsWith($".{keyName}", StringComparison.OrdinalIgnoreCase);
    }

    private static Bindings.ProjectedField[]? FindTopProjectFields(LogicalNode node)
    {
        while (node is not null)
        {
            switch (node)
            {
                case ProjectNode project:
                    return project.Fields;
                case MultiStatementNode { Statements.Length: > 0 } multi:
                    node = multi.Statements[^1];
                    continue;
                case FilterNode filter:
                    node = filter.Input;
                    continue;
                case HavingFilterNode having:
                    node = having.Input;
                    continue;
                case QualifyFilterNode qualify:
                    node = qualify.Input;
                    continue;
                case SortNode sort:
                    node = sort.Input;
                    continue;
                case SkipNode skip:
                    node = skip.Input;
                    continue;
                case TakeNode take:
                    node = take.Input;
                    continue;
                default:
                    return null;
            }
        }

        return null;
    }

    private static Bindings.ColumnSchema? ResolveKeyColumn(Bindings.ColumnSchema[] columns, string keyName)
    {
        foreach (var column in columns)
        {
            if (string.Equals(column.Name, keyName, StringComparison.OrdinalIgnoreCase))
                return column;
        }

        foreach (var column in columns)
        {
            var dotIndex = column.Name.LastIndexOf('.');
            if (dotIndex < 0)
                continue;
            var unqualified = column.Name[(dotIndex + 1)..];
            if (string.Equals(unqualified, keyName, StringComparison.OrdinalIgnoreCase))
                return column;
        }

        return null;
    }
}
