using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Benchmarks
{
    [MemoryDiagnoser]
    public class NonEquiJoinBenchmark
    {
        private CompiledQuery _query = null!;
        private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();

        [Params(1000, 2000)]
        public int RowsCount { get; set; }

        [Params(true, false)]
        public bool UseSortMergeJoin { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var script = @"
                select 
                    1
                from #test.entities() a
                inner join #test.entities() b on a.Population > b.Population";

            var entities = Enumerable.Range(0, RowsCount).Select(i => new NonEquiEntity 
            { 
                Id = i, 
                Name = $"Name{i}", 
                Population = i
            }).ToList();

            var schemaProvider = new NonEquiSchemaProvider(entities);
            
            _query = InstanceCreator.CompileForExecution(
                script, 
                Guid.NewGuid().ToString(), 
                schemaProvider, 
                _loggerResolver,
                new CompilationOptions(useSortMergeJoin: UseSortMergeJoin));
        }

        [Benchmark]
        public Table RunQuery()
        {
            return _query.Run();
        }
    }

    public class NonEquiEntity
    {
        public string Name { get; set; } = string.Empty;
        public int Population { get; set; }
        public int Id { get; set; }
    }

    public class NonEquiSchemaProvider : ISchemaProvider
    {
        private readonly IEnumerable<NonEquiEntity> _entities;

        public NonEquiSchemaProvider(IEnumerable<NonEquiEntity> entities)
        {
            _entities = entities;
        }

        public ISchema GetSchema(string schema)
        {
            return new NonEquiSchema(_entities);
        }
    }

    public class NonEquiSchema : SchemaBase
    {
        private readonly IEnumerable<NonEquiEntity> _entities;

        public NonEquiSchema(IEnumerable<NonEquiEntity> entities) : base("test", CreateLibrary())
        {
            _entities = entities;
        }

        public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            return new NonEquiTable();
        }

        public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            return new EntitySource<NonEquiEntity>(_entities, new Dictionary<string, int>
            {
                { nameof(NonEquiEntity.Id), 0 },
                { nameof(NonEquiEntity.Name), 1 },
                { nameof(NonEquiEntity.Population), 2 }
            }, new Dictionary<int, Func<NonEquiEntity, object>>
            {
                { 0, e => e.Id },
                { 1, e => e.Name },
                { 2, e => e.Population }
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

    public class NonEquiTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => new ISchemaColumn[]
        {
            new SchemaColumn(nameof(NonEquiEntity.Id), 0, typeof(int)),
            new SchemaColumn(nameof(NonEquiEntity.Name), 1, typeof(string)),
            new SchemaColumn(nameof(NonEquiEntity.Population), 2, typeof(int))
        };

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.Single(c => c.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(c => c.ColumnName == name).ToArray();
        }

        public SchemaTableMetadata Metadata { get; } = new SchemaTableMetadata(typeof(NonEquiEntity));
    }
}
