namespace Musoq.Schema.Os.Self
{
    public class OsBasedTable : ISchemaTable
    {
        public OsBasedTable()
        {
            Columns = OsHelper.ProcessColumns;
        }

        public ISchemaColumn[] Columns { get; }
    }
}