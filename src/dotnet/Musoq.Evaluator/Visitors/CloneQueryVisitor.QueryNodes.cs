using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class CloneQueryVisitor
{
    public override void Visit(QueryNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var orderBy = node.OrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var qualify = node.Qualify != null ? Nodes.Pop() as QualifyNode : null;
        var window = node.Window != null ? Nodes.Pop() as WindowNode : null;
        var groupBy = node.GroupBy != null ? Nodes.Pop() as GroupByNode : null;

        var skip = node.Skip != null ? Nodes.Pop() as SkipNode : null;
        var take = node.Take != null ? Nodes.Pop() as TakeNode : null;

        var select = SafeCast<SelectNode>(Nodes.Pop(), nameof(Visit));
        var where = node.Where != null ? Nodes.Pop() as WhereNode : null;
        var from = SafeCast<FromNode>(Nodes.Pop(), nameof(Visit));

        Nodes.Push(new QueryNode(select, from, where, groupBy, orderBy, skip, take, window, qualify, default));
    }

    public override void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var tieBreak = node.TieBreak == null ? null : (FieldOrderedNode)Nodes.Pop();
        var exp = Nodes.Pop();
        var from = (FromNode)Nodes.Pop();
        Nodes.Push(new Parser.JoinInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, exp, node.JoinType, tieBreak: tieBreak));
    }

    public override void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var from = (FromNode)Nodes.Pop();
        Nodes.Push(new Parser.ApplyInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, node.ApplyType, node.WithOrdinality));
    }

    public override void Visit(InternalQueryNode node)
    {
        throw new NotSupportedException();
    }

    public override void Visit(RootNode node)
    {
        Nodes.Push(new RootNode(Nodes.Pop()));
    }

    public override void Visit(SingleSetNode node)
    {
    }

    public override void Visit(RefreshNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new RefreshNode(node.Nodes));
    }

    public override void Visit(UnionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var take = node.ResultTake != null ? Nodes.Pop() as TakeNode : null;
        var skip = node.ResultSkip != null ? Nodes.Pop() as SkipNode : null;
        var orderBy = node.ResultOrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        Nodes.Push(new UnionNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
            orderBy, skip, take));
    }

    public override void Visit(UnionAllNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var take = node.ResultTake != null ? Nodes.Pop() as TakeNode : null;
        var skip = node.ResultSkip != null ? Nodes.Pop() as SkipNode : null;
        var orderBy = node.ResultOrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        Nodes.Push(new UnionAllNode(node.ResultTableName, node.Keys, left, right, node.IsNested,
            node.IsTheLastOne, orderBy, skip, take));
    }

    public override void Visit(ExceptNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var take = node.ResultTake != null ? Nodes.Pop() as TakeNode : null;
        var skip = node.ResultSkip != null ? Nodes.Pop() as SkipNode : null;
        var orderBy = node.ResultOrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new ExceptNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
            orderBy, skip, take));
    }

    public override void Visit(IntersectNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var take = node.ResultTake != null ? Nodes.Pop() as TakeNode : null;
        var skip = node.ResultSkip != null ? Nodes.Pop() as SkipNode : null;
        var orderBy = node.ResultOrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(
            new IntersectNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
                orderBy, skip, take));
    }

    public override void Visit(PutTrueNode node)
    {
        Nodes.Push(new PutTrueNode());
    }

    public override void Visit(MultiStatementNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var items = new Node[node.Nodes.Length];

        for (var i = node.Nodes.Length - 1; i >= 0; --i)
            items[i] = Nodes.Pop();

        Nodes.Push(new MultiStatementNode(items, node.ReturnType));
    }

    public override void Visit(BinarySchemaNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var fields = new SchemaFieldNode[node.Fields.Length];
        for (var index = fields.Length - 1; index >= 0; index--)
            fields[index] = (SchemaFieldNode)Nodes.Pop();

        Nodes.Push(new BinarySchemaNode(
            node.Name,
            fields,
            node.Extends,
            (string[])node.TypeParameters.Clone()));
    }

    public override void Visit(TextSchemaNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var fields = new TextFieldDefinitionNode[node.Fields.Length];
        for (var index = fields.Length - 1; index >= 0; index--)
            fields[index] = (TextFieldDefinitionNode)Nodes.Pop();

        Nodes.Push(new TextSchemaNode(node.Name, fields, node.Extends));
    }

    public override void Visit(CteExpressionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var sets = new CteInnerExpressionNode[node.InnerExpression.Length];

        for (var i = node.InnerExpression.Length - 1; i >= 0; --i)
            sets[i] = (CteInnerExpressionNode)Nodes.Pop();

        Nodes.Push(new CteExpressionNode(sets, Nodes.Pop(), node.IsRecursive).CopySpansFrom(node));
    }

    public override void Visit(CteInnerExpressionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new CteInnerExpressionNode(Nodes.Pop(), node.Name, (CteColumnName[])node.Columns.Clone(), node.IsRecursiveDefinition).CopySpansFrom(node));
    }

    public override void Visit(JoinNode node)
    {
        Nodes.Push(new Parser.JoinNode((Parser.JoinFromNode)Nodes.Pop()));
    }

    public override void Visit(ApplyNode node)
    {
        Nodes.Push(new Parser.ApplyNode((Parser.ApplyFromNode)Nodes.Pop()));
    }

    public override void Visit(OrderByNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var fields = new FieldOrderedNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            fields[i] = (FieldOrderedNode)Nodes.Pop();

        Nodes.Push(new OrderByNode(fields));
    }

    public override void Visit(CreateTableNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new CreateTableNode(node.Name, node.Columns));
    }

    public override void Visit(CoupleNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
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
        Nodes.Push(new StatementNode(Nodes.Pop()));
    }

    public override void Visit(CaseNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var whenThenPairs = new List<(Node When, Node Then)>();

        for (var i = 0; i < node.WhenThenPairs.Length; ++i)
        {
            var then = Nodes.Pop();
            var when = Nodes.Pop();
            whenThenPairs.Add((when, then));
        }

        var elseNode = Nodes.Pop();

        Nodes.Push(new CaseNode(whenThenPairs.ToArray(), elseNode, node.ReturnType));
    }

    public override void Visit(WhenNode node)
    {
        Nodes.Push(new WhenNode(Nodes.Pop()));
    }

    public override void Visit(ThenNode node)
    {
        Nodes.Push(new ThenNode(Nodes.Pop()));
    }

    public override void Visit(ElseNode node)
    {
        Nodes.Push(new ElseNode(Nodes.Pop()));
    }

}
