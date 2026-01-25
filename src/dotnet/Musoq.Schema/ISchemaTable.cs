namespace Musoq.Schema;

public interface ISchemaTable
{
    ISchemaColumn[] Columns { get; }

    SchemaTableMetadata Metadata { get; }

    ISchemaColumn GetColumnByName(string name);

    ISchemaColumn[] GetColumnsByName(string name);
}
