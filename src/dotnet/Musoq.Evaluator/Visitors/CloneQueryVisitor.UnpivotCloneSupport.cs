using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class CloneQueryVisitor
{
    public override void Visit(UnpivotFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var keepFields = new FieldNode[node.KeepFields.Count];
        for (var index = node.KeepFields.Count - 1; index >= 0; index--)
            keepFields[index] = (FieldNode)Nodes.Pop();

        var entries = new UnpivotEntryNode[node.Entries.Count];
        for (var index = node.Entries.Count - 1; index >= 0; index--)
        {
            var sourceEntry = node.Entries[index];
            entries[index] = new UnpivotEntryNode(Nodes.Pop(), sourceEntry.NameValue, sourceEntry.NameValueSpan);
        }

        var source = (FromNode)Nodes.Pop();
        var cloned = new UnpivotFromNode(source, node.NameColumn, node.ValueColumn, entries, keepFields);
        if (node.HasSpan)
            cloned.WithSpan(node.Span);
        Nodes.Push(cloned);
    }
}
