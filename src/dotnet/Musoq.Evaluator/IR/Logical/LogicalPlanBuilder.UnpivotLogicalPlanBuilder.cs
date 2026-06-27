using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Parser.Nodes.From;
using IrNodes = Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder
{
    public void Visit(UnpivotFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var source = _nodeStack.Pop();
        var entries = node.Entries
            .Select(entry => new IrNodes.UnpivotEntry(entry.NameValue, _converter.Convert(entry.Expression)))
            .ToArray();
        var keepFields = node.KeepFields
            .Select((field, index) => new ProjectedField(field.FieldName, _converter.Convert(field.Expression), index))
            .ToArray();

        _nodeStack.Push(new IrNodes.UnpivotNode(
            node.Alias,
            node.NameColumn,
            node.ValueColumn,
            entries,
            keepFields,
            source,
            BuildOutputSchema(node.Alias)));
    }
}
