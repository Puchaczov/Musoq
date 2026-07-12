using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution.Portability;
using Musoq.Targets.Abstractions;

namespace Musoq.Evaluator.IR.Execution;

public sealed class ExecutionTypeRef : IEquatable<ExecutionTypeRef>
{
    private ExecutionTypeRef(ExecutionPortableTypeDescriptor portableType, Type clrType)
    {
        Descriptor = portableType ?? throw new ArgumentNullException(nameof(portableType));
        ClrType = clrType ?? throw new ArgumentNullException(nameof(clrType));
    }

    public string StableId => Descriptor.StableName;

    public string DisplayName => Descriptor.DisplayName;

    public ExecutionPortableTypeDescriptor Descriptor { get; }

    internal Type ClrType { get; }

    internal string ClrDisplayName => FormatClrType(ClrType);

    internal static ExecutionTypeRef FromClr(Type clrType) =>
        new(ExecutionPortableSymbolFactory.FromType(clrType), clrType);

    internal static ExecutionTypeRef? FromOptionalClr(Type? clrType) =>
        clrType is null ? null : FromClr(clrType);

    internal static IReadOnlyList<ExecutionTypeRef> FromClrTypes(IEnumerable<Type> clrTypes) =>
        clrTypes.Select(FromClr).ToArray();

    private static string FormatClrType(Type type)
    {
        if (type.IsArray)
            return $"{FormatClrType(type.GetElementType()!)}[]";

        if (!type.IsGenericType)
            return type.FullName ?? type.Name;

        var name = type.GetGenericTypeDefinition().FullName ?? type.Name;
        var tickIndex = name.IndexOf('`', StringComparison.Ordinal);
        if (tickIndex >= 0)
            name = name[..tickIndex];

        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(FormatClrType))}>";
    }

    public bool Equals(ExecutionTypeRef? other) =>
        other is not null &&
        string.Equals(StableId, other.StableId, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is ExecutionTypeRef other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableId);

    public static bool operator ==(ExecutionTypeRef? left, ExecutionTypeRef? right) => Equals(left, right);

    public static bool operator !=(ExecutionTypeRef? left, ExecutionTypeRef? right) => !Equals(left, right);

    public override string ToString() => DisplayName;
}
