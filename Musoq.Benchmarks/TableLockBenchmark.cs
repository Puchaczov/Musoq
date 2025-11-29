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
using Musoq.Benchmarks.Schema;
using Musoq.Plugins.Attributes;

namespace Musoq.Benchmarks
{
    /// <summary>
    /// Benchmark to measure Table.Add performance under parallel execution.
    /// Tests the impact of lock-free vs lock-based collection access.
    /// </summary>
    [ShortRunJob]
    [MemoryDiagnoser]
    public class TableLockBenchmark
    {
        private CompiledQuery _sequentialQuery = null!;
        private CompiledQuery _parallelQuery = null!;
        private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();

        [Params(10_000, 100_000)]
        public int RowsCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            // Create test data
            var testData = Enumerable.Range(0, RowsCount).Select(i => new TableTestEntity
            {
                Id = i,
                Name = $"Name{i}",
                Value = i * 10,
                Category = $"Category{i % 10}"
            }).ToList();

            var schemaProvider = new TableTestSchemaProvider(testData);

            // Sequential query - uses regular foreach
            _sequentialQuery = InstanceCreator.CompileForExecution(
                @"select Id, Name, Value, Category, HeavyComputation(Value) from #test.entities() where Value > 100",
                Guid.NewGuid().ToString(),
                schemaProvider,
                _loggerResolver,
                new CompilationOptions(parallelizationMode: ParallelizationMode.None));

            // Parallel query - uses Parallel.ForEach
            _parallelQuery = InstanceCreator.CompileForExecution(
                @"select Id, Name, Value, Category, HeavyComputation(Value) from #test.entities() where Value > 100",
                Guid.NewGuid().ToString(),
                schemaProvider,
                _loggerResolver,
                new CompilationOptions(parallelizationMode: ParallelizationMode.Full));
        }

        [Benchmark(Baseline = true)]
        public void Sequential_TableAdd()
        {
            _sequentialQuery.Run();
        }

        [Benchmark]
        public void Parallel_TableAdd()
        {
            _parallelQuery.Run();
        }
    }

    public class BenchmarkLibrary : LibraryBase
    {
        [BindableMethod]
        public int HeavyComputation(int value)
        {
            double result = value;
            for (int i = 0; i < 1000; i++)
            {
                result = Math.Sqrt(result * result + i + Math.Sin(i));
            }
            return (int)result;
        }
    }

    public class TableTestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public class TableTestSchemaProvider : ISchemaProvider
    {
        private readonly List<TableTestEntity> _entities;

        public TableTestSchemaProvider(List<TableTestEntity> entities)
        {
            _entities = entities;
        }

        public ISchema GetSchema(string schema)
        {
            return new TableTestSchema(_entities);
        }
    }

    public class TableTestSchema : SchemaBase
    {
        private readonly List<TableTestEntity> _entities;

        public TableTestSchema(List<TableTestEntity> entities) : base("test", CreateMethods())
        {
            _entities = entities;
        }

        public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            return new TableTestTable();
        }

        public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            return new TableTestRowSource(_entities);
        }

        private static MethodsAggregator CreateMethods()
        {
            var methodManager = new MethodsManager();
            methodManager.RegisterLibraries(new LibraryBase());
            methodManager.RegisterLibraries(new BenchmarkLibrary());
            return new MethodsAggregator(methodManager);
        }
    }

    public class TableTestTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => new ISchemaColumn[]
        {
            new TableTestColumn("Id", 0, typeof(int)),
            new TableTestColumn("Name", 1, typeof(string)),
            new TableTestColumn("Value", 2, typeof(int)),
            new TableTestColumn("Category", 3, typeof(string))
        };

        public SchemaTableMetadata Metadata => new(typeof(TableTestEntity));
        
        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.FirstOrDefault(c => c.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(c => c.ColumnName == name).ToArray();
        }
    }

    public class TableTestRowSource : RowSource
    {
        private readonly List<TableTestEntity> _entities;

        public TableTestRowSource(List<TableTestEntity> entities)
        {
            _entities = entities;
        }

        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                foreach (var entity in _entities)
                {
                    yield return new TableTestObjectResolver(entity);
                }
            }
        }
    }

    public class TableTestObjectResolver : IObjectResolver
    {
        private readonly TableTestEntity _entity;

        public TableTestObjectResolver(TableTestEntity entity)
        {
            _entity = entity;
        }

        public object[] Contexts => Array.Empty<object>();

        public bool HasColumn(string name) => name switch
        {
            "Id" or "Name" or "Value" or "Category" => true,
            _ => false
        };

        public object this[string name] => name switch
        {
            "Id" => _entity.Id,
            "Name" => _entity.Name,
            "Value" => _entity.Value,
            "Category" => _entity.Category,
            _ => throw new KeyNotFoundException($"Column '{name}' not found")
        };

        public object this[int index] => index switch
        {
            0 => _entity.Id,
            1 => _entity.Name,
            2 => _entity.Value,
            3 => _entity.Category,
            _ => throw new IndexOutOfRangeException($"Index {index} is out of range")
        };
    }

    public class TableTestColumn : ISchemaColumn
    {
        public TableTestColumn(string columnName, int columnIndex, Type columnType)
        {
            ColumnName = columnName;
            ColumnIndex = columnIndex;
            ColumnType = columnType;
        }

        public string ColumnName { get; }
        public int ColumnIndex { get; }
        public Type ColumnType { get; }
    }
}
