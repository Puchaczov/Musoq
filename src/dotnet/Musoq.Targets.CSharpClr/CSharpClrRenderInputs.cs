using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.Utils;

namespace Musoq.Targets.CSharpClr;

internal sealed record CSharpClrRenderInputs : TargetBackendRenderInputs
{
    private IReadOnlyList<Type> _additionalReferenceTypes = Array.AsReadOnly(Array.Empty<Type>());
    private IReadOnlyList<ScriptParameterDefinition> _scriptParameterDefinitions = Array.AsReadOnly(Array.Empty<ScriptParameterDefinition>());
    private IReadOnlyList<ScriptVariableDefinition> _scriptVariableDefinitions = Array.AsReadOnly(Array.Empty<ScriptVariableDefinition>());
    private IReadOnlyList<Assembly> _referenceAssemblies = Array.AsReadOnly(Array.Empty<Assembly>());

    public CSharpClrRenderInputs()
        : base(ExecutionTargetIds.CSharpClr)
    {
    }

    public required CSharpClrExecutionBindingContext ExecutionBindings { get; init; }

    public required CompilationOptions CompilationOptions { get; init; }

    public required TargetRenderProfile RenderProfile { get; init; }

    public required string AssemblyName { get; init; }

    public required string NamespaceName { get; init; }

    public required QueryResultMode QueryResultMode { get; init; }

    public Type? OutputType { get; init; }

    public required IReadOnlyList<Type> AdditionalReferenceTypes
    {
        get => _additionalReferenceTypes;
        init => _additionalReferenceTypes = Freeze(value);
    }

    public string? InterpreterSourceCode { get; init; }

    public required Scope Scope { get; init; }

    public required IReadOnlyList<ScriptParameterDefinition> ScriptParameterDefinitions
    {
        get => _scriptParameterDefinitions;
        init => _scriptParameterDefinitions = Freeze(value);
    }

    public required IReadOnlyList<ScriptVariableDefinition> ScriptVariableDefinitions
    {
        get => _scriptVariableDefinitions;
        init => _scriptVariableDefinitions = Freeze(value);
    }

    public required IReadOnlyList<Assembly> ReferenceAssemblies
    {
        get => _referenceAssemblies;
        init => _referenceAssemblies = Freeze(value);
    }

    public bool EnableContextualExecution { get; init; }

    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T>? values)
    {
        return Array.AsReadOnly(values?.ToArray() ?? []);
    }
}
