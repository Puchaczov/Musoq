using System.Text;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Bindings;

internal static class WindowRegistrationIdentity
{
    internal static string Create(WindowRegistration registration)
    {
        var builder = new StringBuilder();
        builder.Append(registration.Function?.ToString() ?? registration.FunctionName);
        if (registration.NumbersQualifiedRows)
            builder.Append(".QualifiedResultPosition");
        builder.Append('\u001E');
        builder.Append(registration.ReturnType.AssemblyQualifiedName);
        builder.Append('\u001E');
        builder.Append(registration.Frame?.Id);
        builder.Append('\u001E');
        AppendExpressions(builder, registration.PartitionKeys);
        builder.Append('\u001E');
        AppendOrderFields(builder, registration.OrderKeys);
        builder.Append('\u001E');
        AppendExpressions(builder, registration.ValueArguments);
        builder.Append('\u001E');
        if (registration.FilterPredicate != null)
            builder.Append(IrExpressionPrinter.PrintForBinding(registration.FilterPredicate));
        return builder.ToString();
    }

    private static void AppendExpressions(StringBuilder builder, IrExpression[] expressions)
    {
        for (var index = 0; index < expressions.Length; index++)
        {
            if (index > 0)
                builder.Append('\u001F');

            builder.Append(IrExpressionPrinter.PrintForBinding(expressions[index]));
        }
    }

    private static void AppendOrderFields(StringBuilder builder, OrderField[] orderFields)
    {
        for (var index = 0; index < orderFields.Length; index++)
        {
            if (index > 0)
                builder.Append('\u001F');

            var orderField = orderFields[index];
            builder.Append(orderField.Descending ? "DESC:" : "ASC:").Append(orderField.NullOrdering).Append(':');
            builder.Append(IrExpressionPrinter.PrintForBinding(orderField.Expression));
        }
    }
}
