namespace Musoq.Schema
{
    public interface ISchemaTable
    {
        ISchemaColumn[] Columns { get; }
    }
}