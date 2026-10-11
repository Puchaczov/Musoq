namespace Musoq.Schema;

/// <summary>Immutable public column metadata for a described query's native result shape.</summary>
public readonly record struct QueryColumnDescription(string Name, int Index, string TypeName);
