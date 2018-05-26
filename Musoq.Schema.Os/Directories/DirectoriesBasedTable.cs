namespace Musoq.Schema.Os.Directories
{
    public class DirectoriesBasedTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = SchemaDirectoriesHelper.DirectoriesColumns;
    }
}