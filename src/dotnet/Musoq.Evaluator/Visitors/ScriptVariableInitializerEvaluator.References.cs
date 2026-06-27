using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static partial class ScriptVariableInitializerEvaluator
{
    private static ScriptConstantEvaluationResult EvaluateParameterReference(
        ParameterReferenceNode reference,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables,
        IReadOnlyDictionary<string, ScriptParameterDefinition>? parameters,
        string ownerName)
    {
        if (variables.TryGetValue(reference.Name, out var variable))
            return ScriptConstantEvaluationResult.Evaluated(variable.Value, variable.VariableType);

        if (parameters != null && parameters.ContainsKey(reference.Name))
        {
            return ScriptConstantEvaluationResult.Failed(
                DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
                $"Script variable '{ownerName}' initializer cannot reference runtime parameter '${reference.Name}'.");
        }

        return ScriptConstantEvaluationResult.Failed(
            DiagnosticCode.MQ3066_ScriptVariableUsedBeforeDeclaration,
            string.IsNullOrEmpty(ownerName)
                ? $"Script variable '${reference.Name}' is not declared before it is used."
                : $"Script variable '{ownerName}' initializer references '${reference.Name}' before it is declared.");
    }

    private static ScriptConstantEvaluationResult EvaluateVariableReference(
        ScriptVariableReferenceNode reference,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables,
        string ownerName)
    {
        if (variables.TryGetValue(reference.Name, out var variable))
            return ScriptConstantEvaluationResult.Evaluated(variable.Value, variable.VariableType);

        return ScriptConstantEvaluationResult.Failed(
            DiagnosticCode.MQ3066_ScriptVariableUsedBeforeDeclaration,
            string.IsNullOrEmpty(ownerName)
                ? $"Script variable '${reference.Name}' is not declared before it is used."
                : $"Script variable '{ownerName}' initializer references '${reference.Name}' before it is declared.");
    }
}
