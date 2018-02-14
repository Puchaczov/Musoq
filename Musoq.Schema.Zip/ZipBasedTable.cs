using System.IO.MemoryMappedFiles;

namespace Musoq.Schema.Zip
{
    public class ZipBasedTable : ISchemaTable
    {
        public ZipBasedTable()
        {
            Columns = SchemaZipHelper.SchemaColumns;
        }

        public ISchemaColumn[] Columns { get; }
    }
}
