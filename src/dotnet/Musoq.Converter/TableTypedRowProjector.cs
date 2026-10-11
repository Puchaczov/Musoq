using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TypedOutput;

namespace Musoq.Converter;

internal sealed class TableTypedRowProjector<TOut>(System.Func<Row, TOut> project)
{
    private readonly System.Func<Row, TOut> _project = project;

    public static TableTypedRowProjector<TOut> Create(IReadOnlyList<TypedOutputColumn> columns)
    {
        var binding = TypedOutputBinder.Create(typeof(TOut), columns);
        var row = Expression.Parameter(typeof(Row), "row");
        var output = CreateOutput(binding, row);
        return new TableTypedRowProjector<TOut>(Expression.Lambda<System.Func<Row, TOut>>(output, row).Compile());
    }

    public TOut Project(Row row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return _project(row);
    }

    private static Expression CreateOutput(TypedOutputBindingPlan binding, ParameterExpression row)
    {
        if (binding.Constructor is not null)
            return Expression.New(binding.Constructor, binding.ConstructorBindings.Select(argument =>
                ReadColumn(row, argument.Column, argument.TargetType)));

        return Expression.MemberInit(Expression.New(typeof(TOut)), binding.MemberBindings.Select(member =>
            Expression.Bind(member.Member, ReadColumn(row, member.Column, member.TargetType))));
    }

    private static Expression ReadColumn(ParameterExpression row, TypedOutputColumn column, System.Type targetType)
    {
        var accessor = typeof(Row).GetMethod(nameof(Row.GetValue))!.MakeGenericMethod(column.Type);
        var read = Expression.Call(row, accessor, Expression.Constant(column.Index));
        return column.Type == targetType ? read : Expression.Convert(read, targetType);
    }
}
