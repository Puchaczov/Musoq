using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(CreateTransformationTableNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var fields = CreateFields(node.Fields);

        Nodes.Push(new CreateTransformationTableNode(node.Name, node.Keys, fields, node.ForGrouping));
    }

    public override void Visit(RenameTableNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new RenameTableNode(node.TableSourceName, node.TableDestinationName));
    }

    public override void Visit(IntoNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new IntoNode(node.Name));
    }

    public override void Visit(ShouldBePresentInTheTable node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new ShouldBePresentInTheTable(node.Table, node.ExpectedResult, node.Keys));
    }

    public override void Visit(JoinNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _sourceBinding.Identifier = node.Alias;
        Nodes.Push(new Parser.JoinNode((Parser.JoinFromNode)Nodes.Pop()));
    }

    public override void Visit(ApplyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _sourceBinding.Identifier = node.Alias;
        Nodes.Push(new Parser.ApplyNode((Parser.ApplyFromNode)Nodes.Pop()));
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
        Nodes.Push(new CoupleNode(node.SchemaMethodNode, node.TableName, node.ProfileName, node.MappedSchemaName));
    }

    public override void Visit(StatementsArrayNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var statements = new StatementNode[node.Statements.Length];
        for (var i = 0; i < node.Statements.Length; ++i)
            statements[node.Statements.Length - 1 - i] = (StatementNode)Nodes.Pop();

        Nodes.Push(new StatementsArrayNode(statements));
    }

    public override void Visit(StatementNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.Node is not ParameterBlockNode)
            _diagnostics.HasSeenNonParameterStatement = true;

        Nodes.Push(new StatementNode(Nodes.Pop()));
    }
}
