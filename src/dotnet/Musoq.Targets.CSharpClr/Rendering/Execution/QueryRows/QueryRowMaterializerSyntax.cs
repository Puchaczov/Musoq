using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Targets.CSharpClr;

internal static class QueryRowMaterializerSyntax
{
    public static string CreateReadExpression(ExecutionQueryRowField field)
    {
        var fieldType = field.FieldType.RequireClrType();
        var sourceReadType = field.SourceReadType.RequireClrType();
        var read = $"reader.Read<{EvaluationHelper.GetCastableType(sourceReadType)}>({field.Slot})";

        return fieldType == sourceReadType
            ? read
            : $"({EvaluationHelper.GetCastableType(fieldType)}){read}";
    }
}
