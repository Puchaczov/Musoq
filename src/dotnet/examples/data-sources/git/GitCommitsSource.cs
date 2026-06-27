using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Git;

public sealed class GitCommitsSource : DiagnosticChunkedRowSource<GitCommitRow>
{
    private const int ChunkSize = 32;
    internal const string SourceName = "git.commits";

    private readonly SourceExecutionContext _context;
    private readonly string? _repository;
    private readonly SourceExecutionPlan _plan;
    private readonly IGitHistoryStore _store;

    public GitCommitsSource(IGitHistoryStore store, SourceExecutionContext context)
        : this(repository: null, store, context)
    {
    }

    public GitCommitsSource(string? repository, IGitHistoryStore store, SourceExecutionContext context)
        : base(context, SourceName)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
        _repository = ResolveRepository(repository, context);
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _plan = context.Plan;
    }

    protected override void CollectChunks(DiagnosticChunkWriter<GitCommitRow> writer)
    {
        var token = writer.CancellationToken;
        token.ThrowIfCancellationRequested();
        _context.ReportDataSourceBegin(SourceName);

        long rowsRead = 0;

        try
        {
            var commits = _store.GetCommits(_repository);
            _context.ReportDataSourceRowsKnown(SourceName, commits.Count);

            var rows = commits
                .Select(commit => new GitCommitRow(commit, _store.GetStats));
            var plannedRows = GitCommitPlan.Apply(rows, _plan);
            var chunk = new List<GitCommitRow>(ChunkSize);

            foreach (var row in plannedRows)
            {
                token.ThrowIfCancellationRequested();

                chunk.Add(row);

                if (chunk.Count < ChunkSize)
                    continue;

                rowsRead += WriteChunk(writer, chunk);
                ReportRowsRead(rowsRead);
            }

            var finalRowsRead = WriteChunk(writer, chunk);
            if (finalRowsRead > 0)
            {
                rowsRead += finalRowsRead;
                ReportRowsRead(rowsRead);
            }
        }
        finally
        {
            _context.ReportDataSourceEnd(SourceName, rowsRead);
        }
    }

    private static string? ResolveRepository(string? explicitRepository, SourceExecutionContext context)
    {
        if (!string.IsNullOrWhiteSpace(explicitRepository))
            return explicitRepository;

        if (context.Plan.Properties.TryGetValue(GitSourcePlanProperties.Repository, out var plannedRepository) &&
            plannedRepository is string repositoryFromPlan &&
            !string.IsNullOrWhiteSpace(repositoryFromPlan))
            return repositoryFromPlan;

        return context.SourceRuntimeSettings.TryGetValue(GitSchema.RepositoryRuntimeSetting, out var repository) &&
            !string.IsNullOrWhiteSpace(repository)
                ? repository
                : null;
    }

    private static int WriteChunk(
        DiagnosticChunkWriter<GitCommitRow> writer,
        List<GitCommitRow> chunk)
    {
        if (chunk.Count == 0)
            return 0;

        var rowsWritten = chunk.Count;
        writer.Write(chunk.ToArray());
        chunk.Clear();
        return rowsWritten;
    }

    private void ReportRowsRead(long rowsRead)
    {
        _context.ReportDataSourceRowsRead(SourceName, rowsRead);
    }
}
