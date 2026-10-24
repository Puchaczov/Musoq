using System.Linq;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

internal static class TypedSourceShapeValidation
{
    public static void Validate(ExecutionPlan plan, DiagnosticContext diagnostics)
    {
        var reads = ExecutionIrAnalysis.FlattenExpressions(plan.Body).OfType<ExecutionFieldRead>().ToArray();
        foreach (var source in plan.Shapes.OfType<SourceEntityShape>())
        {
            if (source.EntityType.ResolveClrType() == typeof(object[]))
            {
                diagnostics.ReportError(DiagnosticCode.MQ3071_SourceContractError,
                    $"Source '{source.Alias}' requires native query-scoped transfer; System.Object[] is not a concrete execution row.");
                continue;
            }
            if (source.GeneratedTypeName is null &&
                source.Fields.Any(static field => field.AccessStrategy is PositionalAccess) &&
                !typeof(IReadOnlyRow).IsAssignableFrom(source.EntityType.ResolveClrType()))
            {
                diagnostics.ReportError(DiagnosticCode.MQ3071_SourceContractError,
                    $"Source '{source.Alias}' has no native typed positional accessor. " +
                    "The provider must use native query-scoped transfer or a concrete IReadOnlyRow contract.");
                continue;
            }
            foreach (var field in source.Fields)
            {
                if (!reads.Any(read => read.Alias == source.Alias &&
                    (read.FieldName == field.Name || read.FieldName.StartsWith(field.Name + ".", StringComparison.Ordinal))))
                    continue;
                if (field.GeneratedTypeName is not null || field.InterpretationShape != null)
                    continue;
                var unresolved = ConcreteValueShape.FindUnresolvedValueType(field.Type.ResolveClrType()) ??
                                 ConcreteValueShape.FindUnresolvedValueType(field.SourceReadType.ResolveClrType());
                if (unresolved is null)
                    continue;

                diagnostics.ReportError(DiagnosticCode.MQ3071_SourceContractError,
                    $"Source field '{field.QualifiedName}' has an unresolved object-valued shape at '{unresolved}'. " +
                    "The provider must declare and supply a concrete value type before compilation.");
            }
        }
    }
}
