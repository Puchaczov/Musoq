using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization.Execution;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Analysis;

/// <summary>
/// Shared, stability-aware scalar-use discovery used by execution optimizers.
/// Keeping discovery here prevents a new operator from accidentally falling
/// back to field-only reuse rules.
/// </summary>
internal static class ScalarReuseCollector
{
    public static IReadOnlyList<ScalarReuseCandidate> Collect(ExecutionPlan plan, string ownerScope = "plan")
    {
        ArgumentNullException.ThrowIfNull(plan);

        return ExecutionExpressionCseFacts
            .CollectStableScalarReuseOccurrences(plan.Body)
            .GroupBy(static occurrence => occurrence.Signature, StringComparer.Ordinal)
            .Select(group => CreateCandidate(group.Key, group.ToArray(), ownerScope))
            .OrderByDescending(static candidate => candidate.UseCount)
            .ThenBy(static candidate => candidate.Fingerprint, StringComparer.Ordinal)
            .ToArray();
    }

    private static ScalarReuseCandidate CreateCandidate(
        string fingerprint,
        IReadOnlyList<ExecutionExpressionCseFacts.HoistOccurrence> occurrences,
        string ownerScope)
    {
        var expression = occurrences[0].Expression;
        var dependencies = ExecutionIrAnalysis.FlattenExpressions(expression)
            .Select(static current => current switch
            {
                ExecutionFieldRead field => $"field:{field.Alias}.{field.FieldName}",
                ExecutionVariableRead variable => $"variable:{variable.Variable.Name}",
                ExecutionScriptVariableRead variable => $"script:{variable.Name}",
                ExecutionScriptParameterRead parameter => $"parameter:{parameter.Name}",
                _ => null
            })
            .Where(static dependency => dependency != null)
            .Select(static dependency => dependency!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static dependency => dependency, StringComparer.Ordinal)
            .ToArray();

        var region = ScalarEvaluationRegion.Root(ownerScope);
        return new ScalarReuseCandidate(
            fingerprint,
            expression.ReturnType.ResolveClrType(),
            ExpressionStabilityAnalyzer.IsStable(expression) ? ColumnStability.Stable : ColumnStability.Volatile,
            dependencies,
            ownerScope,
            region,
            occurrences.Count,
            occurrences.Max(static occurrence => occurrence.Depth + 1),
            EstimatePayload(expression.ReturnType.ResolveClrType()));
    }

    private static int EstimatePayload(Type type)
    {
        if (type.IsValueType)
            return Math.Max(1, System.Runtime.InteropServices.Marshal.SizeOf(type));

        return 8;
    }
}
