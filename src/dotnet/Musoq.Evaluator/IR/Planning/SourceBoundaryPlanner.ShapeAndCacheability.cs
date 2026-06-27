using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class SourceBoundaryPlanner
{
    private static SourceBoundaryInputMode ResolveApplyInputMode(
        IReadOnlyList<string> leftAliases,
        string[] dependencyAliases)
    {
        if (dependencyAliases.Length == 0)
            return SourceBoundaryInputMode.Independent;

        var left = new HashSet<string>(leftAliases, StringComparer.OrdinalIgnoreCase);
        return dependencyAliases.Any(left.Contains)
            ? SourceBoundaryInputMode.Correlated
            : SourceBoundaryInputMode.Unknown;
    }

    private static SourceInvocationShape ResolveInvocationShape(SourceBoundaryInputMode mode)
    {
        return mode switch
        {
            SourceBoundaryInputMode.Independent => SourceInvocationShape.PerQuery,
            SourceBoundaryInputMode.Correlated => SourceInvocationShape.PerRow,
            _ => SourceInvocationShape.Unknown
        };
    }

    private static SourceRowBehavior ResolveRowBehavior(ApplyKind applyKind)
    {
        return applyKind == ApplyKind.Outer
            ? SourceRowBehavior.RowPreserving
            : SourceRowBehavior.RowMultiplying;
    }

    private static SourceResultShape ResolveResultShape(LogicalNode node)
    {
        return node switch
        {
            InterpretSourceNode interpret => ResolveResultShape(interpret.ResultType),
            PropertySourceNode property => ResolveResultShape(property.ResultType),
            AccessMethodSourceNode accessMethod => ResolveResultShape(accessMethod.ResultType),
            SchemaScanNode or CteRefNode => SourceResultShape.Declared,
            ApplyNode apply => ResolveResultShape(apply.Right),
            _ => node.OutputSchema.Columns.Length == 0 ? SourceResultShape.Unknown : SourceResultShape.Declared
        };
    }

    private static SourceResultShape ResolveResultShape(Type resultType)
    {
        if (DynamicEntityBoundary.IsDynamicResultShape(resultType, ImplementsGenericDictionary))
        {
            return SourceResultShape.Dynamic;
        }

        return resultType == typeof(object) ? SourceResultShape.Unknown : SourceResultShape.Declared;
    }

    private static bool ImplementsGenericDictionary(Type resultType)
    {
        return IsGenericDictionary(resultType) || resultType.GetInterfaces().Any(IsGenericDictionary);
    }

    private static bool IsGenericDictionary(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var typeDefinition = type.GetGenericTypeDefinition();
        return typeDefinition == typeof(IDictionary<,>) ||
               typeDefinition == typeof(IReadOnlyDictionary<,>);
    }

    private static SourceCacheability ResolveCacheability(SourceBoundaryInputMode mode)
    {
        return mode switch
        {
            SourceBoundaryInputMode.Independent => SourceCacheability.CacheCandidate,
            SourceBoundaryInputMode.Correlated => SourceCacheability.NotCacheable,
            _ => SourceCacheability.Unknown
        };
    }

    private static PlanningConfidence ResolveCacheabilityConfidence(SourceBoundaryInputMode mode)
    {
        return mode switch
        {
            SourceBoundaryInputMode.Independent => PlanningConfidence.Medium,
            SourceBoundaryInputMode.Correlated => PlanningConfidence.High,
            _ => PlanningConfidence.Low
        };
    }
}
