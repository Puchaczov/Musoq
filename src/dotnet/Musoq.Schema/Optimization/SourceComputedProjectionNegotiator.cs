using System.Collections.Generic;
using System.Linq;

namespace Musoq.Schema.Optimization;

/// <summary>
/// Validates the requested/accepted/residual partition for provider-computed
/// projections. It never evaluates an expression and therefore cannot freeze
/// a volatile value accidentally.
/// </summary>
public static class SourceComputedProjectionNegotiator
{
    public static bool TryPartition(
        IReadOnlyList<SourceComputedProjection> requested,
        IReadOnlyList<SourceComputedProjection> accepted,
        SourceComputedProjectionCapabilities capabilities,
        out SourceComputedProjectionPartition partition,
        out string diagnostic
    )
    {
        ArgumentNullException.ThrowIfNull(requested);
        ArgumentNullException.ThrowIfNull(accepted);

        var requestedByName = new Dictionary<string, SourceComputedProjection>(StringComparer.Ordinal);
        foreach (var projection in requested)
        {
            if (
                projection is null
                || string.IsNullOrWhiteSpace(projection.Name)
                || !requestedByName.TryAdd(projection.Name, projection)
            )
            {
                partition = SourceComputedProjectionPartition.Empty;
                diagnostic = "Requested computed projections must have unique, nonempty names.";
                return false;
            }
        }
        var acceptedNames = new HashSet<string>(StringComparer.Ordinal);
        var acceptedList = new List<SourceComputedProjection>(accepted.Count);
        foreach (var projection in accepted)
        {
            if (projection is null || !requestedByName.TryGetValue(projection.Name, out var requestedProjection))
            {
                partition = SourceComputedProjectionPartition.Empty;
                diagnostic = $"Provider accepted unknown computed projection '{projection?.Name}'.";
                return false;
            }

            if (!acceptedNames.Add(projection.Name))
            {
                partition = SourceComputedProjectionPartition.Empty;
                diagnostic = $"Provider accepted computed projection '{projection.Name}' more than once.";
                return false;
            }

            if (!SourceComputedProjectionFacts.CanProviderEvaluate(projection, capabilities))
            {
                partition = SourceComputedProjectionPartition.Empty;
                diagnostic = $"Provider accepted unsupported or unstable computed projection '{projection.Name}'.";
                return false;
            }

            if (
                projection.ResultType != requestedProjection.ResultType
                || projection.Stability != requestedProjection.Stability
                || !requestedProjection.IsStable
                || !string.Equals(
                    SourceScalarExpressionFingerprint.Compute(requestedProjection.Expression),
                    SourceScalarExpressionFingerprint.Compute(projection.Expression),
                    StringComparison.Ordinal
                )
            )
            {
                partition = SourceComputedProjectionPartition.Empty;
                diagnostic = $"Provider changed computed projection '{projection.Name}' while accepting it.";
                return false;
            }

            acceptedList.Add(requestedProjection);
        }

        var residual = requested.Where(projection => !acceptedNames.Contains(projection.Name)).ToArray();
        partition = new SourceComputedProjectionPartition(requested.ToArray(), acceptedList, residual);
        diagnostic = string.Empty;
        return true;
    }
}

public static class SourceComputedProjectionFacts
{
    public static SourceComputedProjectionCapabilities RequiredCapabilities(SourceScalarExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        if (!SourceScalarExpressionGraph.IsValid(expression))
            return SourceComputedProjectionCapabilities.None;
        return Required(expression);
    }

    private static SourceComputedProjectionCapabilities Required(SourceScalarExpression expression)
    {
        return expression switch
        {
            SourceScalarLiteral => SourceComputedProjectionCapabilities.Literals,
            SourceScalarColumn => SourceComputedProjectionCapabilities.Columns,
            SourceScalarUnary unary => SourceComputedProjectionCapabilities.Unary | Required(unary.Operand),
            SourceScalarBinary binary => SourceComputedProjectionCapabilities.Binary
                | Required(binary.Left)
                | Required(binary.Right),
            SourceScalarCast cast => SourceComputedProjectionCapabilities.Cast | Required(cast.Operand),
            SourceScalarNullCheck nullCheck => SourceComputedProjectionCapabilities.NullCheck
                | Required(nullCheck.Operand),
            SourceScalarCoalesce coalesce => SourceComputedProjectionCapabilities.Coalesce
                | coalesce.Expressions.Aggregate(
                    SourceComputedProjectionCapabilities.None,
                    static (current, child) => current | Required(child)
                ),
            _ => SourceComputedProjectionCapabilities.None,
        };
    }

    public static bool CanProviderEvaluate(
        SourceComputedProjection projection,
        SourceComputedProjectionCapabilities capabilities
    )
    {
        ArgumentNullException.ThrowIfNull(projection);
        if (
            string.IsNullOrWhiteSpace(projection.Name)
            || projection.Expression is null
            || projection.ResultType != projection.Expression.ResultType
            || !projection.IsStable
        )
            return false;
        var required = RequiredCapabilities(projection.Expression);
        return required != SourceComputedProjectionCapabilities.None && (capabilities & required) == required;
    }
}
