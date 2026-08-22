using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class CloneQueryVisitor
{
    public override void Visit(WhereNode node) => Nodes.Push(new WhereNode(Nodes.Pop()));

    public override void Visit(GroupByNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var having = Nodes.Peek() as HavingNode;

        if (having != null)
            Nodes.Pop();

        var fields = new FieldNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            fields[i] = SafeCast<FieldNode>(Nodes.Pop(), nameof(Visit));

        Nodes.Push(new GroupByNode(fields, having, node.IsAll, node.Span));
    }

    public override void Visit(HavingNode node) => Nodes.Push(new HavingNode(Nodes.Pop()));

    public override void Visit(QualifyNode node) => Nodes.Push(new QualifyNode(Nodes.Pop()));

    public override void Visit(SkipNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new SkipNode((IntegerNode)node.Expression));
    }

    public override void Visit(TakeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new TakeNode((IntegerNode)node.Expression));
    }

    public override void Visit(SchemaFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var cloned =
            node is Parser.SchemaFromNode schemaFromNode
                ? new Parser.SchemaFromNode(node.Schema, node.Method, (ArgsListNode)Nodes.Pop(), node.Alias,
                    node.QueryId, schemaFromNode.HasExternallyProvidedTypes)
                : new Parser.SchemaFromNode(node.Schema, node.Method, (ArgsListNode)Nodes.Pop(), node.Alias,
                    node.QueryId, false);
        if (node.HasSpan)
            cloned.WithSpan(node.Span);
        if (node is Parser.SchemaFromNode boundSource &&
            boundSource.BoundInvocation is { } boundInvocation)
            cloned.SetBoundInvocation(boundInvocation);
        if (node is Parser.SchemaFromNode metadataSource)
            cloned.SetStaticMetadataArguments(metadataSource.StaticMetadataArguments, metadataSource.HasRequiredRuntimeArguments);
        Nodes.Push(cloned);
    }

    public override void Visit(JoinSourcesTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var tieBreak = node.TieBreak == null ? null : (FieldOrderedNode)Nodes.Pop();
        var b = (FromNode)Nodes.Pop();
        var a = (FromNode)Nodes.Pop();
        var exp = Nodes.Pop();

        Nodes.Push(new Parser.JoinSourcesTableFromNode(a, b, exp, node.JoinType, tieBreak));
    }

    public override void Visit(ApplySourcesTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var b = (FromNode)Nodes.Pop();
        var a = (FromNode)Nodes.Pop();

        Nodes.Push(new Parser.ApplySourcesTableFromNode(a, b, node.ApplyType, node.WithOrdinality));
    }

    public override void Visit(InMemoryTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node is DerivedTableFromNode derived)
        {
            var clonedDerived = new DerivedTableFromNode(Nodes.Pop(), derived.Alias, derived.AllowsCorrelation);
            if (derived.HasSpan)
                clonedDerived.WithSpan(derived.Span);
            if (!derived.FullSpan.IsEmpty)
                clonedDerived.WithFullSpan(derived.FullSpan);
            Nodes.Push(clonedDerived);
            return;
        }

        var cloned = new Parser.InMemoryTableFromNode(node.VariableName, node.Alias);
        if (node.HasSpan)
            cloned.WithSpan(node.Span);
        Nodes.Push(cloned);
    }

    public override void Visit(ValuesFromNode node)
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

        var cloned = new ValuesFromNode(rows, node.Alias);
        if (node.HasSpan)
            cloned.WithSpan(node.Span);
        if (!node.FullSpan.IsEmpty)
            cloned.WithFullSpan(node.FullSpan);
        Nodes.Push(cloned);
    }

    public override void Visit(JoinFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var tieBreak = node.TieBreak == null ? null : (FieldOrderedNode)Nodes.Pop();
        var expression = Nodes.Pop();
        var joinedTable = (FromNode)Nodes.Pop();
        var source = (FromNode)Nodes.Pop();
        var joinedFrom = new Parser.JoinFromNode(source, joinedTable, expression, node.JoinType, tieBreak);
        Nodes.Push(joinedFrom);
    }

    public override void Visit(ApplyFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var appliedTable = (FromNode)Nodes.Pop();
        var source = (FromNode)Nodes.Pop();
        var appliedFrom = new Parser.ApplyFromNode(source, appliedTable, node.ApplyType, node.WithOrdinality);
        Nodes.Push(appliedFrom);
    }

    public override void Visit(ExpressionFromNode node) => Nodes.Push(new Parser.ExpressionFromNode((FromNode)Nodes.Pop()));

    public override void Visit(InterpretFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var interpretCall = Nodes.Pop();
        Nodes.Push(new Parser.InterpretFromNode(node.Alias, interpretCall, node.ApplyType, node.ReturnType ?? typeof(object)));
    }

    public override void Visit(AccessMethodFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var accessMethod = (AccessMethodNode)Nodes.Pop();
        Nodes.Push(new Parser.AccessMethodFromNode(node.Alias, node.SourceAlias, accessMethod, node.ReturnType ?? typeof(object)));
    }

    public override void Visit(PropertyFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new Parser.PropertyFromNode(node.Alias, node.SourceAlias, node.PropertiesChain));
    }

    public override void Visit(AliasedFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new Parser.AliasedFromNode(node.Identifier, (ArgsListNode)Nodes.Pop(), node.Alias,
            node.InSourcePosition, node.TypeParameter));
    }

    public override void Visit(SchemaMethodFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new Parser.SchemaMethodFromNode(node.Alias, node.Schema, node.Method));
    }

    public override void Visit(CreateTransformationTableNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var items = new FieldNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            items[i] = (FieldNode)Nodes.Pop();

        Nodes.Push(new CreateTransformationTableNode(node.Name, node.Keys, items, node.ForGrouping));
    }

    public override void Visit(RenameTableNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new RenameTableNode(node.TableSourceName, node.TableDestinationName));
    }

    public override void Visit(TranslatedSetTreeNode node) { }

    public override void Visit(IntoNode node) { ArgumentNullException.ThrowIfNull(node); Nodes.Push(new IntoNode(node.Name)); }

    public override void Visit(QueryScope node) { }

    public override void Visit(ShouldBePresentInTheTable node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new ShouldBePresentInTheTable(node.Table, node.ExpectedResult, node.Keys));
    }

    public override void Visit(TranslatedSetOperatorNode node) { }

    public override void Visit(WindowFunctionNode node) => Nodes.Push(node);

    public override void Visit(WindowSpecificationNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var frame = node.Frame == null ? null : (WindowFrameNode)Nodes.Pop();

        var orderByFields = new FieldOrderedNode[node.OrderByFields.Length];
        for (var i = node.OrderByFields.Length - 1; i >= 0; i--)
            orderByFields[i] = (FieldOrderedNode)Nodes.Pop();

        var partitionFields = new FieldNode[node.PartitionFields.Length];
        for (var i = node.PartitionFields.Length - 1; i >= 0; i--)
            partitionFields[i] = (FieldNode)Nodes.Pop();

        Nodes.Push(new WindowSpecificationNode(partitionFields, orderByFields, frame));
    }

    public override void Visit(WindowFrameNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var end = (WindowFrameBoundNode)Nodes.Pop();
        var start = (WindowFrameBoundNode)Nodes.Pop();
        Nodes.Push(new WindowFrameNode(node.FrameType, start, end));
    }

    public override void Visit(WindowFrameBoundNode node) { ArgumentNullException.ThrowIfNull(node); Nodes.Push(new WindowFrameBoundNode(node.BoundType, node.Offset)); }

    public override void Visit(WindowDefinitionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var spec = (WindowSpecificationNode)Nodes.Pop();
        Nodes.Push(new WindowDefinitionNode(node.Name, spec));
    }

    public override void Visit(WindowNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var definitions = new WindowDefinitionNode[node.Definitions.Length];
        for (var i = node.Definitions.Length - 1; i >= 0; i--)
            definitions[i] = (WindowDefinitionNode)Nodes.Pop();

        Nodes.Push(new WindowNode(definitions));
    }
}
