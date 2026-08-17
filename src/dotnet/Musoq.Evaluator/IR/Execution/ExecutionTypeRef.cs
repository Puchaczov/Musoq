using Musoq.Targets.Abstractions;

namespace Musoq.Evaluator.IR.Execution;

public sealed class ExecutionTypeRef : IEquatable<ExecutionTypeRef>
{
    internal ExecutionTypeRef(ExecutionPortableTypeDescriptor portableType)
    {
        Descriptor = portableType ?? throw new ArgumentNullException(nameof(portableType));
    }

    public string StableId => Descriptor.StableName;

    public string DisplayName => Descriptor.DisplayName;

    public ExecutionPortableTypeDescriptor Descriptor { get; }

    public bool Equals(ExecutionTypeRef? other) =>
        other is not null &&
        string.Equals(StableId, other.StableId, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is ExecutionTypeRef other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableId);

    public static bool operator ==(ExecutionTypeRef? left, ExecutionTypeRef? right) => Equals(left, right);

    public static bool operator !=(ExecutionTypeRef? left, ExecutionTypeRef? right) => !Equals(left, right);

    public override string ToString() => DisplayName;
}
