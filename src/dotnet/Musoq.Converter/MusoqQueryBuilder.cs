using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator;

namespace Musoq.Converter;

public sealed class MusoqQueryBuilder
{
    private readonly string _query;
    private readonly List<InMemorySourceSlot> _sources = [];
    private readonly Dictionary<string, MusoqSourceRows> _defaultRows = new(StringComparer.OrdinalIgnoreCase);
    private ILoggerResolver _loggerResolver = NullLoggerResolver.Instance;
    private CompilationOptions _compilationOptions = new();

    internal MusoqQueryBuilder(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or whitespace.", nameof(query));

        _query = query;
    }

    public MusoqQueryBuilder Source<T>(string schemaName, string sourceName)
    {
        AddOrValidateSource<T>(schemaName, sourceName);
        return this;
    }

    public MusoqQueryBuilder Source<T>(
        string schemaName,
        string sourceName,
        IEnumerable<IReadOnlyList<T>> chunks)
    {
        AddOrValidateSource<T>(schemaName, sourceName);
        var binding = MusoqSourceRows.Create(schemaName, sourceName, chunks);
        _defaultRows[binding.SchemaName + "." + binding.SourceName] = binding;
        return this;
    }

    public MusoqQueryBuilder WithLoggerResolver(ILoggerResolver loggerResolver)
    {
        _loggerResolver = loggerResolver ?? throw new ArgumentNullException(nameof(loggerResolver));
        return this;
    }

    public MusoqQueryBuilder WithCompilationOptions(CompilationOptions compilationOptions)
    {
        _compilationOptions = compilationOptions ?? throw new ArgumentNullException(nameof(compilationOptions));
        return this;
    }

    public ICompiledTypedQuery<TOut> Compile<TOut>()
    {
        var sourceBinding = CreateSourceBinding();
        var factory = InstanceCreator.CompileForTypedExecutionFactory<TOut>(
            _query,
            $"MusoqTyped_{Guid.NewGuid():N}",
            sourceBinding.CreateMetadataProvider(),
            _loggerResolver,
            _compilationOptions,
            sourceBinding.AdditionalReferenceTypes);

        return new PublicCompiledTypedQuery<TOut>(factory, sourceBinding);
    }

    public CompiledTypedQueryArtifact CompileArtifact<TOut>()
    {
        var sourceBinding = CreateSourceBinding();
        return InstanceCreator.CompileForTypedArtifact<TOut>(
            _query,
            $"MusoqTyped_{Guid.NewGuid():N}",
            sourceBinding.CreateMetadataProvider(),
            _loggerResolver,
            _compilationOptions,
            sourceBinding.AdditionalReferenceTypes,
            sourceBinding.Slots);
    }

    public TypedQueryInspectionResult InspectTyped<TOut>()
    {
        var sourceBinding = CreateSourceBinding();
        return InstanceCreator.CompileForTypedInspection<TOut>(
            _query,
            $"MusoqTypedInspection_{Guid.NewGuid():N}",
            sourceBinding.CreateMetadataProvider(),
            _loggerResolver,
            _compilationOptions,
            sourceBinding.AdditionalReferenceTypes);
    }

    public ICompiledTypedProfileQuery<TOut> CompileForProfile<TOut>()
    {
        return new PublicCompiledTypedProfileQuery<TOut>(
            _query,
            CreateSourceBinding(),
            _loggerResolver,
            _compilationOptions);
    }

    public IEnumerable<TOut> CompileAndRun<TOut>(CancellationToken token)
    {
        return Compile<TOut>().Run(token, CreateSourceBinding().SnapshotDefaultRows(_defaultRows.Values));
    }

    private InMemorySourceBinding CreateSourceBinding()
    {
        return new InMemorySourceBinding(_sources);
    }

    private void AddOrValidateSource<T>(string schemaName, string sourceName)
    {
        var slot = new InMemorySourceSlot(schemaName, sourceName, typeof(T));
        var existing = _sources.FirstOrDefault(source => string.Equals(source.Key, slot.Key, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
        {
            _sources.Add(slot);
            return;
        }

        if (existing.RowType != slot.RowType)
        {
            throw new InvalidOperationException(
                $"Source '#{slot.SchemaName}.{slot.SourceName}()' is already declared with row type '{existing.RowType.FullName}'.");
        }
    }
}
