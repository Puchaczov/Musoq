using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Musoq.Schema;

/// <summary>
///     Portable, immutable logical identity and member contract for an enum.
/// </summary>
public sealed partial class EnumTypeDescriptor : IEquatable<EnumTypeDescriptor>
{
    private const string FingerprintFormat = "musoq-enum-v1";
    private readonly FrozenDictionary<string, EnumScalarValue> _membersByName;
    private readonly FrozenDictionary<EnumScalarValue, string> _canonicalNamesByValue;

    public EnumTypeDescriptor(
        string displayName,
        EnumTypeOrigin origin,
        EnumUnderlyingKind underlyingKind,
        bool isFlags,
        IReadOnlyList<EnumMemberDescriptor> members)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(members);

        if (!Enum.IsDefined(origin))
            throw new ArgumentOutOfRangeException(nameof(origin), origin, "Unknown enum type origin.");
        if (!Enum.IsDefined(underlyingKind))
            throw new ArgumentOutOfRangeException(nameof(underlyingKind), underlyingKind,
                "Unknown enum backing kind.");

        var memberCopy = members.ToArray();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var membersByName = new Dictionary<string, EnumScalarValue>(memberCopy.Length, StringComparer.Ordinal);
        var canonicalNamesByValue = new Dictionary<EnumScalarValue, string>();
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var member in memberCopy)
        {
            if (member == null)
                throw new ArgumentException("An enum member descriptor cannot be null.", nameof(members));
            if (member.Value.Kind != underlyingKind)
                throw new ArgumentException(
                    $"Enum member '{member.Name}' uses backing kind '{member.Value.Kind}', expected '{underlyingKind}'.",
                    nameof(members));
            if (!names.Add(member.Name))
                throw new ArgumentException(
                    $"Enum member name '{member.Name}' is duplicated or differs from another member only by casing.",
                    nameof(members));

            membersByName.Add(member.Name, member.Value);
            if (!canonicalNamesByValue.TryAdd(member.Value, member.Name))
                aliases.Add(member.Name, canonicalNamesByValue[member.Value]);
        }

        DisplayName = displayName;
        Origin = origin;
        UnderlyingKind = underlyingKind;
        IsFlags = isFlags;
        Members = Array.AsReadOnly(memberCopy);
        Aliases = new ReadOnlyDictionary<string, string>(aliases);
        _membersByName = membersByName.ToFrozenDictionary(StringComparer.Ordinal);
        _canonicalNamesByValue = canonicalNamesByValue.ToFrozenDictionary();
        Fingerprint = ComputeFingerprint(displayName, origin, underlyingKind, isFlags, memberCopy);
    }

    public string DisplayName { get; }

    public EnumTypeOrigin Origin { get; }

    public EnumUnderlyingKind UnderlyingKind { get; }

    public bool IsFlags { get; }

    public IReadOnlyList<EnumMemberDescriptor> Members { get; }

    /// <summary>
    ///     Gets exact alias name to canonical first-declared name mappings.
    /// </summary>
    public IReadOnlyDictionary<string, string> Aliases { get; }

    public string Fingerprint { get; }

    public bool TryGetValue(string memberName, out EnumScalarValue value)
    {
        ArgumentNullException.ThrowIfNull(memberName);
        return _membersByName.TryGetValue(memberName, out value);
    }

    public bool TryGetCanonicalName(EnumScalarValue value, out string? memberName)
    {
        if (value.Kind != UnderlyingKind)
        {
            memberName = null;
            return false;
        }

        return _canonicalNamesByValue.TryGetValue(value, out memberName);
    }

    public bool IsDefined(EnumScalarValue value)
    {
        return value.Kind == UnderlyingKind && _canonicalNamesByValue.ContainsKey(value);
    }

    public bool Equals(EnumTypeDescriptor? other)
    {
        return other != null && string.Equals(Fingerprint, other.Fingerprint, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as EnumTypeDescriptor);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Fingerprint);
    }

    private static string ComputeFingerprint(
        string displayName,
        EnumTypeOrigin origin,
        EnumUnderlyingKind underlyingKind,
        bool isFlags,
        IReadOnlyList<EnumMemberDescriptor> members)
    {
        var builder = new StringBuilder();
        AppendText(builder, FingerprintFormat);
        AppendText(builder, displayName);
        builder.Append((byte)origin).Append('|')
            .Append((byte)underlyingKind).Append('|')
            .Append(isFlags ? '1' : '0').Append('|')
            .Append(members.Count.ToString(CultureInfo.InvariantCulture)).Append('|');

        foreach (var member in members)
        {
            AppendText(builder, member.Name);
            builder.Append(member.Value.RawValue.ToString("X16", CultureInfo.InvariantCulture)).Append('|');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static void AppendText(StringBuilder builder, string value)
    {
        builder.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append('|');
    }
}
