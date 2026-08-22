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

    /// <summary>
    /// Gets the columns inferred before the source table is opened.
    /// </summary>
    /// <remarks>
    /// An empty collection is the established unrestricted metadata request: the source may return its
    /// complete schema. Core uses that request for direct SELECT-field wildcard projections, while
    /// non-projection uses of <c>AllColumnsNode</c> such as <c>Count(*)</c> do not independently activate it.
    /// </remarks>
    public IReadOnlyCollection<ISchemaColumn> AllColumns { get; } = originallyInferredColumns;

    public IReadOnlyDictionary<string, string> SourceRuntimeSettings { get; } = sourceRuntimeSettings;

    public ILogger Logger { get; } = logger;
}
