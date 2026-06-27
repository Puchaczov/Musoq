using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tables;

internal class TransitionSchema(string name, ISchemaTable table)
    : SchemaBase(name, GetOrCreateLibrary())
{
    private static readonly Lazy<MethodsAggregator> CachedLibrary = new(CreateLibrary);

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return table;
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, object>(name, new TransientVariableSource(name));
    }

    private static MethodsAggregator GetOrCreateLibrary()
    {
        return CachedLibrary.Value;
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();

        var library = new TransitionLibrary();

        methodsManager.RegisterLibraries(library);

        return new MethodsAggregator(methodsManager);
    }

    public override SchemaMethodInfo[] GetConstructors()
    {
        var constructors = new List<SchemaMethodInfo>();

        constructors.AddRange(TypeHelper.GetSchemaMethodInfosForType<TransientVariableSource>("transient"));

        return constructors.ToArray();
    }
}
