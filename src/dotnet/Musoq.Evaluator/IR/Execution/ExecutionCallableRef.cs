using Musoq.Evaluator.IR.Execution.Portability;
using Musoq.Targets.Abstractions;

namespace Musoq.Evaluator.IR.Execution;

public sealed class ExecutionCallableRef : IEquatable<ExecutionCallableRef>
{
    internal ExecutionCallableRef(ExecutionPortableCallableDescriptor portableCallable)
    {
        Descriptor = portableCallable ?? throw new ArgumentNullException(nameof(portableCallable));
    }

    public string StableId => Descriptor.StableName;

    public string DisplayName => Descriptor.DisplayName;

    public string MethodName => Descriptor.MethodName;

    internal bool IsStatic => Descriptor.IsStatic;

    public ExecutionPortableCallableDescriptor Descriptor { get; }

    public bool Equals(ExecutionCallableRef? other) =>
        other is not null && string.Equals(StableId, other.StableId, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is ExecutionCallableRef other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableId);

    public static bool operator ==(ExecutionCallableRef? left, ExecutionCallableRef? right) => Equals(left, right);

    public static bool operator !=(ExecutionCallableRef? left, ExecutionCallableRef? right) => !Equals(left, right);

    public override string ToString() => DisplayName;
}
