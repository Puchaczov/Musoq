using Musoq.Schema.Optimization;

namespace Musoq.Converter;

public sealed record TypedArtifactSourceSlotIdentity
{
    public TypedArtifactSourceSlotIdentity(string schemaName, string sourceName, string rowTypeName,
        SourceExecutionPermissions executionPermissions = SourceExecutionPermissions.None)
    {
        SchemaName = InMemorySourceNames.NormalizeSchemaName(schemaName);
        SourceName = InMemorySourceNames.NormalizeSourceName(sourceName);
        RowTypeName = string.IsNullOrWhiteSpace(rowTypeName)
            ? throw new ArgumentException("Row type name cannot be null or whitespace.", nameof(rowTypeName))
            : rowTypeName;
        Key = InMemorySourceNames.CreateKey(SchemaName, SourceName);
        ExecutionPermissions = executionPermissions;
    }

    public string SchemaName { get; }

    public string SourceName { get; }

    public string RowTypeName { get; }

    public string Key { get; }

    public SourceExecutionPermissions ExecutionPermissions { get; }

    internal static TypedArtifactSourceSlotIdentity FromSlot(InMemorySourceSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        return new TypedArtifactSourceSlotIdentity(
            slot.SchemaName,
            slot.SourceName,
            slot.RowType.AssemblyQualifiedName ?? slot.RowType.FullName ?? slot.RowType.Name, slot.ExecutionPermissions);
    }

    internal bool Matches(InMemorySourceSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        var rowTypeName = slot.RowType.AssemblyQualifiedName ?? slot.RowType.FullName ?? slot.RowType.Name;
        return Key == slot.Key && RowTypeName == rowTypeName && ExecutionPermissions == slot.ExecutionPermissions;
    }
}
