using Musoq.Schema;

namespace Musoq.Converter;

internal sealed class InMemorySchemaTable : ISchemaTable
{
    private readonly InMemorySourceShape _shape;

    public InMemorySchemaTable(Type rowType)
        : this(InMemorySourceShape.For(rowType))
    {
    }

    internal InMemorySchemaTable(InMemorySourceShape shape)
    {
        _shape = shape ?? throw new ArgumentNullException(nameof(shape));
    }

    public Type RowType => _shape.RowType;

    public ISchemaColumn[] Columns => _shape.CreateColumnsSnapshot();

    public SchemaTableMetadata Metadata => _shape.Metadata;

    public ISchemaColumn? GetColumnByName(string name)
    {
        return _shape.GetColumnByName(name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return _shape.GetColumnsByName(name);
    }
}
