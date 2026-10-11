using System;
using System.Collections.Generic;
using Musoq.Examples.DataSources.StructuredInputs;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Tests;

public readonly record struct SpecStressCoupledRow(string Name);

public sealed class SpecStressCoupledSchemaProvider : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (!string.Equals(schema.TrimStart('#'), "A", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(schema);

        return new SpecStressCoupledSchema();
    }
}

public sealed class SpecStressCoupledSchema : SchemaBase
{
    public SpecStressCoupledSchema()
        : base("A", CreateLibrary())
    {
        AddTable<StructuredInputTable<SpecStressCoupledRow>>("Entities");
        AddTypedSource<SpecStressCoupledSource>("Entities");
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methodsManager);
    }
}

public sealed class SpecStressCoupledSource : RowSource<SpecStressCoupledRow>
{
    private readonly bool _enabled;
    private readonly string _displayName;
    private readonly SourceExecutionContext _context;

    public SpecStressCoupledSource(bool enabled, string displayName, SourceExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(displayName);
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _enabled = enabled;
        _displayName = displayName;
    }

    public override IEnumerable<IReadOnlyList<SpecStressCoupledRow>> Chunks
    {
        get
        {
            _context.EndWorkToken.ThrowIfCancellationRequested();
            if (!_enabled)
                yield break;

            yield return [new SpecStressCoupledRow(_displayName)];
        }
    }
}
