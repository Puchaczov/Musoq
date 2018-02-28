namespace Musoq.Schema.FlatFile
{
    public class FlatFileTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = FlatFileHelper.FlatColumns;
    }
}