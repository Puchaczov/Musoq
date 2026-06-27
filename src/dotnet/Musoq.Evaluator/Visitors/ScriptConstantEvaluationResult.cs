using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Visitors;

internal sealed record ScriptConstantEvaluationResult(
    bool Success,
    object? Value,
    Type ReturnType,
    DiagnosticCode? ErrorCode,
    string Error)
{
    public static ScriptConstantEvaluationResult Evaluated(object? value, Type returnType)
    {
        return new ScriptConstantEvaluationResult(true, value, returnType, null, string.Empty);
    }

    public static ScriptConstantEvaluationResult Failed(DiagnosticCode code, string error)
    {
        return new ScriptConstantEvaluationResult(false, null, typeof(object), code, error);
    }
}