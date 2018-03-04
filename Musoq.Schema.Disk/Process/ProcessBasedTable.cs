namespace Musoq.Schema.Disk.Process
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