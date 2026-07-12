using System;
using System.Linq;

namespace Musoq.Targets.Abstractions;

internal readonly record struct ExecutionTargetId
{
    public ExecutionTargetId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Execution target id cannot be empty.", nameof(value));
        if (!string.Equals(value, value.Trim(), StringComparison.Ordinal) || value.Any(char.IsControl))
            throw new ArgumentException("Execution target id must be trimmed and cannot contain control characters.", nameof(value));

        Value = value;
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value;
    }
}
