using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

/// <summary>
/// Orchestrates focused semantic advisory analyzers after metadata binding.
/// </summary>
internal sealed class SemanticAdvisoryPhaseCoordinator
{
    public void Analyze(
        RootNode query,
        SemanticMetadataSnapshot metadata,
        DiagnosticContext diagnosticContext,
        RootNode? sourceQuery = null,
        RootNode? authoredQuery = null)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(diagnosticContext);

        var context = new SemanticAdvisoryContext(query, metadata, diagnosticContext, sourceQuery, authoredQuery);
        ScalarSubqueryCardinalityAnalyzer.Analyze(context);
        RegexPatternAdvisoryAnalyzer.Analyze(context);
        LikePatternAdvisoryAnalyzer.Analyze(context);
        TemporalConversionAdvisoryAnalyzer.Analyze(context);
        NullSensitiveMembershipAdvisoryAnalyzer.Analyze(context);
        PathColumnAdvisoryAnalyzer.Analyze(context);
        PredicateAdvisoryAnalyzer.Analyze(context);
        OuterJoinAdvisoryAnalyzer.Analyze(context);
        UnreachableBranchAdvisoryAnalyzer.Analyze(context);
        UnusedDeclarationAdvisoryAnalyzer.Analyze(context);
        OrderingSlicingAdvisoryAnalyzer.Analyze(context);
    }
}
