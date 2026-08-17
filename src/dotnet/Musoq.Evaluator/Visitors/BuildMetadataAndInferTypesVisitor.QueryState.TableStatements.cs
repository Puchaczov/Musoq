using Musoq.Evaluator.Utils;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(CreateTransformationTableNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var fields = CreateFields(node.Fields);

        PushSemanticNode(new CreateTransformationTableNode(node.Name, node.Keys, fields, node.ForGrouping));
    }

    public override void Visit(RenameTableNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        PushSemanticNode(new RenameTableNode(node.TableSourceName, node.TableDestinationName));
    }

    public override void Visit(IntoNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        PushSemanticNode(new IntoNode(node.Name));
    }

    public override void Visit(ShouldBePresentInTheTable node)
    {
        ArgumentNullException.ThrowIfNull(node);
        PushSemanticNode(new ShouldBePresentInTheTable(node.Table, node.ExpectedResult, node.Keys));
    }

    public override void Visit(JoinNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _sourceBinding.Identifier = node.Alias;
        PushSemanticNode(new Parser.JoinNode((Parser.JoinFromNode)PopSemanticNode()));
    }

    public override void Visit(ApplyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _sourceBinding.Identifier = node.Alias;
        PushSemanticNode(new Parser.ApplyNode((Parser.ApplyFromNode)PopSemanticNode()));
    }

    public void SetScope(Scope scope)
    {
        _sourceBinding.CurrentScope = scope;
    }

    public override void Visit(CoupleNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _sourceBinding.ExplicitlyCoupledSources.Add(
            node.MappedSchemaName,
            new CoupledSourceDefinition(node.SchemaMethodNode, node.TableName, node.ProfileName));
        PushSemanticNode(new CoupleNode(node.SchemaMethodNode, node.TableName, node.ProfileName, node.MappedSchemaName));
    }

    public override void Visit(StatementsArrayNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var statements = new StatementNode[node.Statements.Length];
        for (var i = 0; i < node.Statements.Length; ++i)
            statements[node.Statements.Length - 1 - i] = (StatementNode)PopSemanticNode();

        PushSemanticNode(new StatementsArrayNode(statements));
    }

    public override void Visit(StatementNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.Node is not ParameterBlockNode)
            _diagnostics.HasSeenNonParameterStatement = true;

        // Interpretation schema definitions are intentionally skipped by the
        // metadata traverser. They still arrive wrapped in a StatementNode,
        // so there is no semantic child on the stack to pop.
        if (node.Node is BinarySchemaNode or TextSchemaNode)
        {
            PushSemanticNode(new StatementNode(node.Node));
            return;
        }

        PushSemanticNode(new StatementNode(PopSemanticNode()));
    }
}
