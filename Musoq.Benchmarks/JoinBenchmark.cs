using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
public class JoinBenchmark
{
    public enum JoinType
    {
        Inner,
        Left,
        Right
    }

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _query = null!;

    [Params(100, 1000, 10000)] public int RowsCount { get; set; }

    [Params(JoinType.Inner, JoinType.Left, JoinType.Right)]
    public JoinType Type { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var joinClause = "";
        switch (Type)
        {
            case JoinType.Inner:
                joinClause = "inner join";
                break;
            case JoinType.Left:
                joinClause = "left outer join";
                break;
            case JoinType.Right:
                joinClause = "right outer join";
                break;
        }

        var script = $@"
                select 
                    a.Name, 
                    b.Country
                from #test.entities() a
                {joinClause} #test.entities() b on a.Id = b.Id";

        var entities = Enumerable.Range(0, RowsCount).Select(i => new MyEntity
        {
            Id = i,
            Name = $"Name{i}",
            Country = $"Country{i}",
            City = $"City{i}"
        }).ToList();

        var schemaProvider = new MySchemaProvider(entities);

        _query = InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);
    }

    [Benchmark]
    public Table RunQuery()
    {
        return _query.Run();
    }

    private class MyEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int Id { get; set; }
    }

    private class MySchemaProvider : ISchemaProvider
    {
        private readonly IEnumerable<MyEntity> _entities;

        public MySchemaProvider(IEnumerable<MyEntity> entities)
        {
            _entities = entities;
        }

        public ISchema GetSchema(string schema)
        {
            return new MySchema(_entities);
        }
    }

    private class MySchema : SchemaBase
    {
        private readonly IEnumerable<MyEntity> _entities;

        public MySchema(IEnumerable<MyEntity> entities) : base("test", CreateLibrary())
        {
            _entities = entities;
        }

        public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext,
            params object[] parameters)
        {
            return new MyTable();
        }

        public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            return new EntitySource<MyEntity>(_entities, new Dictionary<string, int>
            {
                { nameof(MyEntity.Name), 0 },
                { nameof(MyEntity.Country), 1 },
                { nameof(MyEntity.City), 2 },
                { nameof(MyEntity.Id), 3 }
            }, new Dictionary<int, Func<MyEntity, object>>
            {
                { 0, e => e.Name },
                { 1, e => e.Country },
                { 2, e => e.City },
                { 3, e => e.Id }
            });
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodManager = new MethodsManager();
            var lib = new Library();
            methodManager.RegisterLibraries(lib);
            return new MethodsAggregator(methodManager);
        }
    }

    private class MyTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => new ISchemaColumn[]
        {
            new SchemaColumn(nameof(MyEntity.Name), 0, typeof(string)),
            new SchemaColumn(nameof(MyEntity.Country), 1, typeof(string)),
            new SchemaColumn(nameof(MyEntity.City), 2, typeof(string)),
            new SchemaColumn(nameof(MyEntity.Id), 3, typeof(int))
        };

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.First(c => c.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(c => c.ColumnName == name).ToArray();
        }

        public SchemaTableMetadata Metadata { get; } = new(typeof(MyEntity));
    }

    private class Library : LibraryBase
    {
    }
}