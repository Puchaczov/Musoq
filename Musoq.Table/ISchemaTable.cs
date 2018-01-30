using FQL.Schema.DataSources;

namespace FQL.Schema
{
    public interface ISchemaTable
    {
        ISchemaColumn[] Columns { get; }
    }
}