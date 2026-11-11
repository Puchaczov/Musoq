using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser;

namespace Musoq.Evaluator.IR.Planning.SourcePlanning;

internal sealed record SourceComputedTransferPlan(
    IReadOnlyList<SourceComputedProjectionBinding> Bindings,
    FrozenSet<string> UnusedSourceColumns);

internal sealed record SourceComputedProjectionAnalysis(
    IReadOnlyList<SourceComputedProjectionBinding> Offers,
    IReadOnlyDictionary<IrExpression, FrozenSet<string>> InputsByExpression,
    FrozenSet<string> RequiredOutsideProjection,
    int DeclinedForTransferCost)
{
    internal static SourceComputedProjectionAnalysis Empty { get; } =
        new([], FrozenDictionary<IrExpression, FrozenSet<string>>.Empty, FrozenSet<string>.Empty, 0);

    internal SourceComputedTransferPlan? ResolveValidatedAcceptance(SourcePlanResult result,
        CancellationToken cancellationToken)
    {
        if (result.AcceptedComputedProjections.Count == 0)
            return null;

        var acceptedNames = result.AcceptedComputedProjections.Select(static projection => projection.Name)
            .ToHashSet(StringComparer.Ordinal);
        var bindings = Offers.Where(binding => acceptedNames.Contains(binding.Projection.Name)).ToArray();
        if (bindings.Length != acceptedNames.Count)
            throw new SourcePlanContractException("Accepted computed projections lost their planner bindings.", TextSpan.Empty);

        var accepted = bindings.Select(static binding => binding.Expression).ToHashSet();
        var liveInputs = RequiredOutsideProjection.ToHashSet(StringComparer.Ordinal);
        var providerInputs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (expression, inputs) in InputsByExpression)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (accepted.Contains(expression))
                providerInputs.UnionWith(inputs);
            else
                liveInputs.UnionWith(inputs);
        }

        providerInputs.ExceptWith(liveInputs);
        return new SourceComputedTransferPlan(Array.AsReadOnly(bindings),
            providerInputs.ToFrozenSet(StringComparer.Ordinal));
    }
}
