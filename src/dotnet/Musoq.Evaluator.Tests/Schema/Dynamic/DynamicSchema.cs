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
    Func<SourceMetadataContext, SchemaMethodInfo[]>? getRawConstructors = null,
    Func<string, SourceMetadataContext, SchemaMethodInfo[]>? getRawConstructorsByName = null)
    : SchemaBase(SchemaName, CachedLibrary.Value)
{
    private const string SchemaName = "Dynamic";
    private static readonly Lazy<MethodsAggregator> CachedLibrary = new(CreateLibrary);

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new DynamicTable(tableSchema);
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, IReadOnlyDictionary<string, object?>>(name, new DynamicSource(values));
    }

    public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext)
    {
        return getRawConstructors?.Invoke(metadataContext) ?? base.GetRawConstructors(metadataContext);
    }

    public override SchemaMethodInfo[] GetRawConstructors(string methodName, SourceMetadataContext metadataContext)
    {
        return getRawConstructorsByName?.Invoke(methodName, metadataContext) ??
               base.GetRawConstructors(methodName, metadataContext);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();

        var lib = new DynamicLibrary();

        methodManager.RegisterLibraries(lib);

        return new MethodsAggregator(methodManager);
    }
}
