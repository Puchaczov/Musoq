using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.Execution.Analysis;

internal static class ExecutionTargetReadinessAnalyzer
{
    public static ExecutionTargetReadinessReport AnalyzeFutureTargets(
        ExecutionTargetCompatibilityReport compatibilityReport)
    {
        return Analyze(compatibilityReport, ExecutionTargetReadinessProfile.FutureTargetProfiles);
    }

    public static ExecutionTargetReadinessReport AnalyzeFutureTargets(
        ExecutionTargetCompatibilityReport compatibilityReport,
        TargetRuntimeContract runtimeContract)
    {
        return Analyze(compatibilityReport, runtimeContract, ExecutionTargetReadinessProfile.FutureTargetProfiles);
    }

    public static ExecutionTargetReadinessReport AnalyzeFutureTargets(
        ExecutionTargetCompatibilityReport compatibilityReport,
        TargetRuntimeContract runtimeContract,
        ExecutionSemanticsContract semanticsContract)
    {
        return Analyze(
            compatibilityReport,
            runtimeContract,
            semanticsContract,
            ExecutionTargetReadinessProfile.FutureTargetProfiles);
    }

    public static ExecutionTargetReadinessReport Analyze(
        ExecutionTargetCompatibilityReport compatibilityReport,
        params ExecutionTargetReadinessProfile[] profiles)
    {
        return Analyze(compatibilityReport, (IReadOnlyList<ExecutionTargetReadinessProfile>)profiles);
    }

    public static ExecutionTargetReadinessReport Analyze(
        ExecutionTargetCompatibilityReport compatibilityReport,
        TargetRuntimeContract runtimeContract,
        params ExecutionTargetReadinessProfile[] profiles)
    {
        return Analyze(compatibilityReport, runtimeContract, (IReadOnlyList<ExecutionTargetReadinessProfile>)profiles);
    }

    public static ExecutionTargetReadinessReport Analyze(
        ExecutionTargetCompatibilityReport compatibilityReport,
        IReadOnlyList<ExecutionTargetReadinessProfile> profiles)
    {
        return Analyze(compatibilityReport, runtimeContract: null, profiles);
    }

    public static ExecutionTargetReadinessReport Analyze(
        ExecutionTargetCompatibilityReport compatibilityReport,
        TargetRuntimeContract? runtimeContract,
        IReadOnlyList<ExecutionTargetReadinessProfile> profiles)
    {
        return Analyze(
            compatibilityReport,
            runtimeContract,
            ExecutionSemanticsContract.Version1,
            profiles);
    }

    public static ExecutionTargetReadinessReport Analyze(
        ExecutionTargetCompatibilityReport compatibilityReport,
        TargetRuntimeContract? runtimeContract,
        ExecutionSemanticsContract semanticsContract,
        IReadOnlyList<ExecutionTargetReadinessProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(compatibilityReport);
        ArgumentNullException.ThrowIfNull(semanticsContract);
        ArgumentNullException.ThrowIfNull(profiles);

        var issues = new List<ExecutionTargetReadinessIssue>();
        var seen = new HashSet<(ExecutionTargetRuntimeFamily RuntimeFamily, ExecutionTargetReadinessCategory Category, string Detail)>();
        var requirements = compatibilityReport.Requirements
            .Concat(runtimeContract != null
                ? ExecutionTargetRuntimeRequirementAnalyzer.Analyze(runtimeContract)
                : [])
            .OrderBy(static requirement => requirement.Kind)
            .ThenBy(static requirement => requirement.Detail, StringComparer.Ordinal)
            .ToArray();

        foreach (var profile in profiles)
        {
            foreach (var requirement in requirements)
            {
                var category = Classify(requirement.Kind);
                AddUnsupportedCategoryIssue(profile, category, requirement, issues, seen);
                AddUnsupportedSymbolPortabilityIssues(profile, requirement, issues, seen);
            }
        }

        return new ExecutionTargetReadinessReport(issues.ToArray(), semanticsContract);
    }

    public static ExecutionTargetReadinessCategory Classify(
        ExecutionTargetRequirementKind requirementKind)
    {
        return requirementKind switch
        {
            ExecutionTargetRequirementKind.ClrTypeUsage => ExecutionTargetReadinessCategory.ClrOnlyTypeUsage,
            ExecutionTargetRequirementKind.MethodInfoCall => ExecutionTargetReadinessCategory.ReflectionMethodInfo,
            ExecutionTargetRequirementKind.SchemaProviderBinding => ExecutionTargetReadinessCategory.SchemaProviderBinding,
            ExecutionTargetRequirementKind.GeneratedClrRow => ExecutionTargetReadinessCategory.GeneratedRowShape,
            ExecutionTargetRequirementKind.PluginInvocation => ExecutionTargetReadinessCategory.PluginInvocation,
            ExecutionTargetRequirementKind.HostSourceAccess => ExecutionTargetReadinessCategory.HostSourceAccess,
            ExecutionTargetRequirementKind.QueryRowSourceAccess => ExecutionTargetReadinessCategory.HostSourceAccess,
            ExecutionTargetRequirementKind.NullTypeCoercion => ExecutionTargetReadinessCategory.NullTypeCoercion,
            ExecutionTargetRequirementKind.ProfilingDiagnostics => ExecutionTargetReadinessCategory.ProfilingDiagnostics,
            ExecutionTargetRequirementKind.Cancellation => ExecutionTargetReadinessCategory.Cancellation,
            ExecutionTargetRequirementKind.ClrOnlyConstant => ExecutionTargetReadinessCategory.ClrOnlyTypeUsage,
            _ => throw new ArgumentOutOfRangeException(nameof(requirementKind), requirementKind, null)
        };
    }

    private static string FormatDiagnostic(
        ExecutionTargetRuntimeFamily runtimeFamily,
        ExecutionTargetReadinessCategory category,
        ExecutionTargetRequirement requirement)
    {
        return $"{runtimeFamily} target does not support {category}: {requirement.Kind}: {requirement.Detail}";
    }

    private static void AddUnsupportedCategoryIssue(
        ExecutionTargetReadinessProfile profile,
        ExecutionTargetReadinessCategory category,
        ExecutionTargetRequirement requirement,
        ICollection<ExecutionTargetReadinessIssue> issues,
        ISet<(ExecutionTargetRuntimeFamily RuntimeFamily, ExecutionTargetReadinessCategory Category, string Detail)> seen)
    {
        if (profile.Supports(category))
            return;

        AddIssue(profile, category, requirement, requirement.Detail, issues, seen);
    }

    private static void AddUnsupportedSymbolPortabilityIssues(
        ExecutionTargetReadinessProfile profile,
        ExecutionTargetRequirement requirement,
        ICollection<ExecutionTargetReadinessIssue> issues,
        ISet<(ExecutionTargetRuntimeFamily RuntimeFamily, ExecutionTargetReadinessCategory Category, string Detail)> seen)
    {
        if (requirement.TypeSymbol is { } typeSymbol &&
            !profile.SupportsTypeSymbolPortability(typeSymbol.Portability))
        {
            AddIssue(
                profile,
                ExecutionTargetReadinessCategory.ClrOnlyTypeUsage,
                requirement,
                FormatSymbolPortabilityDetail(requirement.Detail, typeSymbol.StableName, typeSymbol.Portability, typeSymbol.PortabilityReason),
                issues,
                seen,
                respectCategorySupport: false);
        }

        if (requirement.CallableSymbol is { } callableSymbol &&
            !profile.SupportsCallableSymbolPortability(callableSymbol.Portability))
        {
            AddIssue(
                profile,
                ExecutionTargetReadinessCategory.ReflectionMethodInfo,
                requirement,
                FormatSymbolPortabilityDetail(requirement.Detail, callableSymbol.StableName, callableSymbol.Portability, callableSymbol.PortabilityReason),
                issues,
                seen,
                respectCategorySupport: false);
        }
    }

    private static void AddIssue(
        ExecutionTargetReadinessProfile profile,
        ExecutionTargetReadinessCategory category,
        ExecutionTargetRequirement requirement,
        string detail,
        ICollection<ExecutionTargetReadinessIssue> issues,
        ISet<(ExecutionTargetRuntimeFamily RuntimeFamily, ExecutionTargetReadinessCategory Category, string Detail)> seen,
        bool respectCategorySupport = true)
    {
        if (respectCategorySupport && profile.Supports(category))
            return;

        if (!seen.Add((profile.RuntimeFamily, category, detail)))
            return;

        var issueRequirement = string.Equals(requirement.Detail, detail, StringComparison.Ordinal)
            ? requirement
            : requirement with { Detail = detail };
        issues.Add(new ExecutionTargetReadinessIssue(
            profile.RuntimeFamily,
            category,
            issueRequirement,
            FormatDiagnostic(profile.RuntimeFamily, category, issueRequirement)));
    }

    private static string FormatSymbolPortabilityDetail(
        string requirementDetail,
        string stableName,
        ExecutionPortableSymbolPortability portability,
        string portabilityReason)
    {
        return $"{requirementDetail} -> {stableName} [{portability}] {portabilityReason}";
    }
}
