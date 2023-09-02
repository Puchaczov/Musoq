namespace Musoq.Schema
{
    public interface ISchemaTable
    {
        ISchemaColumn[] Columns { get; }

        ISchemaColumn GetColumnByName(string name);
        
        SchemaTableMetadata Metadata { get; }
    }
}