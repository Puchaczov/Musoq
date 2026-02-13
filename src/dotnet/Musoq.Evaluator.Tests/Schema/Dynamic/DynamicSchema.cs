using System;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tests.Schema.Dynamic;

public class DynamicSchema(
    IReadOnlyDictionary<string, Type> tableSchema,
    IEnumerable<dynamic> values,
    Func<RuntimeContext, SchemaMethodInfo[]> getRawConstructors = null,
    Func<string, RuntimeContext, SchemaMethodInfo[]> getRawConstructorsByName = null)
    : SchemaBase(SchemaName, CachedLibrary.Value)
{
    private const string SchemaName = "Dynamic";
    private static readonly Lazy<MethodsAggregator> CachedLibrary = new(CreateLibrary);

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new DynamicTable(tableSchema);
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new DynamicSource(values);
    }

    public override SchemaMethodInfo[] GetRawConstructors(RuntimeContext runtimeContext)
    {
        return getRawConstructors?.Invoke(runtimeContext) ?? base.GetRawConstructors(runtimeContext);
    }

    public override SchemaMethodInfo[] GetRawConstructors(string methodName, RuntimeContext runtimeContext)
    {
        return getRawConstructorsByName?.Invoke(methodName, runtimeContext) ??
               base.GetRawConstructors(methodName, runtimeContext);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();

        var lib = new DynamicLibrary();

        methodManager.RegisterLibraries(lib);

        return new MethodsAggregator(methodManager);
    }
}
