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

    public override SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters)
        => base.DescribeSource(name, context, parameters) with
        {
            ExecutionPermissions = SourceExecutionPermissions.ConcurrentAccess | SourceExecutionPermissions.ConcurrentRowProcessing
        };

    public GenericSchema(IEnumerable<T> sources, IDictionary<string, int> testNameToIndexMap,
        IDictionary<int, Func<T, object?>> testIndexToObjectAccessMap)
        : base("test", CachedLibrary.Value)
    {
        _sources = sources as IReadOnlyList<T> ?? sources.ToArray();
        AddSource<EntitySource<T>>("entities", new[] { _sources }, testNameToIndexMap, testIndexToObjectAccessMap);
        AddTable<TTable>("entities");
    }

    public override RowSource<TRow> GetRowSource<TRow, TArguments>(string name,
        SourceExecutionContext context, TArguments arguments)
    {
        if (!name.Equals("entities", StringComparison.OrdinalIgnoreCase) || arguments.Count != 0)
            throw new NotSupportedException(name);
        return EnsureSourceType<TRow, T>(name, new EntitySource<T>(
            new[] { _sources }, new Dictionary<string, int>(), new Dictionary<int, Func<T, object?>>()));
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();

        var lib = new Library();

        methodManager.RegisterLibraries(lib);

        return new MethodsAggregator(methodManager);
    }
}
