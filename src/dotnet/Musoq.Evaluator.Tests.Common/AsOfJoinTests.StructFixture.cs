using System;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Examples.DataSources.StructuredInputs;

namespace Musoq.Evaluator.Tests;

public sealed class SpecStressAsofTieBreakSchemaProvider : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (!string.Equals(schema.TrimStart('#'), "stress", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(schema);

        return new SpecStressAsofTieBreakSchema();
    }
}

public sealed class SpecStressAsofTieBreakSchema : SchemaBase
{
    public SpecStressAsofTieBreakSchema()
        : base("stress", CreateLibrary())
    {
        AddTable<StructuredInputTable<SpecStressAsofTieBreakRow>>("rows");
        AddTypedSource<SpecStressAsofTieBreakSource>("rows");
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new SpecStressAsofTieBreakLibrary());
        return new MethodsAggregator(methodsManager);
    }
}

public readonly record struct SpecStressAsofTieBreakRow(int Id, int? KeyValue, int? Priority);

public sealed class SpecStressAsofTieBreakLibrary : LibraryBase
{
    [BindableMethod]
    public int ReadId([InjectSpecificSource(typeof(SpecStressAsofTieBreakRow))] SpecStressAsofTieBreakRow row) => row.Id;
}

public sealed class SpecStressAsofTieBreakSource : RowSource<SpecStressAsofTieBreakRow>
{
    private readonly string _side;
    private readonly SourceExecutionContext _context;

    public SpecStressAsofTieBreakSource(string side, SourceExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(side);
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _side = side;
    }

    public override IEnumerable<IReadOnlyList<SpecStressAsofTieBreakRow>> Chunks
    {
        get
        {
            _context.EndWorkToken.ThrowIfCancellationRequested();
            yield return _side switch
            {
                "left" => new[]
                {
                    new SpecStressAsofTieBreakRow(0, 2, null),
                    new SpecStressAsofTieBreakRow(1, 5, null),
                    new SpecStressAsofTieBreakRow(2, null, null),
                    new SpecStressAsofTieBreakRow(3, 7, null)
                },
                "right" => new[]
                {
                    new SpecStressAsofTieBreakRow(10, 3, 9),
                    new SpecStressAsofTieBreakRow(11, 3, 1),
                    new SpecStressAsofTieBreakRow(12, 4, 2),
                    new SpecStressAsofTieBreakRow(13, 4, null),
                    new SpecStressAsofTieBreakRow(14, 6, 1),
                    new SpecStressAsofTieBreakRow(15, 6, null)
                },
                "zero" => new[] { default(SpecStressAsofTieBreakRow) with { KeyValue = 0 } },
                _ => throw new NotSupportedException(_side)
            };
        }
    }
}
