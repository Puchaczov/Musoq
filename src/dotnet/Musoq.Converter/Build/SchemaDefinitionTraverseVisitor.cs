using System;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Converter.Build;

/// <summary>
///     Traverse visitor for extracting schema definitions from the AST.
/// </summary>
public class SchemaDefinitionTraverseVisitor : NoOpExpressionVisitor
{
    private readonly SchemaDefinitionVisitor _visitor;

    public SchemaDefinitionTraverseVisitor(SchemaDefinitionVisitor visitor)
    {
        _visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
    }

    public override void Visit(BinarySchemaNode node)
    {
        _visitor.Visit(node);
    }

    public override void Visit(TextSchemaNode node)
    {
        _visitor.Visit(node);
    }

    public override void Visit(RootNode node)
    {
        node.Expression?.Accept(this);
    }

    public override void Visit(StatementsArrayNode node)
    {
        foreach (var statement in node.Statements) statement.Accept(this);
    }

    public override void Visit(StatementNode node)
    {
        node.Node?.Accept(this);
    }

    public override void Visit(MultiStatementNode node)
    {
        foreach (var statement in node.Nodes) statement.Accept(this);
    }
}
