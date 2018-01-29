namespace FQL.Schema.Word
{
    public class WordBasedTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = SchemaWordHelper.SchemaColumns;
    }
}