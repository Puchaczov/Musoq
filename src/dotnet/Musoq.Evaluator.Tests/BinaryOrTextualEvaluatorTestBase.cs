using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Base class providing shared infrastructure for BinaryOrTextual interpretation E2E tests.
/// </summary>
public abstract class BinaryOrTextualEvaluatorTestBase
{
    protected static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();

    protected static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    private readonly object _batchedQueryGate = new();
    private readonly List<CompiledQuery> _batchedQueries = [];

    [Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanup]
    public void DisposeBatchedCompiledQueries()
    {
        CompiledQuery[] queries;
        lock (_batchedQueryGate)
        {
            queries = _batchedQueries.ToArray();
            _batchedQueries.Clear();
        }

        foreach (var query in queries)
            query.Dispose();
    }

    protected CompiledQuery CompileGeneratedQuery(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions compilationOptions)
    {
        _ = assemblyName;
        var result = StableTypedExecutionCompilationCoordinator.Submit(
            script,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            consumerFamily: "stable-interpretation-specification",
            batchOrigin: "binary-textual-specification");

        if (!result.Result.Succeeded)
        {
            throw result.Result.CaughtException != null
                ? new MusoqQueryException(result.Result.ToEnvelopes(), result.Result.CaughtException)
                : new MusoqQueryException(result.Result.ToEnvelopes());
        }

        if (result.WasBatched)
        {
            lock (_batchedQueryGate)
                _batchedQueries.Add(result.Result.CompiledQuery);
        }

        return result.Result.CompiledQuery;
    }

    #region Test Entities and Schema Infrastructure

    /// <summary>
    ///     Entity with binary content for testing Interpret().
    /// </summary>
    public class BinaryEntity
    {
        public static readonly IReadOnlyDictionary<string, int> NameToIndexMap = new Dictionary<string, int>
        {
            { nameof(Name), 0 },
            { nameof(Content), 1 }
        };

        public static readonly IReadOnlyDictionary<int, Func<BinaryEntity, object?>> IndexToObjectAccessMap =
            new Dictionary<int, Func<BinaryEntity, object?>>
            {
                { 0, e => e.Name },
                { 1, e => e.Content }
            };

        public string Name { get; set; } = string.Empty;
        public byte[] Content { get; set; } = [];
    }

    /// <summary>
    ///     Entity with text content for testing Parse().
    /// </summary>
    public class TextEntity
    {
        public static readonly IReadOnlyDictionary<string, int> NameToIndexMap = new Dictionary<string, int>
        {
            { nameof(Name), 0 },
            { nameof(Text), 1 },
            { nameof(Line), 1 } // Alias for Text
        };

        public static readonly IReadOnlyDictionary<int, Func<TextEntity, object?>> IndexToObjectAccessMap =
            new Dictionary<int, Func<TextEntity, object?>>
            {
                { 0, e => e.Name },
                { 1, e => e.Text }
            };

        public string Name { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Line => Text;
    }

    /// <summary>
    ///     Table for binary entities.
    /// </summary>
    protected class BinaryEntityTable : ISchemaTable
    {
        public ISchemaColumn[] Columns =>
        [
            new SchemaColumn(nameof(BinaryEntity.Name), 0, typeof(string)),
            new SchemaColumn(nameof(BinaryEntity.Content), 1, typeof(byte[]))
        ];

        public SchemaTableMetadata Metadata => new(typeof(BinaryEntity));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Array.Find(Columns, c => c.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Array.FindAll(Columns, c => c.ColumnName == name);
        }
    }

    /// <summary>
    ///     Table for text entities.
    /// </summary>
    protected class TextEntityTable : ISchemaTable
    {
        public ISchemaColumn[] Columns =>
        [
            new SchemaColumn(nameof(TextEntity.Name), 0, typeof(string)),
            new SchemaColumn(nameof(TextEntity.Text), 1, typeof(string)),
            new SchemaColumn(nameof(TextEntity.Line), 1, typeof(string))
        ];

        public SchemaTableMetadata Metadata => new(typeof(TextEntity));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Array.Find(Columns, c => c.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Array.FindAll(Columns, c => c.ColumnName == name);
        }
    }

    /// <summary>
    ///     Schema for binary entities with byte[] content.
    /// </summary>
    protected class BinarySchema(IEnumerable<BinaryEntity> entities) : SchemaBase("test", CachedLibrary.Value)
    {
        private readonly IReadOnlyList<BinaryEntity> _entities = entities as IReadOnlyList<BinaryEntity> ?? entities.ToArray();
        private static readonly Lazy<MethodsAggregator> CachedLibrary = new(CreateLibrary);

        public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            return new BinaryEntityTable();
        }

        public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
        {
            return EnsureSourceType<T, BinaryEntity>(name, new EntitySource<BinaryEntity>(
                [_entities],
                BinaryEntity.NameToIndexMap,
                BinaryEntity.IndexToObjectAccessMap));
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodManager = new MethodsManager();
            methodManager.RegisterLibraries(new LibraryBase());
            return new MethodsAggregator(methodManager);
        }
    }

    /// <summary>
    ///     Schema for text entities with string content.
    /// </summary>
    protected class TextSchema(IEnumerable<TextEntity> entities) : SchemaBase("test", CachedLibrary.Value)
    {
        private readonly IReadOnlyList<TextEntity> _entities = entities as IReadOnlyList<TextEntity> ?? entities.ToArray();
        private static readonly Lazy<MethodsAggregator> CachedLibrary = new(CreateLibrary);

        public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            return new TextEntityTable();
        }

        public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
        {
            return EnsureSourceType<T, TextEntity>(name, new EntitySource<TextEntity>(
                [_entities],
                TextEntity.NameToIndexMap,
                TextEntity.IndexToObjectAccessMap));
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodManager = new MethodsManager();
            methodManager.RegisterLibraries(new LibraryBase());
            return new MethodsAggregator(methodManager);
        }
    }

    /// <summary>
    ///     Schema provider for binary entities.
    /// </summary>
    protected class BinarySchemaProvider(IDictionary<string, IEnumerable<BinaryEntity>> values) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (values.TryGetValue(schema, out var entities)) return new BinarySchema(entities);
            throw new InvalidOperationException($"Schema '{schema}' not found");
        }
    }

    /// <summary>
    ///     Schema provider for text entities.
    /// </summary>
    protected class TextSchemaProvider(IDictionary<string, IEnumerable<TextEntity>> values) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (values.TryGetValue(schema, out var entities)) return new TextSchema(entities);
            throw new InvalidOperationException($"Schema '{schema}' not found");
        }
    }

    /// <summary>
    ///     Simple schema column implementation.
    /// </summary>
    protected class SchemaColumn(string columnName, int columnIndex, Type columnType) : ISchemaColumn
    {
        public string ColumnName { get; } = columnName;
        public int ColumnIndex { get; } = columnIndex;
        public Type ColumnType { get; } = columnType;
    }

    #endregion
}
