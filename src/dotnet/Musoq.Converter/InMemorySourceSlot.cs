namespace Musoq.Converter;

internal sealed class InMemorySourceSlot
{
    public InMemorySourceSlot(string schemaName, string sourceName, Type rowType)
    {
        SchemaName = InMemorySourceNames.NormalizeSchemaName(schemaName);
        SourceName = InMemorySourceNames.NormalizeSourceName(sourceName);
        RowType = rowType ?? throw new ArgumentNullException(nameof(rowType));
        Key = InMemorySourceNames.CreateKey(SchemaName, SourceName);
    }

    public string SchemaName { get; }

    public string SourceName { get; }

    public Type RowType { get; }

    public string Key { get; }
}
