using System.Collections.Generic;
using System.Linq;

namespace Musoq.Schema.Optimization;

/// <summary>Declares versioned, opt-in predicate operations a source can evaluate.</summary>
public sealed record SourcePredicateCapabilities
{
    /// <summary>The current source predicate capability contract version.</summary>
    public const int CurrentContractVersion = 1;

    private IReadOnlyList<SourceStringMatchCapability> _stringMatches =
        Array.AsReadOnly(Array.Empty<SourceStringMatchCapability>());

    /// <summary>Gets the declared contract version.</summary>
    public int ContractVersion { get; init; } = CurrentContractVersion;

    /// <summary>Gets the immutable string-match capability declarations.</summary>
    public IReadOnlyList<SourceStringMatchCapability> StringMatches
    {
        get => _stringMatches;
        init => _stringMatches = Freeze(value);
    }

    /// <summary>Gets whether the declaration uses the contract version understood by this runtime.</summary>
    public bool IsKnownVersion => ContractVersion == CurrentContractVersion;

    /// <summary>Gets an immutable declaration with no supported typed predicates.</summary>
    public static SourcePredicateCapabilities None { get; } = new();

    /// <summary>Determines whether a typed predicate is supported at a specific phase.</summary>
    /// <param name="predicate">The typed predicate.</param>
    /// <param name="phase">The requested application phase.</param>
    /// <returns><see langword="true"/> when a known declaration advertises every required dimension.</returns>
    public bool Supports(
        SourcePredicateStringMatch predicate,
        SourcePredicateEvaluationPhase phase)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return IsKnownVersion && StringMatches.Any(capability => capability.Supports(predicate, phase));
    }

    /// <summary>Validates the capability declaration as a well-formed contract.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The contract version is not positive.</exception>
    /// <exception cref="ArgumentException">A declaration is null or repeats a column name.</exception>
    public void Validate()
    {
        if (ContractVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(ContractVersion), ContractVersion, "The contract version must be positive.");

        ArgumentNullException.ThrowIfNull(StringMatches);
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var capability in StringMatches)
        {
            ArgumentNullException.ThrowIfNull(capability);
            if (!columns.Add(capability.Column.Name))
            {
                throw new ArgumentException(
                    $"The string-match capability for column '{capability.Column.Name}' is declared more than once.",
                    nameof(StringMatches));
            }
        }
    }

    private static IReadOnlyList<SourceStringMatchCapability> Freeze(
        IEnumerable<SourceStringMatchCapability> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return Array.AsReadOnly(values.ToArray());
    }
}
