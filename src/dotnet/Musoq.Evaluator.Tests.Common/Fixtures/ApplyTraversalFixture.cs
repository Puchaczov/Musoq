using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

internal sealed class ApplyTraversalFixture
{
    private ApplyTraversalFixture(
        CountingApplySchema schema,
        CountingParent rejected,
        CountingParent kept,
        CountingParent empty,
        CountingParent nullable
    )
    {
        Schema = schema;
        Rejected = rejected;
        Kept = kept;
        Empty = empty;
        Nullable = nullable;
    }

    public CountingApplySchema Schema { get; }
    public CountingParent Rejected { get; }
    public CountingParent Kept { get; }
    public CountingParent Empty { get; }
    public CountingParent Nullable { get; }

    public static ApplyTraversalFixture Create()
    {
        var rejected = new CountingParent("reject", false, [new CountingChild(9, [new CountingGrandchild(90)])]);
        var kept = new CountingParent(
            "keep",
            true,
            [
                new CountingChild(1, [new CountingGrandchild(10), new CountingGrandchild(11)]),
                new CountingChild(2, [new CountingGrandchild(20)]),
            ]
        );
        var empty = new CountingParent("empty", false, []);
        var nullable = new CountingParent(null, null, [new CountingChild(3, [new CountingGrandchild(30)])]);
        var rows = new[] { rejected, kept, empty, nullable };
        var schema = new CountingApplySchema(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                ["parents"] = (new GenericEntityTable<CountingParent>(), new CountingParentRowSource(rows)),
            }
        );

        return new ApplyTraversalFixture(schema, rejected, kept, empty, nullable);
    }
}

internal sealed class CountingApplySchema(
    IReadOnlyDictionary<string, (ISchemaTable SchemaTable, object RowSource)> tables
) : GenericSchema<GenericLibrary>(tables)
{
    public int SourcePlanningRequestCount { get; private set; }

    public override SourcePlanResult TryPlanSource(string name, SourcePlanRequest request, params object?[] parameters)
    {
        SourcePlanningRequestCount++;
        return SourcePlanResult.RejectAll(request);
    }
}

internal sealed class CountingParentRowSource(IReadOnlyList<CountingParent> rows) : RowSourceBase<CountingParent>
{
    protected override void CollectChunks(IChunkWriter<CountingParent> writer) => writer.Write(rows);
}

public sealed class CountingEnumerable<T>(IReadOnlyList<T> values) : IEnumerable<T>
{
    private readonly IReadOnlyList<T> _values = values;

    public int EnumerationCount { get; private set; }

    public IEnumerator<T> GetEnumerator()
    {
        EnumerationCount++;
        return new CountingEnumerator<T>(_values.GetEnumerator());
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public T this[int index] => _values[index];

    public T Single() => _values.Single();
}

public sealed class CountingEnumerator<T>(IEnumerator<T> inner) : IEnumerator<T>
{
    public T Current => inner.Current;

    object IEnumerator.Current => Current!;

    public bool MoveNext() => inner.MoveNext();

    public void Reset() => inner.Reset();

    public void Dispose() => inner.Dispose();
}

public sealed class CountingParent(string? name, bool? enabled, IReadOnlyList<CountingChild> children)
{
    public string? Name { get; } = name;
    public bool? Enabled { get; } = enabled;
    public CountingEnumerable<CountingChild> Children { get; } = new(children);
}

public sealed class CountingChild(int x, IReadOnlyList<CountingGrandchild> other)
{
    public int X { get; } = x;
    public CountingEnumerable<CountingGrandchild> Other { get; } = new(other);
}

public sealed class CountingGrandchild(int value)
{
    public int Value { get; } = value;
}
