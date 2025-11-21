using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Musoq.Converter;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Plugins;
using Musoq.Evaluator;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Musoq.Playground
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "benchmark")
            {
                BenchmarkRunner.Run<QueryBenchmark>();
                return;
            }

            var script = @"
                select 
                    a.Name, 
                    b.Country
                from #test.entities() a
                left outer join #test.entities() b on a.Id = b.Id";

            var schemaProvider = new MySchemaProvider(new List<MyEntity>());
            
            try 
            {
                var buildItems = InstanceCreator.CreateForAnalyze(
                    script, 
                    Guid.NewGuid().ToString(), 
                    schemaProvider, 
                    new MyLoggerResolver());

                foreach (var tree in buildItems.Compilation.SyntaxTrees)
                {
                    Console.WriteLine(tree.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    [MemoryDiagnoser]
    public class QueryBenchmark
    {
        private CompiledQuery _query;
        
        [GlobalSetup]
        public void Setup()
        {
            var script = @"
                select 
                    a.Name, 
                    b.Country
                from #test.entities() a
                left outer join #test.entities() b on a.Id = b.Id";

            var entities = Enumerable.Range(0, 100).Select(i => new MyEntity 
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
                new MyLoggerResolver());
        }

        [Benchmark]
        public void RunQuery()
        {
            _query.Run();
        }
    }

    public class Library : LibraryBase
    {
    }

    public class MyEntity
    {
        public string Name { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public int Id { get; set; }
    }

    public class MySchemaProvider : ISchemaProvider
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

    public class MySchema : SchemaBase
    {
        private readonly IEnumerable<MyEntity> _entities;

        public MySchema(IEnumerable<MyEntity> entities) : base("test", CreateLibrary())
        {
            _entities = entities;
        }
        
        public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
             return new MyTable();
        }

        public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            return new EntitySource<MyEntity>(_entities, new Dictionary<string, int>
            {
                {nameof(MyEntity.Name), 0},
                {nameof(MyEntity.Country), 1},
                {nameof(MyEntity.City), 2},
                {nameof(MyEntity.Id), 3},
            }, new Dictionary<int, Func<MyEntity, object>>
            {
                {0, e => e.Name},
                {1, e => e.Country},
                {2, e => e.City},
                {3, e => e.Id},
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
    
    public class MyTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => new ISchemaColumn[]
        {
            new SchemaColumn(nameof(MyEntity.Name), 0, typeof(string)),
            new SchemaColumn(nameof(MyEntity.Country), 1, typeof(string)),
            new SchemaColumn(nameof(MyEntity.City), 2, typeof(string)),
            new SchemaColumn(nameof(MyEntity.Id), 3, typeof(int)),
        };

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.First(c => c.ColumnName == name);
        }
        
        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(c => c.ColumnName == name).ToArray();
        }

        public SchemaTableMetadata Metadata { get; } = new SchemaTableMetadata(typeof(MyEntity));
    }

    public class MyLoggerResolver : ILoggerResolver
    {
        public ILogger ResolveLogger()
        {
            return new NoOpLogger();
        }

        public ILogger<T> ResolveLogger<T>()
        {
            return new NoOpLogger<T>();
        }
    }

    public class NoOpLogger : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }

    public class NoOpLogger<T> : NoOpLogger, ILogger<T>
    {
    }
}
