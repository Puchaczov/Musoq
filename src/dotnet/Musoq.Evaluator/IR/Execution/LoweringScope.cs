using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace Musoq.Evaluator.IR.Execution;

internal sealed class LoweringScope
{
    public LoweringScope(PhysicalLoweringFacts facts)
        : this(
            facts,
            CteLoweringContext.Empty,
            DirectTableLoweringContext.Empty)
    {
    }

    private LoweringScope(
        PhysicalLoweringFacts facts,
        CteLoweringContext cte,
        DirectTableLoweringContext directTable)
    {
        Facts = facts ?? throw new ArgumentNullException(nameof(facts));
        Cte = cte ?? throw new ArgumentNullException(nameof(cte));
        DirectTable = directTable ?? throw new ArgumentNullException(nameof(directTable));
    }

    public PhysicalLoweringFacts Facts { get; }

    public CteLoweringContext Cte { get; }

    public DirectTableLoweringContext DirectTable { get; }

    public ExecutionStrategyPlan ExecutionStrategies => Facts.ExecutionStrategies;

    public IReadOnlyDictionary<string, FusedCteHashBuildSource>? FusedCteHashBuildSources =>
        Cte.FusedHashBuildSources;

    public CteSidecarHashPayloadState CteSidecarHashPayloads => Cte.SidecarHashPayloads;

    public bool SuppressSidecarJoinPipeline => Cte.SuppressSidecarJoinPipeline;

    public IReadOnlyDictionary<string, ScalarSubqueryEmptyResultSpec> ScalarSubqueryEmptyResults =>
        Cte.ScalarSubqueryEmptyResults;

    public RecursiveCteTableSink? RecursiveCteSink => Cte.RecursiveCte.Sink;

    public IDirectTableSink? DirectTableSink => DirectTable.Sink;

    public IReadOnlyDictionary<string, RecursiveCteInvariantInput> RecursiveCteInvariantInputs =>
        Cte.RecursiveCte.InvariantInputs;

    public LoweringScope WithFusedCteHashBuildSources(
        IReadOnlyDictionary<string, FusedCteHashBuildSource>? fusedCteHashBuildSources) =>
        Create(Cte with
        {
            FusedHashBuildSources = fusedCteHashBuildSources == null
                ? null
                : new ReadOnlyDictionary<string, FusedCteHashBuildSource>(
                    new Dictionary<string, FusedCteHashBuildSource>(
                        fusedCteHashBuildSources,
                        StringComparer.OrdinalIgnoreCase))
        });

    public LoweringScope WithCteSidecarHashPayloads(
        CteSidecarHashPayloadState sidecarHashPayloads)
    {
        ArgumentNullException.ThrowIfNull(sidecarHashPayloads);
        return Create(Cte with { SidecarHashPayloads = sidecarHashPayloads });
    }

    public LoweringScope WithSidecarJoinPipelineSuppressed() =>
        Create(Cte with { SuppressSidecarJoinPipeline = true });

    public LoweringScope WithScalarSubqueryEmptyResults(
        IReadOnlyDictionary<string, ScalarSubqueryEmptyResultSpec> scalarSubqueryEmptyResults)
    {
        ArgumentNullException.ThrowIfNull(scalarSubqueryEmptyResults);
        return Create(Cte with
        {
            ScalarSubqueryEmptyResults = new ReadOnlyDictionary<string, ScalarSubqueryEmptyResultSpec>(
                new Dictionary<string, ScalarSubqueryEmptyResultSpec>(
                    scalarSubqueryEmptyResults,
                    StringComparer.OrdinalIgnoreCase))
        });
    }

    public LoweringScope WithRecursiveCteSink(RecursiveCteTableSink recursiveCteSink)
    {
        ArgumentNullException.ThrowIfNull(recursiveCteSink);
        return Create(
            Cte with { RecursiveCte = Cte.RecursiveCte with { Sink = recursiveCteSink } },
            new DirectTableLoweringContext(recursiveCteSink));
    }

    public LoweringScope WithDirectTableSink(IDirectTableSink directTableSink)
    {
        ArgumentNullException.ThrowIfNull(directTableSink);
        return Create(Cte, new DirectTableLoweringContext(directTableSink));
    }

    public LoweringScope WithoutDirectTableSink() =>
        Create(Cte, DirectTableLoweringContext.Empty);

    public LoweringScope WithRecursiveCteInvariantInputs(
        IReadOnlyDictionary<string, RecursiveCteInvariantInput> recursiveCteInvariantInputs)
    {
        ArgumentNullException.ThrowIfNull(recursiveCteInvariantInputs);
        return Create(Cte with
        {
            RecursiveCte = Cte.RecursiveCte with
            {
                InvariantInputs = new ReadOnlyDictionary<string, RecursiveCteInvariantInput>(
                    new Dictionary<string, RecursiveCteInvariantInput>(
                        recursiveCteInvariantInputs,
                        StringComparer.Ordinal))
            }
        });
    }

    private LoweringScope Create(
        CteLoweringContext cte,
        DirectTableLoweringContext? directTable = null) =>
        new(Facts, cte, directTable ?? DirectTable);
}
