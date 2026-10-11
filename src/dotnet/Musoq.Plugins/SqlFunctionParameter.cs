namespace Musoq.Plugins;

/// <summary>Describes a SQL argument without carrying an execution-time value.</summary>
/// <param name="Name">SQL argument name.</param>
/// <param name="Type">Declared CLR argument type.</param>
/// <param name="Optional">Whether SQL may omit the argument.</param>
/// <param name="IsAggregateParent">Whether the argument selects parent-group depth.</param>
public sealed record SqlFunctionParameter(string Name, Type Type, bool Optional, bool IsAggregateParent);
