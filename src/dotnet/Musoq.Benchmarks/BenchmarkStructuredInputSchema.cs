using Musoq.Examples.DataSources.StructuredInputs;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

/// <summary>
/// Benchmark-only sources whose contracts expose nested and nullable
/// collection preparation.  They keep those cohorts on the same generated
/// execution path as the maintained example sources without changing the
/// public example datasource surface.
/// </summary>
public sealed class BenchmarkStructuredInputSchemaProvider : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (string.Equals(schema, BenchmarkStructuredInputSchema.SchemaName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(schema, $"#{BenchmarkStructuredInputSchema.SchemaName}", StringComparison.OrdinalIgnoreCase))
            return new BenchmarkStructuredInputSchema();

        throw new Musoq.Schema.Exceptions.SourceNotFoundException(
            $"Benchmark structural schema does not expose '{schema}'.");
    }
}

public sealed class BenchmarkStructuredInputSchema : SchemaBase
{
    public const string SchemaName = "benchmarkinputs";

    public BenchmarkStructuredInputSchema()
        : base(SchemaName, CreateLibrary())
    {
        AddTable<StructuredInputTable<BenchmarkOptionsRow>>("options");
        AddTypedSource<BenchmarkOptionsSource>("options");
        AddTable<StructuredInputTable<BenchmarkNullableRow>>("nullable");
        AddTypedSource<BenchmarkNullableSource>("nullable");
    }

    private static MethodsAggregator CreateLibrary()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(manager);
    }
}

public readonly record struct BenchmarkOptionsRow(
    bool Enabled,
    int CodeCount,
    int Before,
    int After);

public sealed class BenchmarkOptionsSource : RowSource<BenchmarkOptionsRow>
{
    private readonly IReadOnlyList<OptionsInput> _options;
    private readonly SourceExecutionContext _context;

    public BenchmarkOptionsSource(
        IReadOnlyList<OptionsInput> options,
        SourceExecutionContext context)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override IEnumerable<IReadOnlyList<BenchmarkOptionsRow>> Chunks
    {
        get
        {
            _context.EndWorkToken.ThrowIfCancellationRequested();
            var rows = new BenchmarkOptionsRow[_options.Count];
            for (var index = 0; index < rows.Length; index++)
            {
                _context.EndWorkToken.ThrowIfCancellationRequested();
                var option = _options[index];
                rows[index] = new BenchmarkOptionsRow(
                    option.Enabled,
                    option.Codes.Length,
                    option.Window.Before,
                    option.Window.After);
            }

            if (rows.Length > 0)
                yield return rows;
        }
    }
}

public readonly record struct BenchmarkNullableRow(int? Value);

public sealed class BenchmarkNullableSource : RowSource<BenchmarkNullableRow>
{
    private readonly IReadOnlyList<int?> _values;
    private readonly SourceExecutionContext _context;

    public BenchmarkNullableSource(
        IReadOnlyList<int?> values,
        SourceExecutionContext context)
    {
        _values = values ?? throw new ArgumentNullException(nameof(values));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override IEnumerable<IReadOnlyList<BenchmarkNullableRow>> Chunks
    {
        get
        {
            _context.EndWorkToken.ThrowIfCancellationRequested();
            var rows = new BenchmarkNullableRow[_values.Count];
            for (var index = 0; index < rows.Length; index++)
            {
                _context.EndWorkToken.ThrowIfCancellationRequested();
                rows[index] = new BenchmarkNullableRow(_values[index]);
            }

            if (rows.Length > 0)
                yield return rows;
        }
    }
}
