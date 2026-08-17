using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator;
using Musoq.Evaluator.Utils;

namespace Musoq.Converter.Build;

// Converter-owned state available only while a descriptor adapts compiler data
// into its target-specific TargetBackendRenderInputs.
internal sealed record TargetRenderInputCompilerState
{
    private readonly IReadOnlyList<Type> _additionalReferenceTypes;
    private readonly IReadOnlyList<ScriptParameterDefinition> _scriptParameterDefinitions;
    private readonly IReadOnlyList<ScriptVariableDefinition> _scriptVariableDefinitions;
    private readonly IReadOnlyList<Assembly> _referenceAssemblies;

    public TargetRenderInputCompilerState(
        string compilationUnitName,
        Type? outputType,
        IEnumerable<Type>? additionalReferenceTypes,
        string? interpreterSourceCode,
        Scope scope,
        IEnumerable<ScriptParameterDefinition>? scriptParameterDefinitions,
        IEnumerable<ScriptVariableDefinition>? scriptVariableDefinitions,
        IEnumerable<Assembly>? referenceAssemblies)
    {
        CompilationUnitName = string.IsNullOrWhiteSpace(compilationUnitName)
            ? throw new ArgumentException("Compilation unit name cannot be null or whitespace.", nameof(compilationUnitName))
            : compilationUnitName;
        OutputType = outputType;
        _additionalReferenceTypes = Freeze(additionalReferenceTypes);
        InterpreterSourceCode = interpreterSourceCode;
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _scriptParameterDefinitions = Freeze(scriptParameterDefinitions);
        _scriptVariableDefinitions = Freeze(scriptVariableDefinitions);
        _referenceAssemblies = Freeze(referenceAssemblies);
    }

    public string CompilationUnitName { get; }

    public Type? OutputType { get; }

    public IReadOnlyList<Type> AdditionalReferenceTypes => _additionalReferenceTypes;

    public string? InterpreterSourceCode { get; }

    public Scope Scope { get; }

    public IReadOnlyList<ScriptParameterDefinition> ScriptParameterDefinitions => _scriptParameterDefinitions;

    public IReadOnlyList<ScriptVariableDefinition> ScriptVariableDefinitions => _scriptVariableDefinitions;

    public IReadOnlyList<Assembly> ReferenceAssemblies => _referenceAssemblies;

    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T>? values)
    {
        return Array.AsReadOnly(values?.ToArray() ?? []);
    }
}
