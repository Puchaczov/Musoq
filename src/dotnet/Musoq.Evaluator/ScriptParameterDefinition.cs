namespace Musoq.Evaluator;

public sealed record ScriptParameterDefinition(
    string Name,
    Type ParameterType,
    bool HasDefaultValue,
    object? DefaultValue)
{
    public bool IsRequired => !HasDefaultValue;
}
