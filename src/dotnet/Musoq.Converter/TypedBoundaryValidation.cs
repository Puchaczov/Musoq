using Musoq.Evaluator;
using Musoq.Evaluator.IR.Execution;
using Musoq.Schema;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter;

internal static class TypedBoundaryValidation
{
    public static bool ValidateSources(Type? outputType, ExecutionPlan plan, DiagnosticContext diagnostics)
    {
        var unresolved = outputType is null ? null : ConcreteValueShape.FindUnresolvedMember(outputType);
        if (unresolved is not null)
            diagnostics.ReportError(DiagnosticCode.MQ3071_SourceContractError,
                $"Typed result has an unresolved object-valued shape at '{unresolved}'. Declare concrete result members before compilation.");

        TypedSourceShapeValidation.Validate(plan, diagnostics);
        if (diagnostics.HasErrors)
            return false;
        TypedExecutionValueValidation.Validate(plan, diagnostics);
        return !diagnostics.HasErrors;
    }
}
