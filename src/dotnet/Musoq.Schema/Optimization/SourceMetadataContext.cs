using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Musoq.Schema.Optimization;

public class SourceMetadataContext(
    string queryId,
    CancellationToken endWorkToken,
    IReadOnlyCollection<ISchemaColumn> originallyInferredColumns,
    IReadOnlyDictionary<string, string> sourceRuntimeSettings,
    ILogger logger)
{
    public string QueryId { get; } = queryId;

    public CancellationToken EndWorkToken { get; } = endWorkToken;

    public IReadOnlyCollection<ISchemaColumn> AllColumns { get; } = originallyInferredColumns;

    public IReadOnlyDictionary<string, string> SourceRuntimeSettings { get; } = sourceRuntimeSettings;

    public ILogger Logger { get; } = logger;
}
