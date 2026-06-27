using System.Collections.Generic;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder
{

    public void Visit(SelectNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _projectedFields.Clear();
        _selectVisited = true;
        _selectIsDistinct = node.IsDistinct;

        var nameCounts = new Dictionary<string, int>();
        foreach (var field in node.Fields)
            nameCounts[field.FieldName] = nameCounts.GetValueOrDefault(field.FieldName) + 1;

        for (var i = 0; i < node.Fields.Length; i++)
        {
            var field = node.Fields[i];
            var expr = _converter.Convert(field.Expression);
            var outputName = ResolveProjectedFieldName(field.FieldName, expr, nameCounts);
            _projectedFields.Add(new ProjectedField(outputName, expr, i));
        }
    }

    private static string ResolveProjectedFieldName(
        string baseName,
        IrExpression expression,
        Dictionary<string, int> nameCounts)
    {
        if (nameCounts.TryGetValue(baseName, out var count) && count > 1
            && expression is ColumnRef columnRef
            && !string.IsNullOrEmpty(columnRef.Alias))
            return NamingHelper.ToColumnName(columnRef.Alias, baseName);

        return baseName;
    }

    public void Visit(GroupSelectNode node)
    {
    }
}
