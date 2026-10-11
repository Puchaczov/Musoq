using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks.Schema;

public class GenericSchema<T, TTable> : SchemaBase
{
    private readonly IEnumerable<IReadOnlyList<T>> _chunks;
    public GenericSchema(IEnumerable<IReadOnlyList<T>> chunks, IDictionary<string, int> testNameToIndexMap,
        IDictionary<int, Func<T, object?>> testIndexToObjectAccessMap)
        : base("test", CreateLibrary())
    {
        _chunks = chunks;
        AddSource<EntitySource<T>>("entities", chunks, testNameToIndexMap, testIndexToObjectAccessMap);
        AddTable<TTable>("entities");
    }

    public override RowSource<TRow> GetRowSource<TRow, TArguments>(string name,
        Musoq.Schema.Optimization.SourceExecutionContext context, TArguments arguments)
    {
        if (!name.Equals("entities", StringComparison.OrdinalIgnoreCase) || arguments.Count != 0)
            throw new NotSupportedException(name);
        return EnsureSourceType<TRow, T>(name, new EntitySource<T>(
            _chunks, new Dictionary<string, int>(), new Dictionary<int, Func<T, object?>>()));
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();

        var lib = new Library();

        methodManager.RegisterLibraries(lib);

        return new MethodsAggregator(methodManager);
    }
}
