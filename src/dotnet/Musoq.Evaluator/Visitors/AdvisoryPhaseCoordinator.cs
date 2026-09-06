using Musoq.Parser.Nodes;
using System.Threading;

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
        RootNode? authoredQuery = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(diagnosticContext);

        var context = new SemanticAdvisoryContext(query, metadata, diagnosticContext, sourceQuery, authoredQuery, cancellationToken);
        ScalarSubqueryCardinalityAnalyzer.Analyze(context);
        context.ThrowIfCancellationRequested();
        RegexPatternAdvisoryAnalyzer.Analyze(context);
        context.ThrowIfCancellationRequested();
        LikePatternAdvisoryAnalyzer.Analyze(context);
        context.ThrowIfCancellationRequested();
        TemporalConversionAdvisoryAnalyzer.Analyze(context);
        context.ThrowIfCancellationRequested();
        NullSensitiveMembershipAdvisoryAnalyzer.Analyze(context);
        context.ThrowIfCancellationRequested();
        PathColumnAdvisoryAnalyzer.Analyze(context);
        context.ThrowIfCancellationRequested();
        PredicateAdvisoryAnalyzer.Analyze(context);
        context.ThrowIfCancellationRequested();
        OuterJoinAdvisoryAnalyzer.Analyze(context);
        context.ThrowIfCancellationRequested();
        UnreachableBranchAdvisoryAnalyzer.Analyze(context);
        context.ThrowIfCancellationRequested();
        UnusedDeclarationAdvisoryAnalyzer.Analyze(context);
        context.ThrowIfCancellationRequested();
        OrderingSlicingAdvisoryAnalyzer.Analyze(context);
        context.ThrowIfCancellationRequested();
    }
}
