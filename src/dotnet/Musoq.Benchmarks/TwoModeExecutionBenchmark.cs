using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Helpers;
using Musoq.Benchmarks.Schema;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tables;
using MusoqApi = Musoq.Converter.Musoq;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class TwoModeExecutionBenchmark : BenchmarkBase
{
    private const int DefaultRowsCount = 5_000;
    private const string DirectProjectionTableQuery =
        "select FirstName, LastName, Email from #A.Entities() where Gender = 'Female'";
    private const string DirectProjectionTypedQuery =
        "select p.FirstName as FirstName, p.LastName as LastName, p.Email as Email from #A.entities() p where p.Gender = 'Female'";
    private const string FallbackTypedQuery =
        "select distinct p.Gender as Value from #A.entities() p order by p.Gender skip 0 take 2";

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private List<ProfileEntity> _profiles = null!;
    private CompiledQuery _tableProjection = null!;
    private ICompiledTypedQuery<ProfileProjection> _publicTypedProjection = null!;
    private ICompiledTypedQuery<ValueProjection> _publicTypedFallback = null!;
    private CompiledTypedQueryArtifact _publicTypedArtifact = null!;
    private ICompiledTypedQuery<ProfileProjection> _publicTypedLoadedArtifact = null!;
    private ICompiledTypedProfileQuery<ProfileProjection> _publicTypedProfile = null!;
    private CompiledTypedQuery<ProfileProjection> _typedSerialProjection = null!;
    private CompiledTypedQuery<ProfileProjection> _typedParallelProjection = null!;
    private MusoqSourceRows _publicSourceRows = null!;
    private List<ValueRow> _typedPostOperationRows = null!;

    [Params(DefaultRowsCount)]
    public int RowsCount { get; set; }

    [Params(BenchmarkChunkShape.Chunk512, BenchmarkChunkShape.Chunk4096, BenchmarkChunkShape.SingleGiant)]
    public BenchmarkChunkShape ChunkShape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _profiles = ReadProfiles();
        _publicSourceRows = MusoqApi.Source("#A", "entities", Chunks(_profiles, ChunkShape));

        _tableProjection = CreateForProfilesWithOptions(
            DirectProjectionTableQuery,
            CreateProfileSources(_profiles),
            new CompilationOptions(ParallelizationMode.Full),
            ChunkShape);

        _publicTypedProjection = MusoqApi
            .Query(DirectProjectionTypedQuery)
            .Source<ProfileEntity>("#A", "entities")
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.Full))
            .Compile<ProfileProjection>();

        _publicTypedArtifact = MusoqApi
            .Query(DirectProjectionTypedQuery)
            .Source<ProfileEntity>("#A", "entities")
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.Full))
            .CompileArtifact<ProfileProjection>();
        _publicTypedLoadedArtifact = MusoqApi.Load<ProfileProjection>(_publicTypedArtifact);

        _publicTypedProfile = MusoqApi
            .Query(DirectProjectionTypedQuery)
            .Source<ProfileEntity>("#A", "entities")
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.Full))
            .CompileForProfile<ProfileProjection>();

        _publicTypedFallback = MusoqApi
            .Query(FallbackTypedQuery)
            .Source<ProfileEntity>("#A", "entities")
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.Full))
            .Compile<ValueProjection>();

        _typedSerialProjection = CompileTypedProjection(new CompilationOptions(ParallelizationMode.None));
        _typedParallelProjection = CompileTypedProjection(new CompilationOptions(ParallelizationMode.Full));
        _typedPostOperationRows = _profiles
            .Select(static row => new ValueRow(row.Gender))
            .ToList();
    }

    [Benchmark(Baseline = true)]
    public Table Table_DirectProjection()
    {
        return _tableProjection.Run();
    }

    [Benchmark]
    public ProfileProjection[] PublicTyped_DirectProjection()
    {
        return _publicTypedProjection
            .Run(CancellationToken.None, _publicSourceRows)
            .ToArray();
    }

    [Benchmark]
    public ProfileProjection[] Linq_DirectProjection()
    {
        return _profiles
            .Where(static row => row.Gender == "Female")
            .Select(static row => new ProfileProjection(row.FirstName, row.LastName, row.Email))
            .ToArray();
    }

    [Benchmark]
    public ProfileProjection[] PublicTyped_CompileFromScratchAndRun()
    {
        return MusoqApi
            .Query(DirectProjectionTypedQuery)
            .Source("#A", "entities", Chunks(_profiles, ChunkShape))
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.Full))
            .CompileAndRun<ProfileProjection>(CancellationToken.None)
            .ToArray();
    }

    [Benchmark]
    public ProfileProjection[] PublicTyped_ReusableCompiledRun()
    {
        return _publicTypedProjection
            .Run(CancellationToken.None, _publicSourceRows)
            .ToArray();
    }

    [Benchmark]
    public ProfileProjection[] PublicTyped_LoadedArtifactRun()
    {
        return _publicTypedLoadedArtifact
            .Run(CancellationToken.None, _publicSourceRows)
            .ToArray();
    }

    [Benchmark]
    public ProfileProjection[] PublicTyped_LoadArtifactAndRun()
    {
        return MusoqApi
            .Load<ProfileProjection>(_publicTypedArtifact)
            .Run(CancellationToken.None, _publicSourceRows)
            .ToArray();
    }

    [Benchmark]
    public ProfileProjection[] PublicTyped_ProfileTableBacked()
    {
        var result = _publicTypedProfile.RunWithProfile(CancellationToken.None, _publicSourceRows);
        var rows = result.Rows.ToArray();
        _ = result.Profile;
        return rows;
    }

    [Benchmark]
    public ProfileProjection[] TypedSerial_DirectProjection()
    {
        return _typedSerialProjection.Run(CancellationToken.None).ToArray();
    }

    [Benchmark]
    public ProfileProjection[] TypedParallel_DirectProjection()
    {
        return _typedParallelProjection.Run(CancellationToken.None).ToArray();
    }

    [Benchmark]
    public ValueProjection[] PublicTyped_FallbackDistinctOrderSkipTake()
    {
        return _publicTypedFallback
            .Run(CancellationToken.None, _publicSourceRows)
            .ToArray();
    }

    [Benchmark]
    public ValueProjection[] TypedPostOperationRows_DistinctOrderSkipTake()
    {
        return TypedPostOperationRows.Project(
                TypedPostOperationRows.Order(
                    TypedPostOperationRows.Distinct(_typedPostOperationRows),
                    [new TypedRowOrderKey<ValueRow>(static row => row[0], false, 0)])
                    .Skip(0)
                    .Take(2),
                static row => new ValueProjection((string)row[0]))
            .ToArray();
    }

    private List<ProfileEntity> ReadProfiles()
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "profiles.csv");
        return DataHelpers.ReadProfiles(contentPath)
            .Take(RowsCount)
            .ToList();
    }

    private CompiledTypedQuery<ProfileProjection> CompileTypedProjection(CompilationOptions options)
    {
        return InstanceCreator.CompileForTypedExecution<ProfileProjection>(
            DirectProjectionTableQuery,
            $"MusoqTwoModeBenchmark_{Guid.NewGuid():N}",
            new GenericSchemaProvider<ProfileEntity, ProfileEntityTable>(
                BenchmarkSourceChunks.FromRows(CreateProfileSources(_profiles), ChunkShape),
                ProfileEntity.KNameToIndexMap,
                ProfileEntity.KIndexToObjectAccessMap),
            _loggerResolver,
            options);
    }

    private static Dictionary<string, IEnumerable<ProfileEntity>> CreateProfileSources(
        IReadOnlyList<ProfileEntity> profiles)
    {
        return new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            ["#A"] = profiles
        };
    }

    public sealed record ProfileProjection(string FirstName, string LastName, string Email);

    public sealed record ValueProjection(string Value);

    private static IEnumerable<IReadOnlyList<T>> Chunks<T>(
        IReadOnlyList<T> rows,
        BenchmarkChunkShape chunkShape)
    {
        return BenchmarkSourceChunks.Single("#A", rows, chunkShape)["#A"];
    }

    public sealed class ValueRow(string value) : Row
    {
        public override int Count => 1;

        public override object this[int columnNumber] => columnNumber switch
        {
            0 => value,
            _ => throw new IndexOutOfRangeException()
        };
    }
}
