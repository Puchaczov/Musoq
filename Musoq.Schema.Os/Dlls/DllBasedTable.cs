namespace Musoq.Schema.Os.Files
{
    public class DllBasedTable : ISchemaTable
    {
        public DllBasedTable()
        {
            Columns = DllInfosHelper.DllInfosColumns;
        }

        public ISchemaColumn[] Columns { get; }
    }
}