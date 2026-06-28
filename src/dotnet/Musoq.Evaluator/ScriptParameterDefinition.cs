namespace Musoq.Evaluator;

public sealed record ScriptParameterDefinition
{
    public ScriptParameterDefinition(
        string name,
        Type parameterType,
        bool hasDefaultValue,
        object? defaultValue)
        : this(ScriptParameterContract.FromLegacy(name, parameterType, hasDefaultValue, defaultValue))
    {
    }

    public ScriptParameterDefinition(ScriptParameterContract contract)
    {
        Contract = contract ?? throw new ArgumentNullException(nameof(contract));
        Name = contract.Name;
        ParameterType = contract.ClrType;
        HasDefaultValue = contract.HasDefaultValue;
        DefaultValue = contract.DefaultValue;
    }

    public string Name { get; }

    public Type ParameterType { get; }

    public bool HasDefaultValue { get; }

    public object? DefaultValue { get; }

    public ScriptParameterContract Contract { get; }

    public bool IsRequired => !HasDefaultValue;
}
