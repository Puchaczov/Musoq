namespace Musoq.Schema.Os.Files
{
    public class FilesBasedTable : ISchemaTable
    {
        public FilesBasedTable()
        {
            Columns = SchemaFilesHelper.FilesColumns;
        }

        public ISchemaColumn[] Columns { get; }
    }
}