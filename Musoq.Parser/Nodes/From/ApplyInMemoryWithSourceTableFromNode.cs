using System;

namespace Musoq.Parser.Nodes.From;

public class ApplyInMemoryWithSourceTableFromNode : FromNode
{
    internal ApplyInMemoryWithSourceTableFromNode(string inMemoryTableAlias, FromNode sourceTable, ApplyType applyType)
        : base($"{inMemoryTableAlias}{sourceTable.Alias}")
    {
        Id =
            $"{nameof(ApplyInMemoryWithSourceTableFromNode)}{inMemoryTableAlias}{sourceTable.Alias}";
        InMemoryTableAlias = inMemoryTableAlias;
        SourceTable = sourceTable;
        ApplyType = applyType;
    }

    public ApplyInMemoryWithSourceTableFromNode(string inMemoryTableAlias, FromNode sourceTable, ApplyType applyType,
        Type returnType)
        : base($"{inMemoryTableAlias}{sourceTable.Alias}", returnType)
    {
        Id =
            $"{nameof(ApplyInMemoryWithSourceTableFromNode)}{inMemoryTableAlias}{sourceTable.Alias}";
        InMemoryTableAlias = inMemoryTableAlias;
        SourceTable = sourceTable;
        ApplyType = applyType;
    }

    public string InMemoryTableAlias { get; }

    public FromNode SourceTable { get; }

    public override string Id { get; }

    public ApplyType ApplyType { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"apply {InMemoryTableAlias} with {SourceTable.Alias}";
    }
}