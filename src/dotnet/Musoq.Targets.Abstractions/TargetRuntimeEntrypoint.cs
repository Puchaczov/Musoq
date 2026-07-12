using System;

namespace Musoq.Targets.Abstractions;

internal sealed record TargetRuntimeEntrypoint
{
    public TargetRuntimeEntrypoint(string name, TargetRuntimeEntrypointKind kind, string symbolName)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Entrypoint name cannot be empty.", nameof(name))
            : name;
        Kind = kind;
        SymbolName = string.IsNullOrWhiteSpace(symbolName)
            ? throw new ArgumentException("Entrypoint symbol cannot be empty.", nameof(symbolName))
            : symbolName;
    }

    public string Name { get; }

    public TargetRuntimeEntrypointKind Kind { get; }

    public string SymbolName { get; }
}
