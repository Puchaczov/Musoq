using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

public sealed class EvaluationFixture
{
    private readonly EvaluationRow[] _rows;

    public EvaluationFixture(params string[] names)
        : this(new GenericSchemaProvider(new Dictionary<string, ISchema>()), names)
    {
    }

    public EvaluationFixture(ISchemaProvider otherSources, params string[] names)
    {
        ArgumentNullException.ThrowIfNull(otherSources);
        ArgumentNullException.ThrowIfNull(names);
        _rows = names.Select((name, index) => new EvaluationRow(name, index * 10, new EvaluationCounter())
        {
            Id = index + 1,
            NullableValue = 2
        }).ToArray();
        var source = new EntitySource<EvaluationRow>([_rows], new Dictionary<string, int>(),
            new Dictionary<int, Func<EvaluationRow, object?>>());
        var schema = new GenericSchema<EvaluationLibrary>(
            new Dictionary<string, (ISchemaTable, object)>(StringComparer.OrdinalIgnoreCase)
            {
                ["entities"] = (new EvaluationTable(), source)
            });
        Provider = new ProviderWithOtherSources(schema, otherSources);
    }

    public ISchemaProvider Provider { get; }
    public int Calls => _rows.Sum(row => row.Counter.Calls);

    public sealed record EvaluationRow(string Name, int SequenceBase, EvaluationCounter Counter)
    {
        public int Id { get; init; }
        public int? NullableValue { get; init; }
    }

    private sealed class ProviderWithOtherSources(ISchema schema, ISchemaProvider otherSources) : ISchemaProvider
    {
        public ISchema GetSchema(string name) => name == "#A" ? schema : otherSources.GetSchema(name);
    }

    public sealed class EvaluationCounter
    {
        private int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public int Next() => Interlocked.Increment(ref _calls);
    }

    public sealed class EvaluationLibrary : LibraryBase
    {
        private int _groupCalls;

        [BindableMethod]
        [NonDeterministic]
        public int NextTestValue([InjectSpecificSource(typeof(EvaluationRow))] EvaluationRow row) =>
            row.SequenceBase + row.Counter.Next();

        [BindableMethod]
        [NonDeterministic]
        public int NextGroupValue() => Interlocked.Increment(ref _groupCalls);
    }

    private sealed class EvaluationTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn("Name", 0, typeof(string)),
            new SchemaColumn("Id", 1, typeof(int)),
            new SchemaColumn("NullableValue", 2, typeof(int?))
        ];
        public ISchemaColumn? GetColumnByName(string name) => Columns.SingleOrDefault(column => column.ColumnName == name);
        public ISchemaColumn[] GetColumnsByName(string name) => Columns.Where(column => column.ColumnName == name).ToArray();
        public SchemaTableMetadata Metadata { get; } = new(typeof(EvaluationRow));
    }
}
