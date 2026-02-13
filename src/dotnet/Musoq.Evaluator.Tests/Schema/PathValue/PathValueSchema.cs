using System;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.PathValue;

public class PathValueSchema : SchemaBase
{
    private const string SchemaName = "pathvalue";
    private static readonly Lazy<MethodsAggregator> CachedLibrary = new(CreateLibrary);
    private readonly IEnumerable<PathValueEntity> _entities;

    public PathValueSchema(IEnumerable<PathValueEntity> entities)
        : base(SchemaName, CachedLibrary.Value)
    {
        _entities = entities;
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new PathValueSchemaTable();
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new PathValueRowSource(_entities);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        var library = new PathValueLibrary();

        methodsManager.RegisterLibraries(library);

        return new MethodsAggregator(methodsManager);
    }
}
