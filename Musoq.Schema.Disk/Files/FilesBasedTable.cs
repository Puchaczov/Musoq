namespace Musoq.Schema.Disk.Files
{
    public class FilesBasedTable : ISchemaTable
    {
        public FilesBasedTable()
        {
            Columns = SchemaDiskHelper.FilesColumns;
        }

        public ISchemaColumn[] Columns { get; }
    }
}