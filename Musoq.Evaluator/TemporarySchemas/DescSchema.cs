using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Reflection;
using System.Collections.Generic;

namespace Musoq.Evaluator.TemporarySchemas;

public class DescSchema : SchemaBase
{
    private readonly ISchemaColumn[] _columns;
    private readonly ISchemaTable _table;

    public DescSchema(string name, ISchemaTable table, ISchemaColumn[] columns)
        : base(name, null)
    {
        _table = table;
        _columns = columns;
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return _table;
    }

    public override RowSource GetRowSource(string name, RuntimeContext interCommunicator, params object[] parameters)
    {
        return new TableMetadataSource(_columns);
    }

    public override SchemaMethodInfo[] GetConstructors()
    {
        var constructors = new List<SchemaMethodInfo> {new(nameof(TableMetadataSource), ConstructorInfo.Empty<TableMetadataSource>())};

        return constructors.ToArray();
    }
}