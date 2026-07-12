using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.Abstractions;

internal sealed record TargetHostAbiImport
{
    public TargetHostAbiImport(
        TargetHostAbiImportKind kind,
        string name,
        string contract,
        int contractVersion,
        TargetHostAbiImportDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        if (contractVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(contractVersion), "Contract version must be positive.");

        if (details.Kind != kind)
        {
            throw new ArgumentException(
                $"ABI import details kind '{details.Kind}' does not match import kind '{kind}'.",
                nameof(details));
        }

        Kind = kind;
        Name = RequireText(name, nameof(name));
        Contract = RequireText(contract, nameof(contract));
        ContractVersion = contractVersion;
        Details = details;
    }

    public static TargetHostAbiImport CreateCustom(
        TargetHostAbiImportKind kind,
        string name,
        string contract,
        int contractVersion = 1,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        return new TargetHostAbiImport(
            kind,
            name,
            contract,
            contractVersion,
            new TargetCustomAbiImportDetails(kind, attributes));
    }

    public TargetHostAbiImportKind Kind { get; }

    public string Name { get; }

    public string Contract { get; }

    public int ContractVersion { get; }

    public TargetHostAbiImportDetails Details { get; }

    public IReadOnlyDictionary<string, string> Attributes => Details.Attributes;

    private static string RequireText(string value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null or whitespace.", parameterName)
            : value;
    }
}

internal sealed record TargetHostAbiInventory
{
    public TargetHostAbiInventory(
        IEnumerable<TargetHostAbiImport>? imports,
        int contractVersion = TargetContractVersions.HostAbi)
    {
        if (contractVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(contractVersion), "ABI contract version must be positive.");

        ContractVersion = contractVersion;
        var values = (imports ?? []).ToArray();
        var duplicate = values
            .GroupBy(static import => (import.Kind, import.Name))
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate != null)
        {
            throw new ArgumentException(
                $"ABI import '{duplicate.Key.Kind}:{duplicate.Key.Name}' is duplicated.",
                nameof(imports));
        }

        Imports = Array.AsReadOnly(
            values
            .OrderBy(static import => import.Kind)
            .ThenBy(static import => import.Name, StringComparer.Ordinal)
            .ThenBy(static import => import.Contract, StringComparer.Ordinal)
            .ThenBy(static import => import.ContractVersion)
            .ThenBy(static import => FormatAttributes(import.Attributes), StringComparer.Ordinal)
            .ToArray());
    }

    public IReadOnlyList<TargetHostAbiImport> Imports { get; }

    public int ContractVersion { get; }

    public static TargetHostAbiInventory Empty { get; } = new([]);

    public bool Requires(TargetHostAbiImportKind kind)
    {
        return Imports.Any(import => import.Kind == kind);
    }

    public void ValidateRuntimeServices(TargetRuntimeServiceRequirements runtimeServices)
    {
        ArgumentNullException.ThrowIfNull(runtimeServices);

        foreach (var (service, fulfillment) in runtimeServices.Fulfillments)
        {
            if (fulfillment != TargetRuntimeServiceFulfillmentKind.HostImport)
                continue;

            var importKind = GetImportKind(service);
            if (!Requires(importKind))
            {
                throw new InvalidOperationException(
                    $"Runtime service '{service}' is host-imported but ABI import '{importKind}' is missing.");
            }
        }

        foreach (var import in Imports)
        {
            var service = GetService(import.Kind);
            if (!runtimeServices.Requires(service))
            {
                throw new InvalidOperationException(
                    $"ABI import '{import.Kind}' has no declared runtime-service fulfillment for '{service}'.");
            }
        }
    }

    public TargetRuntimeServiceRequirements CreateServiceRequirements(
        TargetRuntimeServiceFulfillmentKind fulfillment)
    {
        return new TargetRuntimeServiceRequirements(
            Imports
                .Select(static import => GetService(import.Kind))
                .Distinct()
                .Select(service => new TargetRuntimeServiceFulfillment(service, fulfillment)));
    }

    private static TargetHostAbiImportKind GetImportKind(TargetRuntimeServiceRequirementKind service)
    {
        return service switch
        {
            TargetRuntimeServiceRequirementKind.SourceAccess => TargetHostAbiImportKind.SourceAccess,
            TargetRuntimeServiceRequirementKind.PluginInvocation => TargetHostAbiImportKind.PluginInvocation,
            TargetRuntimeServiceRequirementKind.RowTableShape => TargetHostAbiImportKind.RowShapeTransfer,
            TargetRuntimeServiceRequirementKind.NullSemantics => TargetHostAbiImportKind.NullTypeCoercion,
            TargetRuntimeServiceRequirementKind.Cancellation => TargetHostAbiImportKind.Cancellation,
            TargetRuntimeServiceRequirementKind.Diagnostics => TargetHostAbiImportKind.Diagnostics,
            TargetRuntimeServiceRequirementKind.Profiling => TargetHostAbiImportKind.Profiling,
            _ => throw new ArgumentOutOfRangeException(nameof(service), service, "Unknown runtime service.")
        };
    }

    private static TargetRuntimeServiceRequirementKind GetService(TargetHostAbiImportKind kind)
    {
        return kind switch
        {
            TargetHostAbiImportKind.SourceAccess => TargetRuntimeServiceRequirementKind.SourceAccess,
            TargetHostAbiImportKind.PluginInvocation => TargetRuntimeServiceRequirementKind.PluginInvocation,
            TargetHostAbiImportKind.RowShapeTransfer => TargetRuntimeServiceRequirementKind.RowTableShape,
            TargetHostAbiImportKind.NullTypeCoercion => TargetRuntimeServiceRequirementKind.NullSemantics,
            TargetHostAbiImportKind.Cancellation => TargetRuntimeServiceRequirementKind.Cancellation,
            TargetHostAbiImportKind.Diagnostics => TargetRuntimeServiceRequirementKind.Diagnostics,
            TargetHostAbiImportKind.Profiling => TargetRuntimeServiceRequirementKind.Profiling,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown ABI import kind.")
        };
    }

    private static string FormatAttributes(IReadOnlyDictionary<string, string> attributes)
    {
        return string.Join(
            "|",
            attributes
                .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                .Select(static pair => $"{pair.Key}={pair.Value}"));
    }
}
