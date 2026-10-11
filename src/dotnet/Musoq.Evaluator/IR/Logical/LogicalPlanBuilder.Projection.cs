using Musoq.Evaluator.IR.Bindings;
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

        for (var i = 0; i < node.Fields.Length; i++)
        {
            var field = node.Fields[i];
            var expr = _converter.Convert(field.Expression);
            var outputName = field.FieldName;
            _projectedFields.Add(new ProjectedField(outputName, expr, i));
        }
    }

    public void Visit(GroupSelectNode node)
    {
    }
}
