using System.Collections.Generic;
using System.Threading;

namespace Musoq.Schema.Optimization;

public sealed record SourceRuntimeSettingsResolutionRequest(
    SourceIdentity Identity,
    string? ProfileName,
    IReadOnlyList<SourceRuntimeSettingRequirement> Requirements,
    object?[] Parameters)
{
    /// <summary>
    /// Gets the cooperative cancellation token for this transient resolution request.
    /// </summary>
    /// <remarks>
    /// Resolvers should observe this token while resolving settings and must not retain it in
    /// reusable settings, cache entries, or artifact metadata. A resolver that ignores the token
    /// cannot be forcibly terminated by the engine.
    /// </remarks>
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;
}
