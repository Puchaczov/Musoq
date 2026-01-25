using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.TemporarySchemas;

/// <summary>
///     Schema for describing table metadata using the DESC command.
/// </summary>
public class DescSchema : SchemaBase
{
    private readonly ISchemaColumn[] _columns;
    private readonly ISchemaTable _table;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DescSchema" /> class.
    /// </summary>
    /// <param name="name">The name of the schema.</param>
    /// <param name="table">The table to describe.</param>
    /// <param name="columns">The columns of the table.</param>
    public DescSchema(string name, ISchemaTable table, ISchemaColumn[] columns)
        : base(name, CreateEmptyAggregator())
    {
        _table = table;
        _columns = columns;
    }

    /// <summary>
    ///     Gets the table by name.
    /// </summary>
    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return _table;
    }

    /// <summary>
    ///     Gets the row source for the table metadata.
    /// </summary>
    public override RowSource GetRowSource(string name, RuntimeContext interCommunicator, params object[] parameters)
    {
        return new TableMetadataSource(_columns);
    }

    /// <summary>
    ///     Gets the constructors available for this schema.
    /// </summary>
    public override SchemaMethodInfo[] GetConstructors()
    {
        var constructors = new List<SchemaMethodInfo> { new(nameof(TableMetadataSource), ConstructorInfo.Empty()) };

        return constructors.ToArray();
    }

    private static MethodsAggregator CreateEmptyAggregator()
    {
        return new MethodsAggregator(new MethodsManager());
    }
}
