#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
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

        public static readonly IReadOnlyDictionary<int, Func<BinaryEntity, object>> IndexToObjectAccessMap =
            new Dictionary<int, Func<BinaryEntity, object>>
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

        public static readonly IReadOnlyDictionary<int, Func<TextEntity, object>> IndexToObjectAccessMap =
            new Dictionary<int, Func<TextEntity, object>>
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
    protected class BinarySchema : SchemaBase
    {
        private static readonly Lazy<MethodsAggregator> CachedLibrary = new(CreateLibrary);
        private readonly IEnumerable<BinaryEntity> _entities;

        public BinarySchema(IEnumerable<BinaryEntity> entities)
            : base("test", CachedLibrary.Value)
        {
            _entities = entities;
        }

        public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext,
            params object[] parameters)
        {
            return new BinaryEntityTable();
        }

        public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            return new EntitySource<BinaryEntity>(
                _entities,
                BinaryEntity.NameToIndexMap,
                BinaryEntity.IndexToObjectAccessMap);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodManager = new MethodsManager();
            return new MethodsAggregator(methodManager);
        }
    }

    /// <summary>
    ///     Schema for text entities with string content.
    /// </summary>
    protected class TextSchema : SchemaBase
    {
        private static readonly Lazy<MethodsAggregator> CachedLibrary = new(CreateLibrary);
        private readonly IEnumerable<TextEntity> _entities;

        public TextSchema(IEnumerable<TextEntity> entities)
            : base("test", CachedLibrary.Value)
        {
            _entities = entities;
        }

        public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext,
            params object[] parameters)
        {
            return new TextEntityTable();
        }

        public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            return new EntitySource<TextEntity>(
                _entities,
                TextEntity.NameToIndexMap,
                TextEntity.IndexToObjectAccessMap);
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
    protected class BinarySchemaProvider : ISchemaProvider
    {
        private readonly IDictionary<string, IEnumerable<BinaryEntity>> _values;

        public BinarySchemaProvider(IDictionary<string, IEnumerable<BinaryEntity>> values)
        {
            _values = values;
        }

        public ISchema GetSchema(string schema)
        {
            if (_values.TryGetValue(schema, out var entities)) return new BinarySchema(entities);
            throw new InvalidOperationException($"Schema '{schema}' not found");
        }
    }

    /// <summary>
    ///     Schema provider for text entities.
    /// </summary>
    protected class TextSchemaProvider : ISchemaProvider
    {
        private readonly IDictionary<string, IEnumerable<TextEntity>> _values;

        public TextSchemaProvider(IDictionary<string, IEnumerable<TextEntity>> values)
        {
            _values = values;
        }

        public ISchema GetSchema(string schema)
        {
            if (_values.TryGetValue(schema, out var entities)) return new TextSchema(entities);
            throw new InvalidOperationException($"Schema '{schema}' not found");
        }
    }

    /// <summary>
    ///     Simple schema column implementation.
    /// </summary>
    protected class SchemaColumn : ISchemaColumn
    {
        public SchemaColumn(string columnName, int columnIndex, Type columnType)
        {
            ColumnName = columnName;
            ColumnIndex = columnIndex;
            ColumnType = columnType;
        }

        public string ColumnName { get; }
        public int ColumnIndex { get; }
        public Type ColumnType { get; }
    }

    #endregion
}
