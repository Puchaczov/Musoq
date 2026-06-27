namespace Musoq.Parser.Nodes.From;

public class ApplyInMemoryWithSourceTableFromNode : FromNode
{
    internal ApplyInMemoryWithSourceTableFromNode(string inMemoryTableAlias, FromNode sourceTable, ApplyType applyType, bool withOrdinality = false)
        : base(CreateAlias(inMemoryTableAlias, sourceTable))
    {
        Id =
            $"{nameof(ApplyInMemoryWithSourceTableFromNode)}{inMemoryTableAlias}{sourceTable.Alias}{(withOrdinality ? "WithOrdinality" : string.Empty)}";
        InMemoryTableAlias = inMemoryTableAlias;
        SourceTable = sourceTable;
        ApplyType = applyType;
        WithOrdinality = withOrdinality;
    }

    public ApplyInMemoryWithSourceTableFromNode(string inMemoryTableAlias, FromNode sourceTable, ApplyType applyType,
        Type returnType, bool withOrdinality = false)
        : base(CreateAlias(inMemoryTableAlias, sourceTable), returnType)
    {
        ArgumentNullException.ThrowIfNull(sourceTable);
        Id =
            $"{nameof(ApplyInMemoryWithSourceTableFromNode)}{inMemoryTableAlias}{sourceTable.Alias}{(withOrdinality ? "WithOrdinality" : string.Empty)}";
        InMemoryTableAlias = inMemoryTableAlias;
        SourceTable = sourceTable;
        ApplyType = applyType;
        WithOrdinality = withOrdinality;
    }

    public string InMemoryTableAlias { get; }

    public FromNode SourceTable { get; }

    public override string Id { get; }

    public ApplyType ApplyType { get; }

    public bool WithOrdinality { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var ordinality = WithOrdinality ? " with ordinality" : string.Empty;

        return $"apply {InMemoryTableAlias} with {SourceTable.Alias}{ordinality}";
    }

    private static string CreateAlias(string inMemoryTableAlias, FromNode sourceTable)
    {
        ArgumentNullException.ThrowIfNull(sourceTable);
        return $"{inMemoryTableAlias}{sourceTable.Alias}";
    }
}
