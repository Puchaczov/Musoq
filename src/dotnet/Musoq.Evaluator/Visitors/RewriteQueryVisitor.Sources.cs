using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using AccessMethodFromNode = Musoq.Parser.Nodes.From.AccessMethodFromNode;
using AliasedFromNode = Musoq.Parser.Nodes.From.AliasedFromNode;
using ApplyFromNode = Musoq.Parser.Nodes.From.ApplyFromNode;
using ApplyNode = Musoq.Parser.Nodes.From.ApplyNode;
using ApplySourcesTableFromNode = Musoq.Parser.Nodes.From.ApplySourcesTableFromNode;
using ExpressionFromNode = Musoq.Parser.Nodes.From.ExpressionFromNode;
using InMemoryTableFromNode = Musoq.Parser.Nodes.From.InMemoryTableFromNode;
using InterpretFromNode = Musoq.Parser.Nodes.From.InterpretFromNode;
using JoinFromNode = Musoq.Parser.Nodes.From.JoinFromNode;
using JoinInMemoryWithSourceTableFromNode = Musoq.Parser.Nodes.From.JoinInMemoryWithSourceTableFromNode;
using JoinNode = Musoq.Parser.Nodes.From.JoinNode;
using JoinSourcesTableFromNode = Musoq.Parser.Nodes.From.JoinSourcesTableFromNode;
using PropertyFromNode = Musoq.Parser.Nodes.From.PropertyFromNode;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;
using SchemaMethodFromNode = Musoq.Parser.Nodes.From.SchemaMethodFromNode;

namespace Musoq.Evaluator.Visitors;

public sealed partial class RewriteQueryVisitor
{
    public void Visit(SchemaFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var rewritten = node is Parser.SchemaFromNode schemaFromNode
            ? new Parser.SchemaFromNode(node.Schema, node.Method, (ArgsListNode)Nodes.Pop(), node.Alias,
                node.QueryId, schemaFromNode.HasExternallyProvidedTypes)
            : new Parser.SchemaFromNode(node.Schema, node.Method, (ArgsListNode)Nodes.Pop(), node.Alias,
                node.QueryId, false);
        if (node.SchemaSpan is { } schemaSpan) rewritten.WithSchemaSpan(schemaSpan);
        if (node.MethodSpan is { } methodSpan) rewritten.WithMethodSpan(methodSpan);
        if (node is Parser.SchemaFromNode boundSource &&
            boundSource.BoundInvocation is { } boundInvocation)
            rewritten.SetBoundInvocation(boundInvocation);
        if (node is Parser.SchemaFromNode metadataSource)
            rewritten.SetStaticMetadataArguments(metadataSource.StaticMetadataArguments, metadataSource.HasRequiredRuntimeArguments);
        Nodes.Push(rewritten);
    }

    public void Visit(JoinSourcesTableFromNode node)
    {
    }

    public void Visit(ApplySourcesTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var second = (FromNode)Nodes.Pop();


        var first = (FromNode)Nodes.Pop();


        Nodes.Push(new Parser.ApplySourcesTableFromNode(first, second, node.ApplyType, node.WithOrdinality));
    }

    public void Visit(JoinFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var tieBreak = node.TieBreak == null ? null : (FieldOrderedNode)Nodes.Pop();
        var exp = Nodes.Pop();
        var right = (FromNode)Nodes.Pop();
        var left = (FromNode)Nodes.Pop();
        Nodes.Push(new Parser.JoinFromNode(left, right, exp, node.JoinType, tieBreak, node.WithOrdinality));
        _joinedTables.Add(node);
    }

    public void Visit(ApplyFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = (FromNode)Nodes.Pop();
        var left = (FromNode)Nodes.Pop();
        var newApply = new Parser.ApplyFromNode(left, right, node.ApplyType, node.WithOrdinality);
        Nodes.Push(newApply);

        _joinedTables.Add(newApply);
    }

    public void Visit(ExpressionFromNode node)
    {
        Nodes.Push(new Parser.ExpressionFromNode((FromNode)Nodes.Pop()));
    }

    public void Visit(InMemoryTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new Parser.InMemoryTableFromNode(node.VariableName, node.Alias));
    }

    public void Visit(ValuesFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var rows = new ValuesRowNode[node.Rows.Count];
        for (var rowIndex = node.Rows.Count - 1; rowIndex >= 0; rowIndex--)
        {
            var sourceRow = node.Rows[rowIndex];
            var fields = new ValuesFieldNode[sourceRow.Fields.Count];
            for (var fieldIndex = sourceRow.Fields.Count - 1; fieldIndex >= 0; fieldIndex--)
            {
                var sourceField = sourceRow.Fields[fieldIndex];
                fields[fieldIndex] = new ValuesFieldNode(sourceField.Name, Nodes.Pop());
            }

            rows[rowIndex] = new ValuesRowNode(fields);
        }

        Nodes.Push(new ValuesFromNode(rows, node.Alias));
    }

    public void Visit(UnpivotFromNode node)
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
        Nodes.Push(new UnpivotFromNode(source, node.NameColumn, node.ValueColumn, entries, keepFields));
    }

    public void Visit(AccessMethodFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new AccessMethodFromNode(node.Alias, node.SourceAlias, node.AccessMethod, node.ReturnType ?? typeof(object)));
    }

    public void Visit(InterpretFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var interpretCall = Nodes.Pop();
        Nodes.Push(new InterpretFromNode(node.Alias, interpretCall, node.ApplyType, node.ReturnType ?? typeof(object)));
    }

    public void Visit(SchemaMethodFromNode node)
    {
    }

    public void Visit(PropertyFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new Parser.PropertyFromNode(node.Alias, node.SourceAlias, node.PropertiesChain));
    }

    public void Visit(AliasedFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var identifier = node.Identifier;
        if (identifier == "Interpret" || identifier == "Parse" || identifier == "InterpretAt") Nodes.Push(node);
    }

    public void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var tieBreak = node.TieBreak == null ? null : (FieldOrderedNode)Nodes.Pop();
        var exp = Nodes.Pop();
        var from = (FromNode)Nodes.Pop();
        Nodes.Push(new Parser.JoinInMemoryWithSourceTableFromNode(
            node.InMemoryTableAlias, from, exp, node.JoinType,
            (node as Parser.JoinInMemoryWithSourceTableFromNode)?.InMemoryTableVariableName,
            tieBreak));
    }

    public void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var from = (FromNode)Nodes.Pop();
        Nodes.Push(new Parser.ApplyInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, node.ApplyType, node.WithOrdinality));
    }

    public void Visit(JoinNode node)
    {
        Nodes.Push(new Parser.JoinNode((Parser.JoinFromNode)Nodes.Pop()));
    }

    public void Visit(ApplyNode node)
    {
        Nodes.Push(new Parser.ApplyNode((Parser.ApplyFromNode)Nodes.Pop()));
    }
}
