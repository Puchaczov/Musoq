using System.Collections.Generic;
using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.StructuredInputs;

public sealed class MatchSource : RowSource<PatternMatchRow>
{
    private readonly string _text;
    private readonly IReadOnlyList<PatternInput> _patterns;
    private readonly SourceExecutionContext _context;

    public MatchSource(string text, IReadOnlyList<PatternInput> patterns, SourceExecutionContext context)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _patterns = patterns ?? throw new ArgumentNullException(nameof(patterns));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        StructuredInputSourceCounters.MatchCreated();
    }

    public override IEnumerable<IReadOnlyList<PatternMatchRow>> Chunks
    {
        get
        {
            _context.EndWorkToken.ThrowIfCancellationRequested();
            var rows = StructuredInputOperations.Match(_text, _patterns, _context.EndWorkToken);
            if (rows.Length > 0)
                yield return rows;
        }
    }
}

public sealed class ConfigureSource : RowSource<ConfigureRow>
{
    private readonly OptionsInput _options;
    private readonly SourceExecutionContext _context;

    public ConfigureSource(OptionsInput options, SourceExecutionContext context)
    {
        _options = options;
        _context = context ?? throw new ArgumentNullException(nameof(context));
        StructuredInputSourceCounters.ConfigureCreated();
    }

    public override IEnumerable<IReadOnlyList<ConfigureRow>> Chunks
    {
        get
        {
            _context.EndWorkToken.ThrowIfCancellationRequested();
            yield return [StructuredInputOperations.Configure(_options, _context.EndWorkToken)];
        }
    }
}

public sealed class NumbersSource : RowSource<NumberRow>
{
    private readonly IReadOnlyList<int> _values;
    private readonly SourceExecutionContext _context;

    public NumbersSource(IReadOnlyList<int> values, SourceExecutionContext context)
    {
        _values = values ?? throw new ArgumentNullException(nameof(values));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        StructuredInputSourceCounters.NumbersCreated();
    }

    public override IEnumerable<IReadOnlyList<NumberRow>> Chunks
    {
        get
        {
            _context.EndWorkToken.ThrowIfCancellationRequested();
            var rows = StructuredInputOperations.Numbers(_values, _context.EndWorkToken);
            if (rows.Length > 0)
                yield return rows;
        }
    }
}

public sealed class MatrixSource : RowSource<MatrixValueRow>
{
    private readonly IReadOnlyList<IReadOnlyList<int>> _values;
    private readonly SourceExecutionContext _context;

    public MatrixSource(IReadOnlyList<IReadOnlyList<int>> values, SourceExecutionContext context)
    {
        _values = values ?? throw new ArgumentNullException(nameof(values));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        StructuredInputSourceCounters.MatrixCreated();
    }

    public override IEnumerable<IReadOnlyList<MatrixValueRow>> Chunks
    {
        get
        {
            _context.EndWorkToken.ThrowIfCancellationRequested();
            var rows = StructuredInputOperations.Matrix(_values, _context.EndWorkToken);
            if (rows.Length > 0)
                yield return rows;
        }
    }
}

public sealed class WeightedSource : RowSource<WeightedRow>
{
    private readonly IReadOnlyList<WeightedInput> _items;
    private readonly SourceExecutionContext _context;

    public WeightedSource(IReadOnlyList<WeightedInput> items, SourceExecutionContext context)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        StructuredInputSourceCounters.WeightedCreated();
    }

    public override IEnumerable<IReadOnlyList<WeightedRow>> Chunks
    {
        get
        {
            _context.EndWorkToken.ThrowIfCancellationRequested();
            var rows = StructuredInputOperations.Weighted(_items, _context.EndWorkToken);
            if (rows.Length > 0)
                yield return rows;
        }
    }
}

/// <summary>Demonstrates deterministic selection between scalar and structural overloads.</summary>
public sealed class OverloadSource : RowSource<OverloadRow>
{
    private readonly OverloadRow _row;

    public OverloadSource(int value, SourceExecutionContext context)
    {
        _ = context ?? throw new ArgumentNullException(nameof(context));
        _row = new OverloadRow("numeric", value, 0);
        StructuredInputSourceCounters.OverloadCreated();
    }

    public OverloadSource(decimal value, SourceExecutionContext context)
    {
        _ = context ?? throw new ArgumentNullException(nameof(context));
        _row = new OverloadRow("decimal", decimal.ToInt32(value), 0);
        StructuredInputSourceCounters.OverloadCreated();
    }

    public OverloadSource(IReadOnlyList<PatternInput> patterns, SourceExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        _ = context ?? throw new ArgumentNullException(nameof(context));
        _row = new OverloadRow("structural", 0, patterns.Count);
        StructuredInputSourceCounters.OverloadCreated();
    }

    public override IEnumerable<IReadOnlyList<OverloadRow>> Chunks
    {
        get
        {
            yield return [_row];
        }
    }
}

/// <summary>Shows that a declared structural default wins over a receiver default.</summary>
public sealed class DefaultConflictSource : RowSource<DefaultConflictRow>
{
    private readonly DefaultConflictRow _row;

    public DefaultConflictSource(PatternInput input, SourceExecutionContext context)
    {
        _ = context ?? throw new ArgumentNullException(nameof(context));
        _row = new DefaultConflictRow(input.Id, input.Mode);
        StructuredInputSourceCounters.DefaultConflictCreated();
    }

    public override IEnumerable<IReadOnlyList<DefaultConflictRow>> Chunks
    {
        get
        {
            yield return [_row];
        }
    }
}

/// <summary>Returns the source context identity received by a typed constructor.</summary>
public sealed class ContextProbeSource : RowSource<ContextProbeRow>
{
    private readonly ContextProbeRow _row;

    public ContextProbeSource(SourceExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _row = new ContextProbeRow(context.QueryId, context.Plan.Identity.SourceContextId, context.Plan.Identity.Alias);
        StructuredInputSourceCounters.ContextProbeCreated();
    }

    public override IEnumerable<IReadOnlyList<ContextProbeRow>> Chunks
    {
        get
        {
            yield return [_row];
        }
    }
}

/// <summary>Source registered with deliberately small structural input limits.</summary>
public sealed class StrictLimitSource : RowSource<StrictLimitRow>
{
    private readonly IReadOnlyList<int> _values;
    private readonly SourceExecutionContext _context;

    public StrictLimitSource(IReadOnlyList<int> values, SourceExecutionContext context)
    {
        _values = values ?? throw new ArgumentNullException(nameof(values));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        StructuredInputSourceCounters.StrictCreated();
    }

    public override IEnumerable<IReadOnlyList<StrictLimitRow>> Chunks
    {
        get
        {
            _context.EndWorkToken.ThrowIfCancellationRequested();
            var rows = new StrictLimitRow[_values.Count];
            for (var index = 0; index < _values.Count; index++)
            {
                _context.EndWorkToken.ThrowIfCancellationRequested();
                rows[index] = new StrictLimitRow(_values[index]);
            }

            if (rows.Length > 0)
                yield return rows;
        }
    }
}

/// <summary>Mutates only its owned receiving object to expose cross-invocation sharing.</summary>
public sealed class MutableProbeSource : RowSource<MutableProbeRow>
{
    private readonly IReadOnlyList<MutableInput> _items;
    private readonly SourceExecutionContext _context;

    public MutableProbeSource(IReadOnlyList<MutableInput> items, SourceExecutionContext context)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        StructuredInputSourceCounters.MutableCreated();
    }

    public override IEnumerable<IReadOnlyList<MutableProbeRow>> Chunks
    {
        get
        {
            _context.EndWorkToken.ThrowIfCancellationRequested();
            if (_items.Count == 0)
                yield break;

            var item = _items[0] ?? throw new InvalidOperationException("Mutable input item cannot be null.");
            item.Value++;
            yield return [new MutableProbeRow(item.Value)];
        }
    }
}

/// <summary>Fixture whose constructor always throws after the engine reaches source use.</summary>
public sealed class ThrowingSource : RowSource<StrictLimitRow>
{
    public ThrowingSource(int value, SourceExecutionContext context)
    {
        _ = context ?? throw new ArgumentNullException(nameof(context));
        StructuredInputSourceCounters.ThrowingCreated();
        throw new InvalidOperationException($"Throwing source fixture rejected value {value}.");
    }

    public override IEnumerable<IReadOnlyList<StrictLimitRow>> Chunks => [];
}

/// <summary>Two optional-field shapes intentionally leave a single literal ambiguous.</summary>
public sealed class AmbiguousSource : RowSource<OverloadRow>
{
    private readonly OverloadRow _row;

    public AmbiguousSource(AmbiguousInputA input, SourceExecutionContext context)
    {
        _ = context ?? throw new ArgumentNullException(nameof(context));
        _row = new OverloadRow("a", 0, input.Value.Length);
    }

    public AmbiguousSource(AmbiguousInputB input, SourceExecutionContext context)
    {
        _ = context ?? throw new ArgumentNullException(nameof(context));
        _row = new OverloadRow("b", 0, input.Value.Length);
    }

    public override IEnumerable<IReadOnlyList<OverloadRow>> Chunks
    {
        get
        {
            yield return [_row];
        }
    }
}

/// <summary>Provides a collection-valued column that can collide with a CTE name.</summary>
public sealed class CollectionColumnSource : RowSource<CollectionColumnRow>
{
    private readonly SourceExecutionContext _context;

    public CollectionColumnSource(SourceExecutionContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override IEnumerable<IReadOnlyList<CollectionColumnRow>> Chunks
    {
        get
        {
            _context.EndWorkToken.ThrowIfCancellationRequested();
            yield return [new CollectionColumnRow([new PatternInput("column", "COLUMN")])];
        }
    }
}

/// <summary>Consumes a collection of objects for scalar/CTE interpretation ambiguity tests.</summary>
public sealed class RelationAmbiguitySource : RowSource<OverloadRow>
{
    private readonly IReadOnlyList<PatternInput> _items;
    private readonly SourceExecutionContext _context;

    public RelationAmbiguitySource(IReadOnlyList<PatternInput> items, SourceExecutionContext context)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override IEnumerable<IReadOnlyList<OverloadRow>> Chunks
    {
        get
        {
            _context.EndWorkToken.ThrowIfCancellationRequested();
            yield return [new OverloadRow("relation", 0, _items.Count)];
        }
    }
}
