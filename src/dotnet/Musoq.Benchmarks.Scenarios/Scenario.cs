namespace Musoq.Benchmarks.Scenarios;

/// <summary>A named SQL workload. SQL and data generation live in this assembly only.</summary>
public sealed record Scenario(string Id, string Category, string Description, string Sql,
    string Specification, bool Ordered = true, bool ValidatePrimitiveTypes = true, string InputUnit = "rows",
    int? RecursiveTerminationSteps = null)
{
    public override string ToString() => Id;
}
