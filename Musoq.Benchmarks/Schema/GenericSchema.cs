using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks.Schema;

public class GenericSchema<T, TTable> : SchemaBase
{
    public GenericSchema(IEnumerable<T> sources, IDictionary<string, int> testNameToIndexMap, IDictionary<int, Func<T, object>> testIndexToObjectAccessMap)
        : base("test", CreateLibrary())
    {
        AddSource<EntitySource<T>>("entities", sources, testNameToIndexMap, testIndexToObjectAccessMap);
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