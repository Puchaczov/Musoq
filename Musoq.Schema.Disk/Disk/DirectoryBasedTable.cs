using FQL.Schema.DataSources;

namespace FQL.Schema.Disk.Disk
{
    public class DirectoryBasedTable : ISchemaTable
    {
        public DirectoryBasedTable()
        {
            Columns = SchemaDiskHelper.SchemaColumns;
        }

        public ISchemaColumn[] Columns { get; }
    }
}