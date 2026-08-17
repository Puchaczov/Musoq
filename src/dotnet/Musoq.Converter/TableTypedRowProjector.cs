using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TypedOutput;

namespace Musoq.Converter;

internal sealed class TableTypedRowProjector<TOut>
{
    private readonly ConstructorInfo? _constructor;
    private readonly Func<TOut>? _createDefault;
    private readonly TypedOutputConstructorBinding[] _constructorBindings;
    private readonly MemberBinding[] _memberBindings;

    private TableTypedRowProjector(
        ConstructorInfo? constructor,
        Func<TOut>? createDefault,
        TypedOutputConstructorBinding[] constructorBindings,
        MemberBinding[] memberBindings)
    {
        _constructor = constructor;
        _createDefault = createDefault;
        _constructorBindings = constructorBindings;
        _memberBindings = memberBindings;
    }

    public static TableTypedRowProjector<TOut> Create(Table table)
    {
        ArgumentNullException.ThrowIfNull(table);

        var columns = table.Columns
            .OrderBy(static column => column.ColumnIndex)
            .Select(static column => new TypedOutputColumn(column.ColumnName, column.ColumnIndex, column.ColumnType))
            .ToArray();
        var plan = TypedOutputBinder.Create(typeof(TOut), columns);

        return new TableTypedRowProjector<TOut>(
            plan.Constructor,
            CreateDefaultFactory(plan.Constructor),
            plan.ConstructorBindings.ToArray(),
            plan.MemberBindings
                .Select(static binding => new MemberBinding(
                    binding.MemberName,
                    binding.TargetType,
                    binding.Column,
                    CreateSetter(binding.Member)))
                .ToArray());
    }

    public TOut Project(Row row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (_constructor != null)
        {
            var values = new object?[_constructorBindings.Length];
            for (var index = 0; index < values.Length; index++)
            {
                var binding = _constructorBindings[index];
                values[index] = ReadValue(row, binding.Column, binding.TargetType);
            }

            try
            {
                return (TOut)_constructor.Invoke(values);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        var output = (_createDefault ?? throw new InvalidOperationException(
            $"Could not create typed output '{typeof(TOut).FullName}'.")).Invoke();
        if (output == null)
            throw new InvalidOperationException($"Could not create typed output '{typeof(TOut).FullName}'.");

        foreach (var binding in _memberBindings)
        {
            var value = ReadValue(row, binding.Column, binding.TargetType);
            try
            {
                binding.Set(output, value);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        return output;
    }

    private static object? ReadValue(Row row, TypedOutputColumn column, Type targetType)
    {
        var value = row[column.Index];
        if (value == null || targetType == typeof(object))
            return value;

        return value;
    }

    private static Func<TOut>? CreateDefaultFactory(ConstructorInfo? constructor)
    {
        if (constructor != null)
            return null;

        NewExpression create;
        if (typeof(TOut).IsValueType)
        {
            create = Expression.New(typeof(TOut));
        }
        else
        {
            var defaultConstructor = typeof(TOut).GetConstructor(Type.EmptyTypes) ??
                                     throw new InvalidOperationException(
                                         $"Could not create typed output '{typeof(TOut).FullName}'.");
            create = Expression.New(defaultConstructor);
        }

        return Expression.Lambda<Func<TOut>>(create).Compile();
    }

    private static Action<TOut, object?> CreateSetter(MemberInfo member)
    {
        return member switch
        {
            PropertyInfo property => (target, value) => property.SetValue(target, value),
            FieldInfo field => (target, value) => field.SetValue(target, value),
            _ => throw new InvalidOperationException($"Typed output member '{member.Name}' is not supported.")
        };
    }

    private sealed record MemberBinding(
        string Name,
        Type TargetType,
        TypedOutputColumn Column,
        Action<TOut, object?> Set);
}
