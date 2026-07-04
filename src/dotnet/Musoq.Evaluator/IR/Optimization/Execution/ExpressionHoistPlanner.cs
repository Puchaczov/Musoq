using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal static class ExpressionHoistPlanner
{
    public static ExpressionHoistPlan Create(
        IEnumerable<ExecutionExpressionCseFacts.HoistOccurrence> occurrences,
        HashSet<string> usedNames,
        HashSet<string>? requiredSignatures = null)
    {
        var counts = new Dictionary<string, ExpressionHoistCandidate>(StringComparer.Ordinal);

        foreach (var occurrence in occurrences)
        {
            if (!counts.TryGetValue(occurrence.Signature, out var candidate))
            {
                counts.Add(
                    occurrence.Signature,
                    new ExpressionHoistCandidate(
                        occurrence.Signature,
                        occurrence.Expression,
                        occurrence.Depth,
                        1,
                        occurrence.IsSafeOrigin ? 1 : 0,
                        counts.Count));
                continue;
            }

            counts[occurrence.Signature] = candidate with
            {
                Count = candidate.Count + 1,
                SafeCount = candidate.SafeCount + (occurrence.IsSafeOrigin ? 1 : 0),
                Depth = Math.Min(candidate.Depth, occurrence.Depth)
            };
        }

        var candidates = counts.Values
            .Where(static candidate => candidate is { Count: > 1, SafeCount: > 0 })
            .Where(candidate => requiredSignatures == null || requiredSignatures.Contains(candidate.Signature))
            .OrderBy(static candidate => candidate.Depth)
            .ThenBy(static candidate => candidate.Ordinal)
            .ToArray();
        if (candidates.Length == 0)
            return new ExpressionHoistPlan([], new Dictionary<string, ExecutionVariable>(StringComparer.Ordinal));

        var variables = candidates.ToDictionary(
            static candidate => candidate.Signature,
            candidate => CreateHoistedVariable(candidate.Expression, usedNames),
            StringComparer.Ordinal);

        var lets = new List<ExecutionLet>(candidates.Length);
        var availableVariables = new Dictionary<string, ExecutionVariable>(StringComparer.Ordinal);
        foreach (var candidate in candidates)
        {
            var variable = variables[candidate.Signature];
            var value = ExpressionCseSubstitution.Replace(candidate.Expression, availableVariables);
            lets.Add(new ExecutionLet(variable, value, ExecutionLetCacheMode.SuppressMethodCache));
            availableVariables.Add(candidate.Signature, variable);
        }

        return new ExpressionHoistPlan(lets, variables);
    }

    private static ExecutionVariable CreateHoistedVariable(
        ExecutionExpression expression,
        HashSet<string> usedNames)
    {
        var candidate = expression switch
        {
            ExecutionFieldRead fieldRead => fieldRead.FieldName,
            ExecutionMethodCall methodCall => methodCall.Method.Name,
            ExecutionMethodTargetReuseCandidate methodTargetCandidate => methodTargetCandidate.MethodCall.Method.Name,
            ExecutionStrictCast strictCast => CreateCastHoistVariableName(strictCast),
            _ => "__expr"
        };

        return new ExecutionVariable(
            CreateHoistedVariableName(candidate, usedNames),
            expression.ReturnType);
    }

    private static string CreateCastHoistVariableName(ExecutionStrictCast strictCast)
    {
        var targetName = NormalizeCastTargetName(strictCast);
        return strictCast.Expression switch
        {
            ExecutionFieldRead fieldRead when !string.IsNullOrWhiteSpace(fieldRead.FieldName) => $"{fieldRead.FieldName}{targetName}",
            ExecutionVariableRead variableRead when !string.IsNullOrWhiteSpace(variableRead.Variable.Name) => $"{variableRead.Variable.Name}{targetName}",
            _ => $"cast{targetName}"
        };
    }

    private static string NormalizeCastTargetName(ExecutionStrictCast strictCast)
    {
        var targetType = Nullable.GetUnderlyingType(strictCast.ReturnType) ?? strictCast.ReturnType;
        return targetType == typeof(string) ? nameof(String) : targetType.Name;
    }

    private static string CreateHoistedVariableName(string candidate, HashSet<string> usedNames)
    {
        var normalized = SyntaxHelper.ToCamelCase(GeneratedRowNamingPolicy.CreateLoweringIdentifierCandidate(candidate, usedNames.Count));
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "__expr";
        if (SyntaxFacts.GetKeywordKind(normalized) != SyntaxKind.None)
            normalized += "Value";

        var variableName = normalized;
        var suffix = 1;
        while (!usedNames.Add(variableName))
        {
            variableName = $"{normalized}{suffix.ToString(CultureInfo.InvariantCulture)}";
            suffix++;
        }

        return variableName;
    }

    private sealed record ExpressionHoistCandidate(
        string Signature,
        ExecutionExpression Expression,
        int Depth,
        int Count,
        int SafeCount,
        int Ordinal);
}

