using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class ColumnInstanceMethodValidation
{
    public static void RejectColumnOwner(TableSymbol table, AccessMethodNode method)
    {
        var span = method.FullSpan;
        if (span.IsEmpty)
            span = method.SpanOrEmpty();
        var columnOwner = table.GetTableByColumnName(method.Alias, span);
        if (columnOwner.Table == null)
            return;

        throw new VisitorException(
            nameof(BuildMetadataAndInferTypesVisitor),
            "BindMethodOwner",
            $"Column '{method.Alias}' is a value, not a method owner. " +
            $"Instance method calls such as '{method.Alias}.{method.Name}()' are not supported. " +
            "Use a registered schema/library function.",
            DiagnosticCode.MQ3117_UnsupportedColumnInstanceMethod,
            span);
    }
}
