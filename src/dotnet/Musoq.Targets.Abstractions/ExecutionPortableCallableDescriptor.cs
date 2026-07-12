using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.Abstractions;

public sealed record ExecutionPortableCallableDescriptor(
    ExecutionPortableCallableKind Kind,
    string StableName,
    string DisplayName)
{
    public ExecutionPortableSymbolPortability Portability { get; init; } =
        ExecutionPortableSymbolPortability.ClrOnly;

    public string PortabilityReason { get; init; } = "No portable callable catalog entry.";

    public string MethodName { get; init; } = string.Empty;

    public ExecutionPortableTypeDescriptor? DeclaringType { get; init; }

    public ExecutionPortableTypeDescriptor? ReturnType { get; init; }

    private IReadOnlyList<ExecutionPortableTypeDescriptor> _parameterTypes = [];

    public IReadOnlyList<ExecutionPortableTypeDescriptor> ParameterTypes
    {
        get => _parameterTypes;
        init => _parameterTypes = Freeze(value);
    }

    public bool IsStatic { get; init; }

    public int GenericArity { get; init; }

    public ExecutionCallableInvocationMode InvocationMode { get; init; }

    public ExecutionIntrinsicCallableKind IntrinsicKind { get; init; }

    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T>? values)
    {
        return Array.AsReadOnly(values?.ToArray() ?? []);
    }
}
