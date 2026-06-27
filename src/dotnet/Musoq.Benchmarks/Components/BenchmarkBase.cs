using Musoq.Benchmarks.Schema;
using Musoq.Benchmarks.Schema.Country;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Tests.Common;

namespace Musoq.Benchmarks.Components;

public class BenchmarkBase
{
    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();

    static BenchmarkBase()
    {
        Culture.ApplyWithDefaultCulture();
    }

    protected CancellationTokenSource TokenSource { get; } = new();

    protected CompiledQuery CreateForCountryWithOptions(
        string script,
        IDictionary<string, IEnumerable<CountryEntity>> sources,
        CompilationOptions compilationOptions,
        BenchmarkChunkShape chunkShape = BenchmarkChunkShape.Chunk4096)
    {
        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            new GenericSchemaProvider<CountryEntity, CountryEntityTable>(BenchmarkSourceChunks.FromRows(sources, chunkShape), CountryEntity.KNameToIndexMap,
                CountryEntity.KIndexToObjectAccessMap), _loggerResolver, BenchmarkCompilationOptions.Materialized(compilationOptions));
    }

    protected CompiledQuery CreateForProfilesWithOptions(
        string script,
        IDictionary<string, IEnumerable<ProfileEntity>> sources,
        CompilationOptions compilationOptions,
        BenchmarkChunkShape chunkShape = BenchmarkChunkShape.Chunk4096)
    {
        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            new GenericSchemaProvider<ProfileEntity, ProfileEntityTable>(BenchmarkSourceChunks.FromRows(sources, chunkShape), ProfileEntity.KNameToIndexMap,
                ProfileEntity.KIndexToObjectAccessMap), _loggerResolver, BenchmarkCompilationOptions.Materialized(compilationOptions));
    }
}
