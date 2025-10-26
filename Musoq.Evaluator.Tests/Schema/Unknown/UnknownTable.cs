using System.Dynamic;
using System.Linq;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.Unknown;

public class UnknownTable : ISchemaTable
{
    public UnknownTable(RuntimeContext runtimeContext)
    {
        Columns = runtimeContext.AllColumns.ToArray();
    }

    public ISchemaColumn[] Columns { get; }

    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(col => col.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(col => col.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(DynamicObject));
}
