using Musoq.Schema;
using Musoq.Schema.Exceptions;

namespace Musoq.Examples.DataSources.Git;

public sealed class GitSchemaProvider : ISchemaProvider
{
    private readonly IGitHistoryStore _store;
    private readonly GitDataSourceApiRecorder? _recorder;

    public GitSchemaProvider()
        : this(InMemoryGitHistoryStore.CreateDefault())
    {
    }

    public GitSchemaProvider(IGitHistoryStore store)
        : this(store, null)
    {
    }

    internal GitSchemaProvider(GitDataSourceApiRecorder recorder)
        : this(InMemoryGitHistoryStore.CreateDefault(), recorder)
    {
    }

    internal GitSchemaProvider(IGitHistoryStore store, GitDataSourceApiRecorder? recorder)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _recorder = recorder;
    }

    public ISchema GetSchema(string schema)
    {
        _recorder?.SchemaRequests.Add(schema);

        if (string.Equals(schema, GitSchema.SchemaName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(schema, $"#{GitSchema.SchemaName}", StringComparison.OrdinalIgnoreCase))
            return new GitSchema(_store, _recorder);

        throw new SourceNotFoundException($"Git example schema provider does not expose schema '{schema}'.");
    }
}
