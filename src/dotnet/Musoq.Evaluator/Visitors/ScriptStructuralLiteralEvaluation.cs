using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Visitors;

internal static partial class ScriptVariableInitializerEvaluator
{
    private static ScriptConstantEvaluationResult EvaluateRecord(
        RecordLiteralNode record,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables,
        IReadOnlyDictionary<string, ScriptParameterDefinition>? parameters,
        string ownerName)
    {
        var fields = new List<KeyValuePair<string, StructuralValue>>(record.Fields.Count);
        foreach (var field in record.Fields)
        {
            var evaluation = EvaluateCore(field.Expression, variables, parameters, ownerName);
            if (!evaluation.Success)
                return evaluation;

            fields.Add(new KeyValuePair<string, StructuralValue>(
                field.Name,
                ToStructuralValue(evaluation.Value)));
        }

        try
        {
            return ScriptConstantEvaluationResult.Evaluated(
                StructuralValue.FromRecord(fields),
                typeof(StructuralValue));
        }
        catch (ArgumentException exception)
        {
            return ScriptConstantEvaluationResult.Failed(
                DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
                $"Script variable '{ownerName}' contains an invalid record: {exception.Message}");
        }
    }

    private static ScriptConstantEvaluationResult EvaluateArray(
        ArrayLiteralNode array,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables,
        IReadOnlyDictionary<string, ScriptParameterDefinition>? parameters,
        string ownerName)
    {
        var elements = new StructuralValue[array.Elements.Count];
        for (var index = 0; index < array.Elements.Count; index++)
        {
            var evaluation = EvaluateCore(array.Elements[index], variables, parameters, ownerName);
            if (!evaluation.Success)
                return evaluation;

            elements[index] = ToStructuralValue(evaluation.Value);
        }

        return ScriptConstantEvaluationResult.Evaluated(
            StructuralValue.FromCollection(elements),
            typeof(StructuralValue[]));
    }

    private static StructuralValue ToStructuralValue(object? value)
    {
        return value as StructuralValue ?? StructuralValue.FromScalar(value);
    }
}