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
        out string diagnostic)
    {
        ArgumentNullException.ThrowIfNull(requested);
        ArgumentNullException.ThrowIfNull(accepted);

        var requestedByName = requested.ToDictionary(static projection => projection.Name, StringComparer.OrdinalIgnoreCase);
        var acceptedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var acceptedList = new List<SourceComputedProjection>(accepted.Count);
        foreach (var projection in accepted)
        {
            if (!requestedByName.TryGetValue(projection.Name, out var requestedProjection))
            {
                partition = SourceComputedProjectionPartition.Empty;
                diagnostic = $"Provider accepted unknown computed projection '{projection.Name}'.";
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

            if (!string.Equals(
                    SourceScalarExpressionFingerprint.Compute(requestedProjection.Expression),
                    SourceScalarExpressionFingerprint.Compute(projection.Expression),
                    StringComparison.Ordinal))
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
        return expression switch
        {
            SourceScalarLiteral => SourceComputedProjectionCapabilities.Literals,
            SourceScalarColumn => SourceComputedProjectionCapabilities.Columns,
            SourceScalarUnary unary => SourceComputedProjectionCapabilities.Unary | RequiredCapabilities(unary.Operand),
            SourceScalarBinary binary => SourceComputedProjectionCapabilities.Binary |
                                         RequiredCapabilities(binary.Left) |
                                         RequiredCapabilities(binary.Right),
            SourceScalarCast cast => SourceComputedProjectionCapabilities.Cast | RequiredCapabilities(cast.Operand),
            SourceScalarNullCheck nullCheck => SourceComputedProjectionCapabilities.NullCheck | RequiredCapabilities(nullCheck.Operand),
            SourceScalarCoalesce coalesce => SourceComputedProjectionCapabilities.Coalesce |
                                             coalesce.Expressions.Aggregate(SourceComputedProjectionCapabilities.None, static (current, child) => current | RequiredCapabilities(child)),
            _ => SourceComputedProjectionCapabilities.None
        };
    }

    public static bool CanProviderEvaluate(
        SourceComputedProjection projection,
        SourceComputedProjectionCapabilities capabilities)
    {
        ArgumentNullException.ThrowIfNull(projection);
        var required = RequiredCapabilities(projection.Expression);
        return projection.IsStable && (capabilities & required) == required;
    }
}
