using Musoq.Schema.Optimization;

namespace Musoq.Converter;

internal sealed class InMemorySourceSlot
{
    public InMemorySourceSlot(string schemaName, string sourceName, Type rowType,
        SourceExecutionPermissions executionPermissions = SourceExecutionPermissions.None)
    {
        SchemaName = InMemorySourceNames.NormalizeSchemaName(schemaName);
        SourceName = InMemorySourceNames.NormalizeSourceName(sourceName);
        RowType = rowType ?? throw new ArgumentNullException(nameof(rowType));
        ExecutionPermissions = executionPermissions;
        Key = InMemorySourceNames.CreateKey(SchemaName, SourceName);
    }

    public string SchemaName { get; }

    public string SourceName { get; }

    public Type RowType { get; }

    public SourceExecutionPermissions ExecutionPermissions { get; }

    public string Key { get; }
}
