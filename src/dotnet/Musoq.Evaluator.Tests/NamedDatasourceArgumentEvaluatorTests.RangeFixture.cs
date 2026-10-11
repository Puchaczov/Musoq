using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Examples.DataSources.StructuredInputs;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests;

public sealed class SpecStressSystemRangeSchemaProvider : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (!string.Equals(schema.TrimStart('#'), "system", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(schema);

        return new SpecStressSystemRangeSchema();
    }
}

public sealed class SpecStressSystemRangeSchema : SchemaBase
{
    public SpecStressSystemRangeSchema()
        : base("system", CreateLibrary())
    {
        AddTable<StructuredInputTable<SpecStressSystemRangeRow>>("range");
        AddTypedSource<SpecStressSystemRangeSource>("range");
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methodsManager);
    }
}

public readonly record struct SpecStressSystemRangeRow(int Value);

public sealed class SpecStressSystemRangeSource : RowSource<SpecStressSystemRangeRow>
{
    private readonly int _start;
    private readonly int _end;
    private readonly SourceExecutionContext _context;

    public SpecStressSystemRangeSource(int start, int end, SourceExecutionContext context)
    {
        _start = start;
        _end = end;
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override IEnumerable<IReadOnlyList<SpecStressSystemRangeRow>> Chunks
    {
        get
        {
            _context.EndWorkToken.ThrowIfCancellationRequested();
            yield return Enumerable.Range(_start, _end - _start)
                .Select(value => new SpecStressSystemRangeRow(value))
                .ToArray();
        }
    }
}

