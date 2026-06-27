using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.Basic;

public class GenericSchema<T, TTable> : SchemaBase
{
    private static readonly Lazy<MethodsAggregator> CachedLibrary = new(CreateLibrary);
    private readonly IReadOnlyList<T> _sources;

    public GenericSchema(IEnumerable<T> sources, IDictionary<string, int> testNameToIndexMap,
        IDictionary<int, Func<T, object?>> testIndexToObjectAccessMap)
        : base("test", CachedLibrary.Value)
    {
        _sources = sources as IReadOnlyList<T> ?? sources.ToArray();
        AddSource<EntitySource<T>>("entities", new[] { _sources }, testNameToIndexMap, testIndexToObjectAccessMap);
        AddTable<TTable>("entities");
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();

        var lib = new Library();

        methodManager.RegisterLibraries(lib);

        return new MethodsAggregator(methodManager);
    }
}
