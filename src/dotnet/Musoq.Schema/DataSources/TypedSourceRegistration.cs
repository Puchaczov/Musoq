using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Schema.DataSources;

/// <summary>Describes an explicitly registered typed row source and its overload set.</summary>
public sealed class TypedSourceRegistration
{
    public TypedSourceRegistration(
        string name,
        Type sourceType,
        Type rowType,
        IEnumerable<TypedSourceOverloadDescriptor> overloads)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
        RowType = rowType ?? throw new ArgumentNullException(nameof(rowType));

        var descriptors = (overloads ?? throw new ArgumentNullException(nameof(overloads)))
            .OrderBy(static descriptor => CanonicalSignature(descriptor.Contract), StringComparer.Ordinal)
            .ThenBy(static descriptor => descriptor.StableId, StringComparer.Ordinal)
            .ToArray();
        if (descriptors.Length == 0)
            throw new ArgumentException("A typed source must expose at least one public constructor.", nameof(overloads));

        var duplicateId = descriptors
            .GroupBy(static descriptor => descriptor.StableId, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicateId != null)
            throw new ArgumentException($"Typed source overload id '{duplicateId.Key}' is duplicated.", nameof(overloads));

        var duplicateSignature = descriptors
            .GroupBy(static descriptor => CanonicalSignature(descriptor.Contract), StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicateSignature != null)
            throw new ArgumentException(
                $"Typed source '{name}' has duplicate SQL signature '{duplicateSignature.Key}'.",
                nameof(overloads));

        Overloads = new ReadOnlyCollection<TypedSourceOverloadDescriptor>(descriptors);
    }

    public string Name { get; }
    public Type SourceType { get; }
    public Type RowType { get; }
    public IReadOnlyList<TypedSourceOverloadDescriptor> Overloads { get; }

    private static string CanonicalSignature(StructuralInputContract contract)
    {
        return string.Join(
            "|",
            contract.Parameters.Select(static parameter =>
                $"{parameter.Name.ToLowerInvariant()}:{parameter.Type.ToCanonicalSql()}:{parameter.Required}:{parameter.Default.CanonicalText}"));
    }
}

/// <summary>Describes one deterministic typed-source constructor overload.</summary>
public sealed class TypedSourceOverloadDescriptor
{
    public TypedSourceOverloadDescriptor(
        string stableId,
        ConstructorInfo constructor,
        StructuralInputContract contract,
        bool injectsExecutionContext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stableId);
        StableId = stableId;
        Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
        Contract = contract ?? throw new ArgumentNullException(nameof(contract));
        InjectsExecutionContext = injectsExecutionContext;
    }

    public string StableId { get; }
    public ConstructorInfo Constructor { get; }
    public StructuralInputContract Contract { get; }
    public bool InjectsExecutionContext { get; }
}

/// <summary>Options for typed source registration.</summary>
public sealed record TypedSourceRegistrationOptions(StructuralInputLimits Limits)
{
    public static TypedSourceRegistrationOptions Default { get; } = new(StructuralInputLimits.Default);
}

/// <summary>Exposes source-construction metadata kept separate from table constructors.</summary>
public interface ITypedSourceSchema
{
    bool TryGetTypedSourceRegistration(
        string name,
        out TypedSourceRegistration registration);
}
