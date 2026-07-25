using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Targets.Execution;

internal sealed record ExecutionTargetCapabilities(
    IReadOnlySet<ExecutionTargetRequirementKind> SupportedRequirementKinds,
    IReadOnlySet<ExecutionTargetRequirementKind> SupportedRuntimeRequirementKinds,
    IReadOnlySet<ExecutionPortableSymbolPortability> SupportedTypeSymbolPortabilities,
    IReadOnlySet<ExecutionPortableSymbolPortability> SupportedCallableSymbolPortabilities,
    IReadOnlySet<ExecutionOperationId> SupportedOperations,
    IReadOnlySet<int> SupportedSemanticsVersions,
    IReadOnlySet<string> SupportedSemanticsFingerprints,
    IReadOnlySet<ExecutionTargetFeatureKind> SupportedFeatureKinds)
{
    private static readonly ExecutionPortableSymbolPortability[] PortableTargetSymbolPortabilities =
    [
        ExecutionPortableSymbolPortability.Portable,
        ExecutionPortableSymbolPortability.HostImport
    ];

    private static readonly ExecutionPortableSymbolPortability[] CSharpClrSymbolPortabilities =
    [
        ExecutionPortableSymbolPortability.Portable,
        ExecutionPortableSymbolPortability.HostImport,
        ExecutionPortableSymbolPortability.ClrOnly
    ];

    public static ExecutionTargetCapabilities CSharpClr { get; } = Create(
        [
            ExecutionTargetRequirementKind.ClrTypeUsage,
            ExecutionTargetRequirementKind.MethodInfoCall,
            ExecutionTargetRequirementKind.SchemaProviderBinding,
            ExecutionTargetRequirementKind.GeneratedClrRow,
            ExecutionTargetRequirementKind.PluginInvocation,
            ExecutionTargetRequirementKind.HostSourceAccess,
            ExecutionTargetRequirementKind.NullTypeCoercion,
            ExecutionTargetRequirementKind.ProfilingDiagnostics,
            ExecutionTargetRequirementKind.Cancellation,
            ExecutionTargetRequirementKind.ClrOnlyConstant
        ],
        [
            ExecutionTargetRequirementKind.HostSourceAccess,
            ExecutionTargetRequirementKind.GeneratedClrRow,
            ExecutionTargetRequirementKind.PluginInvocation,
            ExecutionTargetRequirementKind.NullTypeCoercion,
            ExecutionTargetRequirementKind.Cancellation,
            ExecutionTargetRequirementKind.ProfilingDiagnostics
        ],
        CSharpClrSymbolPortabilities,
        CSharpClrSymbolPortabilities,
        ExecutionOperationCatalog.CSharpClrSupportedOperationIds,
        [ExecutionSemanticsContract.Version1.Version],
        [
            ExecutionTargetFeatureKind.ConstantKind,
            ExecutionTargetFeatureKind.BinaryOperation,
            ExecutionTargetFeatureKind.UnaryOperation,
            ExecutionTargetFeatureKind.StrictCastTarget,
            ExecutionTargetFeatureKind.Callable,
            ExecutionTargetFeatureKind.CallableKind,
            ExecutionTargetFeatureKind.SourceKind,
            ExecutionTargetFeatureKind.ReadModifier,
            ExecutionTargetFeatureKind.TypePortability,
            ExecutionTargetFeatureKind.Container,
            ExecutionTargetFeatureKind.DynamicValue
        ]);

    public static ExecutionTargetCapabilities Create(params ExecutionTargetRequirementKind[] supportedRequirementKinds)
    {
        ArgumentNullException.ThrowIfNull(supportedRequirementKinds);

        return new ExecutionTargetCapabilities(
            supportedRequirementKinds.ToFrozenSet(),
            Array.Empty<ExecutionTargetRequirementKind>().ToFrozenSet(),
            PortableTargetSymbolPortabilities.ToFrozenSet(),
            PortableTargetSymbolPortabilities.ToFrozenSet(),
            Array.Empty<ExecutionOperationId>().ToFrozenSet(),
            Array.Empty<int>().ToFrozenSet(),
            Array.Empty<string>().ToFrozenSet(StringComparer.Ordinal),
            Array.Empty<ExecutionTargetFeatureKind>().ToFrozenSet());
    }

    public static ExecutionTargetCapabilities Create(
        IEnumerable<ExecutionTargetRequirementKind> supportedRequirementKinds,
        IEnumerable<ExecutionTargetRequirementKind> supportedRuntimeRequirementKinds)
    {
        ArgumentNullException.ThrowIfNull(supportedRequirementKinds);
        ArgumentNullException.ThrowIfNull(supportedRuntimeRequirementKinds);

        return new ExecutionTargetCapabilities(
            supportedRequirementKinds.ToFrozenSet(),
            supportedRuntimeRequirementKinds.ToFrozenSet(),
            PortableTargetSymbolPortabilities.ToFrozenSet(),
            PortableTargetSymbolPortabilities.ToFrozenSet(),
            Array.Empty<ExecutionOperationId>().ToFrozenSet(),
            Array.Empty<int>().ToFrozenSet(),
            Array.Empty<string>().ToFrozenSet(StringComparer.Ordinal),
            Array.Empty<ExecutionTargetFeatureKind>().ToFrozenSet());
    }

    public static ExecutionTargetCapabilities Create(
        IEnumerable<ExecutionTargetRequirementKind> supportedRequirementKinds,
        IEnumerable<ExecutionTargetRequirementKind> supportedRuntimeRequirementKinds,
        IEnumerable<ExecutionPortableSymbolPortability> supportedTypeSymbolPortabilities,
        IEnumerable<ExecutionPortableSymbolPortability> supportedCallableSymbolPortabilities,
        IEnumerable<ExecutionOperationId>? supportedOperations = null,
        IEnumerable<int>? supportedSemanticsVersions = null,
        IEnumerable<ExecutionTargetFeatureKind>? supportedFeatureKinds = null)
    {
        ArgumentNullException.ThrowIfNull(supportedRequirementKinds);
        ArgumentNullException.ThrowIfNull(supportedRuntimeRequirementKinds);
        ArgumentNullException.ThrowIfNull(supportedTypeSymbolPortabilities);
        ArgumentNullException.ThrowIfNull(supportedCallableSymbolPortabilities);

        return new ExecutionTargetCapabilities(
            supportedRequirementKinds.ToFrozenSet(),
            supportedRuntimeRequirementKinds.ToFrozenSet(),
            supportedTypeSymbolPortabilities.ToFrozenSet(),
            supportedCallableSymbolPortabilities.ToFrozenSet(),
            (supportedOperations ?? []).ToFrozenSet(),
            (supportedSemanticsVersions ?? []).ToFrozenSet(),
            CreateKnownSemanticsFingerprints(supportedSemanticsVersions),
            (supportedFeatureKinds ?? []).ToFrozenSet());
    }

    public static ExecutionTargetCapabilities CreateForSemantics(
        IEnumerable<int> supportedSemanticsVersions) =>
        Create([], [], PortableTargetSymbolPortabilities, PortableTargetSymbolPortabilities, [], supportedSemanticsVersions);

    public ExecutionTargetCapabilityValidationResult Validate(ExecutionTargetOperationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var unsupported = report.Operations
            .Where(usage => !SupportedOperations.Contains(usage.OperationId))
            .Select(static usage => usage.OperationId)
            .ToArray();

        return new ExecutionTargetCapabilityValidationResult([], unsupported, [], [], []);
    }

    public ExecutionTargetCapabilityValidationResult Validate(ExecutionSemanticsContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        if (!SupportedSemanticsVersions.Contains(contract.Version))
            return new ExecutionTargetCapabilityValidationResult([], [], [contract.Version], [], []);

        return SupportedSemanticsFingerprints.Contains(contract.Fingerprint)
            ? new ExecutionTargetCapabilityValidationResult([], [], [], [], [])
            : new ExecutionTargetCapabilityValidationResult([], [], [], [contract.Fingerprint], []);
    }

    public ExecutionTargetCapabilityValidationResult Validate(ExecutionTargetCompatibilityReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var unsupported = FindUnsupportedRequirements(
            report.Requirements,
            SupportedRequirementKinds);

        return new ExecutionTargetCapabilityValidationResult(unsupported, [], [], [], []);
    }

    public ExecutionTargetCapabilityValidationResult Validate(TargetRuntimeContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var unsupported = FindUnsupportedRequirements(
            ExecutionTargetRuntimeRequirementAnalyzer.Analyze(contract),
            SupportedRuntimeRequirementKinds);

        return new ExecutionTargetCapabilityValidationResult(unsupported, [], [], [], []);
    }

    public ExecutionTargetCapabilityValidationResult Validate(
        ExecutionTargetCompatibilityReport report,
        TargetRuntimeContract contract)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(contract);

        var unsupported = Validate(report)
            .UnsupportedRequirements
            .Concat(Validate(contract).UnsupportedRequirements)
            .OrderBy(static requirement => requirement.Kind)
            .ThenBy(static requirement => requirement.Detail, StringComparer.Ordinal)
            .ToArray();

        return new ExecutionTargetCapabilityValidationResult(unsupported, [], [], [], []);
    }

    public ExecutionTargetCapabilityValidationResult Validate(ExecutionTargetFeatureReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var unsupported = report.Features
            .Where(feature => !SupportedFeatureKinds.Contains(feature.Kind))
            .ToArray();

        return new ExecutionTargetCapabilityValidationResult([], [], [], [], unsupported);
    }

    public ExecutionTargetCapabilityValidationResult Validate(
        ExecutionTargetOperationReport operationReport,
        ExecutionSemanticsContract semanticsContract,
        ExecutionTargetCompatibilityReport report,
        TargetRuntimeContract contract,
        ExecutionTargetFeatureReport featureReport)
    {
        ArgumentNullException.ThrowIfNull(operationReport);
        ArgumentNullException.ThrowIfNull(semanticsContract);
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(featureReport);

        var requirementValidation = Validate(report, contract);

        return new ExecutionTargetCapabilityValidationResult(
            requirementValidation.UnsupportedRequirements,
            Validate(operationReport).UnsupportedOperations,
            Validate(semanticsContract).UnsupportedSemanticsVersions,
            Validate(semanticsContract).UnsupportedSemanticsFingerprints,
            Validate(featureReport).UnsupportedFeatures);
    }

    private IReadOnlyList<ExecutionTargetRequirement> FindUnsupportedRequirements(
        IEnumerable<ExecutionTargetRequirement> requirements,
        IReadOnlySet<ExecutionTargetRequirementKind> supportedKinds)
    {
        var unsupported = new List<ExecutionTargetRequirement>();
        foreach (var requirement in requirements)
        {
            if (!supportedKinds.Contains(requirement.Kind))
            {
                unsupported.Add(requirement);
                continue;
            }

            if (requirement.TypeSymbol is { } typeSymbol &&
                !SupportedTypeSymbolPortabilities.Contains(typeSymbol.Portability))
            {
                unsupported.Add(requirement with
                {
                    Detail = FormatUnsupportedSymbol(requirement.Detail, typeSymbol)
                });
            }

            if (requirement.CallableSymbol is { } callableSymbol &&
                !SupportedCallableSymbolPortabilities.Contains(callableSymbol.Portability))
            {
                unsupported.Add(requirement with
                {
                    Detail = FormatUnsupportedSymbol(requirement.Detail, callableSymbol)
                });
            }
        }

        return unsupported;
    }

    private static string FormatUnsupportedSymbol(
        string requirementDetail,
        ExecutionPortableTypeDescriptor symbol)
    {
        return $"{requirementDetail} -> {symbol.StableName} [{symbol.Portability}] {symbol.PortabilityReason}";
    }

    private static string FormatUnsupportedSymbol(
        string requirementDetail,
        ExecutionPortableCallableDescriptor symbol)
    {
        return $"{requirementDetail} -> {symbol.StableName} [{symbol.Portability}] {symbol.PortabilityReason}";
    }

    private static IReadOnlySet<string> CreateKnownSemanticsFingerprints(
        IEnumerable<int>? supportedSemanticsVersions)
    {
        var versions = (supportedSemanticsVersions ?? []).ToHashSet();
        return ExecutionSemanticsContract.KnownContracts
            .Where(contract => versions.Contains(contract.Version))
            .Select(static contract => contract.Fingerprint)
            .ToFrozenSet(StringComparer.Ordinal);
    }
}
