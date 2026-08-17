using Musoq.Parser.Nodes;

namespace Musoq.Converter.Build;

/// <summary>
/// Typed view of the parse stage output that the transform pipeline begins from.
/// </summary>
internal sealed record ParseBuildArtifacts(RootNode RawQueryTree);
