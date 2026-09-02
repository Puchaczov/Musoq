using System.Linq;

namespace Musoq.Schema.Optimization;

/// <summary>
///     Represents a bound enum constant without boxing either the primitive carrier or a CLR enum.
/// </summary>
public sealed record SourcePredicateEnumLiteral : SourcePredicateExpression
{
    public SourcePredicateEnumLiteral(EnumScalarValue value, string enumFingerprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(enumFingerprint);
        if (enumFingerprint.Length != 64 || enumFingerprint.Any(static character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "An enum predicate fingerprint must be a 64-character hexadecimal digest.",
                nameof(enumFingerprint));
        }

        Value = value;
        EnumFingerprint = enumFingerprint.ToUpperInvariant();
    }

    public EnumScalarValue Value { get; init; }

    public string EnumFingerprint { get; init; }
}
