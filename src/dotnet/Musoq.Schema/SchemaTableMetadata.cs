namespace Musoq.Schema;

public class SchemaTableMetadata(Type tableEntityType)
{
    /// <summary>Gets whether column types are owned by the provider or supplied by TABLE.</summary>
    public TableSchemaTypeOrigin TypeOrigin { get; init; } = TableSchemaTypeOrigin.ProviderDefined;

    public Type TableEntityType { get; } = tableEntityType;
}
