using System;
using System.Collections.Generic;
using Musoq.Examples.DataSources.StructuredInputs;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests;

public readonly record struct SpecStressAliasedCoupledRow(string Name, string City);

public sealed class SpecStressAliasedCoupledSchemaProvider : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (!string.Equals(schema.TrimStart('#'), "A", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(schema);

        return new SpecStressAliasedCoupledSchema();
    }
}

public sealed class SpecStressAliasedCoupledSchema : SchemaBase
{
    public SpecStressAliasedCoupledSchema()
        : base("A", CreateLibrary())
    {
        AddTable<StructuredInputTable<SpecStressAliasedCoupledRow>>("Entities");
        AddTypedSource<SpecStressAliasedCoupledSource>("Entities");
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methodsManager);
    }
}

public sealed class SpecStressAliasedCoupledSource : RowSource<SpecStressAliasedCoupledRow>
{
    private readonly bool _enabled;
    private readonly string _name;
    private readonly string _city;
    private readonly SourceExecutionContext _context;

    public SpecStressAliasedCoupledSource(bool enabled, string name, string city, SourceExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(city);
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _enabled = enabled;
        _name = name;
        _city = city;
    }

    public override IEnumerable<IReadOnlyList<SpecStressAliasedCoupledRow>> Chunks
    {
        get
        {
            _context.EndWorkToken.ThrowIfCancellationRequested();
            if (!_enabled)
                yield break;

            yield return [new SpecStressAliasedCoupledRow(_name, _city)];
        }
    }
}
