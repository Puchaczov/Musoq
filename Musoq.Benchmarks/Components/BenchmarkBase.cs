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
        CompilationOptions compilationOptions)
    {
        return InstanceCreator.CompileForExecution(
            script, 
            Guid.NewGuid().ToString(), 
            new GenericSchemaProvider<CountryEntity, CountryEntityTable>(sources, CountryEntity.KNameToIndexMap, CountryEntity.KIndexToObjectAccessMap), _loggerResolver, compilationOptions);
    }

    protected CompiledQuery CreateForProfilesWithOptions(
        string script,
        IDictionary<string, IEnumerable<ProfileEntity>> sources,
        CompilationOptions compilationOptions)
    {
        return InstanceCreator.CompileForExecution(
            script, 
            Guid.NewGuid().ToString(), 
            new GenericSchemaProvider<ProfileEntity, ProfileEntityTable>(sources, ProfileEntity.KNameToIndexMap, ProfileEntity.KIndexToObjectAccessMap), _loggerResolver, compilationOptions);
    }
}