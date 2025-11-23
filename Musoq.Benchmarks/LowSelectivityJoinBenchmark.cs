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
    public class LowSelectivityJoinBenchmark
    {
        private CompiledQuery _query = null!;
        private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();

        [Params(2000, 5000)]
        public int RowsCount { get; set; }

        [Params(true, false)]
        public bool UseSortMergeJoin { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            // Setup data for Low Selectivity.
            // Table A: 0..N
            // Table B: 0..N
            // Query: a.Population > b.Population + offset
            // Matches: Only top 5% of A will match bottom 5% of B (shifted by offset).
            
            var offset = (int)(RowsCount * 0.95);

            var entitiesA = Enumerable.Range(0, RowsCount).Select(i => new NonEquiEntity 
            { 
                Id = i, 
                Name = $"Name{i}", 
                Population = i
            }).ToList();

            var entitiesB = Enumerable.Range(0, RowsCount).Select(i => new NonEquiEntity 
            { 
                Id = i, 
                Name = $"Name{i}", 
                Population = i
            }).ToList();

            var schemaProvider = new LowSelectivitySchemaProvider(entitiesA, entitiesB);
            
            var script = $@"
                select 
                    1
                from #test.A() a
                inner join #test.B() b on a.Population > b.Population + {offset}";
            
            _query = InstanceCreator.CompileForExecution(
                script, 
                Guid.NewGuid().ToString(), 
                schemaProvider, 
                _loggerResolver,
                new CompilationOptions(useSortMergeJoin: UseSortMergeJoin)
            );
        }

        [Benchmark]
        public void RunQuery()
        {
            _query.Run();
        }
    }

    public class LowSelectivitySchemaProvider : ISchemaProvider
    {
        private readonly IEnumerable<NonEquiEntity> _entitiesA;
        private readonly IEnumerable<NonEquiEntity> _entitiesB;

        public LowSelectivitySchemaProvider(IEnumerable<NonEquiEntity> entitiesA, IEnumerable<NonEquiEntity> entitiesB)
        {
            _entitiesA = entitiesA;
            _entitiesB = entitiesB;
        }

        public ISchema GetSchema(string schema)
        {
            return new LowSelectivitySchema(_entitiesA, _entitiesB);
        }
    }

    public class LowSelectivitySchema : SchemaBase
    {
        private readonly IEnumerable<NonEquiEntity> _entitiesA;
        private readonly IEnumerable<NonEquiEntity> _entitiesB;

        public LowSelectivitySchema(IEnumerable<NonEquiEntity> entitiesA, IEnumerable<NonEquiEntity> entitiesB) : base("test", CreateLibrary())
        {
            _entitiesA = entitiesA;
            _entitiesB = entitiesB;
        }

        public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            return new NonEquiTable();
        }

        public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            var entities = name.Equals("A", StringComparison.OrdinalIgnoreCase) ? _entitiesA : _entitiesB;
            
            return new EntitySource<NonEquiEntity>(entities, new Dictionary<string, int>
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
}
