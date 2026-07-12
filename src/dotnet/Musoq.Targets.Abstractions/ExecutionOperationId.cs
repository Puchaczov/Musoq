using System;

namespace Musoq.Targets.Abstractions;

internal readonly record struct ExecutionOperationId
{
    public ExecutionOperationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Execution operation id cannot be empty.", nameof(value));

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
