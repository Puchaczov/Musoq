namespace Musoq.Schema.Os.Process
{
    public class ProcessBasedTable : ISchemaTable
    {
        public ProcessBasedTable()
        {
            Columns = ProcessHelper.ProcessColumns;
        }

        public ISchemaColumn[] Columns { get; }
    }
}