using System.Collections.Generic;

namespace Musoq.Evaluator.Tests.Schema.RuntimeV2;

public sealed class RuntimeV2RegressionEntity
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public int Value { get; init; }

    public string Category { get; init; } = string.Empty;

    public string Department { get; init; } = string.Empty;

    public int Salary { get; init; }

    public object? Amount { get; init; }

    public static IReadOnlyList<RuntimeV2RegressionEntity> EmptyRows { get; } = [];
}
