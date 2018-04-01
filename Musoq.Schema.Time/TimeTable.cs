namespace Musoq.Schema.Time
{
    public class TimeTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = TimeHelper.TimeColumns;
    }
}