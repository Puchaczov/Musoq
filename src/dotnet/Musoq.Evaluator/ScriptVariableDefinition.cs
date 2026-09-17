using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator;

public sealed record ScriptVariableDefinition(
    string Name,
    Type VariableType,
    object? Value,
    bool CanUseConstKeyword,
    StructuralTypeDescriptor? StructuralType = null);