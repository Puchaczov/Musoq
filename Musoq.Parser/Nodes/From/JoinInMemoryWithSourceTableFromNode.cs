using System;

namespace Musoq.Parser.Nodes.From;

public class JoinInMemoryWithSourceTableFromNode : FromNode
{
    internal JoinInMemoryWithSourceTableFromNode(string inMemoryTableAlias, FromNode sourceTable, Node expression, JoinType joinType)
        : base($"{inMemoryTableAlias}{sourceTable.Alias}")
    {
        Id =
            $"{nameof(JoinInMemoryWithSourceTableFromNode)}{inMemoryTableAlias}{sourceTable.Alias}{expression.ToString()}";
        InMemoryTableAlias = inMemoryTableAlias;
        SourceTable = sourceTable;
        Expression = expression;
        JoinType = joinType;
    }
        
    public JoinInMemoryWithSourceTableFromNode(string inMemoryTableAlias, FromNode sourceTable, Node expression, JoinType joinType, Type returnType)
        : base($"{inMemoryTableAlias}{sourceTable.Alias}", returnType)
    {
        Id =
            $"{nameof(JoinInMemoryWithSourceTableFromNode)}{inMemoryTableAlias}{sourceTable.Alias}{expression.ToString()}";
        InMemoryTableAlias = inMemoryTableAlias;
        SourceTable = sourceTable;
        Expression = expression;
        JoinType = joinType;
    }

    public string InMemoryTableAlias { get; }

    public FromNode SourceTable { get; }

    public Node Expression { get; }

    public override string Id { get; }

    public JoinType JoinType { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"join {InMemoryTableAlias} with {SourceTable.Alias} on {Expression.ToString()}";
    }
}