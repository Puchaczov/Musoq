using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Examples.DataSources.StructuredInputs;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Tests;

public sealed class SpecStressCteCollectionSchemaProvider : ISchemaProvider
{
    public ISchema GetSchema(string _) => new SpecStressCteCollectionSchema();
}

public sealed class SpecStressCteCollectionSchema : SchemaBase
{
    public SpecStressCteCollectionSchema()
        : base("specstresscte", CreateLibrary())
    {
        AddTable<StructuredInputTable<SpecStressCteCollectionRow>>("capture");
        AddTypedSource<SpecStressCteCollectionSource>("capture");
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methodsManager);
    }
}

public readonly record struct SpecStressCteCollectionRow(int? Value);

public sealed class SpecStressCteCollectionSource : RowSource<SpecStressCteCollectionRow>
{
    private readonly IReadOnlyList<int> _values;
    private readonly SourceExecutionContext _context;

    public SpecStressCteCollectionSource(IReadOnlyList<int> values, SourceExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = values;
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override IEnumerable<IReadOnlyList<SpecStressCteCollectionRow>> Chunks
    {
        get
        {
            _context.EndWorkToken.ThrowIfCancellationRequested();
            yield return _values.Select(static value => new SpecStressCteCollectionRow(value)).ToArray();
        }
    }
}
