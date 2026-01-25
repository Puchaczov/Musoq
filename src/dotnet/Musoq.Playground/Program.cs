using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Musoq.Converter;
using Musoq.Converter.Build;
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
            var script = @"
                select 
                    1
                from #test.entities() a
                inner join #test.entities() b on a.Population > b.Population";

            var entities = Enumerable.Range(0, 10).Select(i => new NonEquiEntity 
            { 
                Id = i, 
                Name = $"Name{i}", 
                Population = i
            }).ToList();

            var schemaProvider = new NonEquiSchemaProvider(entities);
            
            try 
            {
                var items = new BuildItems
                {
                    SchemaProvider = schemaProvider,
                    RawQuery = script,
                    AssemblyName = Guid.NewGuid().ToString(),
                    CreateBuildMetadataAndInferTypesVisitor = null,
                    CompilationOptions = new CompilationOptions(useSortMergeJoin: true)
                };

                Musoq.Evaluator.Runtime.RuntimeLibraries.CreateReferences();

                var chain = new CreateTree(
                    new TransformTree(
                        new TurnQueryIntoRunnableCode(null), new MyLoggerResolver()));

                chain.Build(items);

                foreach (var tree in items.Compilation.SyntaxTrees)
                {
                    Console.WriteLine(tree);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
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

    public class Library : LibraryBase {}

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

    public class MyLoggerResolver : ILoggerResolver
    {
        public ILogger ResolveLogger() => new NoOpLogger();
        public ILogger<T> ResolveLogger<T>() => new NoOpLogger<T>();
    }

    public class NoOpLogger : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {}
        public bool IsEnabled(LogLevel logLevel) => false;
        public IDisposable BeginScope<TState>(TState state) => null;
    }

    public class NoOpLogger<T> : NoOpLogger, ILogger<T> {}
}
