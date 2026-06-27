using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static partial class ScriptVariableInitializerEvaluator
{
    private static ScriptConstantEvaluationResult InvalidOperandFailure(string operation)
    {
        return ScriptConstantEvaluationResult.Failed(
            DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
            $"Script variable initializer contains a non-constant or incompatible {operation}.");
    }

    private static string CreateUnsupportedExpressionMessage(Node expression, string ownerName)
    {
        var prefix = string.IsNullOrEmpty(ownerName)
            ? "Static source argument"
            : $"Script variable '{ownerName}' initializer";

        return $"{prefix} must use only literals, constant operators, and earlier script variables. Unsupported expression: {expression.GetType().Name}.";
    }
}
