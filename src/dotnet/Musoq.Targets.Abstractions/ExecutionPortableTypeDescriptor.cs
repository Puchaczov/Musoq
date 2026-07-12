using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.Abstractions;

public sealed record ExecutionPortableTypeDescriptor(
    ExecutionPortableTypeKind Kind,
    string StableName,
    string DisplayName)
{
    private IReadOnlyList<ExecutionPortableTypeDescriptor> _arguments = [];
    private IReadOnlyList<ExecutionPortableRowFieldDescriptor> _fields = [];

    public ExecutionPortableSymbolPortability Portability { get; init; } =
        ExecutionPortableSymbolPortability.ClrOnly;

    public string PortabilityReason { get; init; } = "No portable catalog entry.";

    public IReadOnlyList<ExecutionPortableTypeDescriptor> Arguments
    {
        get => _arguments;
        init => _arguments = Freeze(value);
    }

    public int? ArrayRank { get; init; }

    public ExecutionPortableContainerContract? Container { get; init; }

    public IReadOnlyList<ExecutionPortableRowFieldDescriptor> Fields
    {
        get => _fields;
        init => _fields = Freeze(value);
    }

    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T>? values)
    {
        return Array.AsReadOnly(values?.ToArray() ?? []);
    }
}
