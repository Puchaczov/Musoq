namespace Musoq.Schema.Os.Zip
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