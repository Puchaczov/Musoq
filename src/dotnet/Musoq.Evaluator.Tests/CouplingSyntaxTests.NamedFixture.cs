using System;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Examples.DataSources.StructuredInputs;

namespace Musoq.Evaluator.Tests;

public sealed class SpecStressNamedMixSchemaProvider : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (!string.Equals(schema.TrimStart('#'), "specstressnamedmix", System.StringComparison.OrdinalIgnoreCase))
            throw new System.NotSupportedException(schema);

        return new SpecStressNamedMixSchema();
    }
}

public sealed class SpecStressNamedMixSchema : SchemaBase
{
    public SpecStressNamedMixSchema()
        : base("specstressnamedmix", new MethodsAggregator(new MethodsManager()))
    {
        AddTable<StructuredInputTable<SpecStressNamedMixRow>>("capture");
        AddTypedSource<SpecStressNamedMixSource>("capture");
    }
}

public readonly record struct SpecStressNamedMixRow(int Count, bool Enabled);

public sealed class SpecStressNamedMixSource : RowSource<SpecStressNamedMixRow>
{
    private readonly SpecStressNamedMixRow _row;
    private readonly SourceExecutionContext _context;

    public SpecStressNamedMixSource(int count, bool enabled, SourceExecutionContext context)
    {
        _row = new SpecStressNamedMixRow(count, enabled);
        _context = context ?? throw new System.ArgumentNullException(nameof(context));
    }

    public override IEnumerable<IReadOnlyList<SpecStressNamedMixRow>> Chunks
    {
        get
        {
            _context.EndWorkToken.ThrowIfCancellationRequested();
            yield return [_row];
        }
    }
}
