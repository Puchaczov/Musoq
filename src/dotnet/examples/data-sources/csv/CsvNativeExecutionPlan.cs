using System.Globalization;
using System.Numerics;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Csv;

/// <summary>Immutable operations bound to authoritative column types during source planning.</summary>
internal sealed class CsvNativeExecutionPlan(CsvNativePredicate? predicate, CsvNativeOrdering[] ordering)
{
    internal const string PropertyName = "csv.native.execution";
    internal CsvNativePredicate? Predicate { get; } = predicate;
    internal IReadOnlyList<CsvNativeOrdering> Ordering { get; } = Array.AsReadOnly((CsvNativeOrdering[])ordering.Clone());

    internal static CsvNativeExecutionPlan Bind(SourcePlanResult result)
    {
        var columns = result.AcceptedColumns.ToDictionary(column => column.Name, StringComparer.OrdinalIgnoreCase);
        var predicate = result.AcceptedPredicate is null ? null : BindPredicate(result.AcceptedPredicate, columns);
        var ordering = result.AcceptedOrderBy.Select(order => new CsvNativeOrdering(
            BindColumn(columns[order.Column.Name]), order.Direction == OrderDirection.Descending)).ToArray();
        return new CsvNativeExecutionPlan(predicate, ordering);
    }

    private static CsvNativePredicate BindPredicate(SourcePredicateExpression expression,
        IReadOnlyDictionary<string, SourceColumnRef> columns)
        => expression switch
        {
            SourcePredicateComparison comparison => BindComparison(comparison, columns),
            SourcePredicateNullCheck check => BindValue(check.Expression, columns).CreateNullCheck(check.IsNegated),
            SourcePredicateIn membership => BindValue(membership.Expression, columns).CreateMembership(
                membership.Values.Select(value => BindValue(value, columns)).ToArray(), membership.IsNegated),
            SourcePredicateLogical logical => new CsvLogicalPredicate(BindPredicate(logical.Left, columns),
                BindPredicate(logical.Right, columns), logical.Operator),
            _ => throw new InvalidOperationException("CSV planning cannot bind the accepted predicate.")
        };

    private static CsvNativePredicate BindComparison(SourcePredicateComparison comparison,
        IReadOnlyDictionary<string, SourceColumnRef> columns)
    {
        var left = BindValue(comparison.Left, columns);
        var right = BindValue(comparison.Right, columns);
        return left.CreateComparison(right, comparison.Operator);
    }

    private static CsvNativeValue BindValue(SourcePredicateExpression expression,
        IReadOnlyDictionary<string, SourceColumnRef> columns)
        => expression switch
        {
            SourcePredicateColumn column => BindColumn(columns[column.Column.Name]),
            SourcePredicateLiteral literal => BindLiteral(literal.Value),
            _ => throw new InvalidOperationException("CSV planning cannot bind the accepted value expression.")
        };

    private static CsvNativeValue BindColumn(SourceColumnRef column)
    {
        var declared = Type.GetType(column.ValueTypeName ?? string.Empty, throwOnError: false)
            ?? throw new InvalidOperationException($"CSV column '{column.Name}' requires an authoritative concrete type.");
        var underlying = Nullable.GetUnderlyingType(declared);
        var type = underlying ?? declared;
        var nullable = underlying is not null;
        if (type == typeof(string)) return new CsvStringColumn(column.Name);
        if (type == typeof(byte)) return NumericColumn<byte>(column.Name, nullable);
        if (type == typeof(sbyte)) return NumericColumn<sbyte>(column.Name, nullable);
        if (type == typeof(short)) return NumericColumn<short>(column.Name, nullable);
        if (type == typeof(ushort)) return NumericColumn<ushort>(column.Name, nullable);
        if (type == typeof(int)) return NumericColumn<int>(column.Name, nullable);
        if (type == typeof(uint)) return NumericColumn<uint>(column.Name, nullable);
        if (type == typeof(long)) return NumericColumn<long>(column.Name, nullable);
        if (type == typeof(ulong)) return NumericColumn<ulong>(column.Name, nullable);
        if (type == typeof(float)) return NumericColumn<float>(column.Name, nullable);
        if (type == typeof(double)) return NumericColumn<double>(column.Name, nullable);
        if (type == typeof(decimal)) return NumericColumn<decimal>(column.Name, nullable);
        if (type == typeof(bool)) return ScalarColumn<bool>(column.Name, nullable);
        if (type == typeof(char)) return ScalarColumn<char>(column.Name, nullable);
        if (type == typeof(DateTime)) return ScalarColumn<DateTime>(column.Name, nullable);
        if (type == typeof(DateTimeOffset)) return ScalarColumn<DateTimeOffset>(column.Name, nullable);
        if (type == typeof(TimeSpan)) return ScalarColumn<TimeSpan>(column.Name, nullable);
        if (type == typeof(Guid)) return ScalarColumn<Guid>(column.Name, nullable);
        throw new InvalidOperationException($"CSV column '{column.Name}' has no native decoder for '{declared}'.");
    }

    private static CsvNativeValue NumericColumn<T>(string name, bool nullable) where T : struct, INumber<T>
    {
        if (nullable) return new CsvNullableNumericColumn<T>(name);
        return new CsvNumericColumn<T>(name);
    }

    private static CsvNativeValue ScalarColumn<T>(string name, bool nullable) where T : struct
    {
        if (nullable) return new CsvNullableScalarColumn<T>(name);
        return new CsvScalarColumn<T>(name);
    }

    private static CsvNativeValue BindLiteral(object? value)
        => value switch
        {
            null => new CsvNullLiteral(),
            string text => new CsvLiteral<string?>(text, StringComparer.Ordinal),
            bool flag => new CsvLiteral<bool?>(flag),
            char character => new CsvLiteral<char?>(character),
            DateTime date => new CsvLiteral<DateTime?>(date),
            DateTimeOffset date => new CsvLiteral<DateTimeOffset?>(date),
            TimeSpan duration => new CsvLiteral<TimeSpan?>(duration),
            Guid identifier => new CsvLiteral<Guid?>(identifier),
            byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal =>
                new CsvLiteral<decimal?>(Convert.ToDecimal(value, CultureInfo.InvariantCulture)),
            _ => throw new InvalidOperationException("CSV planning cannot bind the literal's concrete type.")
        };
}

internal abstract class CsvNativePredicate
{
    internal abstract bool Matches(ref CsvFieldReader reader);
}

internal abstract class CsvNativeValue
{
    internal abstract CsvNativePredicate CreateComparison(CsvNativeValue right, SourcePredicateComparisonOperator operation);
    internal abstract CsvNativePredicate CreateMembership(CsvNativeValue[] members, bool negated);
    internal abstract CsvNativePredicate CreateNullCheck(bool negated);
    internal abstract int CompareRows(ref CsvFieldReader left, ref CsvFieldReader right);
}

internal abstract class CsvNativeValue<T>(IComparer<T>? comparer = null) : CsvNativeValue
{
    private readonly IComparer<T> _comparer = comparer ?? Comparer<T>.Default;
    internal abstract T Read(ref CsvFieldReader reader);
    internal override CsvNativePredicate CreateComparison(CsvNativeValue right, SourcePredicateComparisonOperator operation)
        => new CsvComparisonPredicate<T>(this, RequireTyped(right), operation, _comparer);
    internal override CsvNativePredicate CreateMembership(CsvNativeValue[] members, bool negated)
        => new CsvMembershipPredicate<T>(this, members.Select(RequireTyped).ToArray(), negated);
    internal override CsvNativePredicate CreateNullCheck(bool negated) => new CsvNullPredicate<T>(this, negated);
    internal override int CompareRows(ref CsvFieldReader left, ref CsvFieldReader right)
        => _comparer.Compare(Read(ref left), Read(ref right));
    private static CsvNativeValue<T> RequireTyped(CsvNativeValue value)
    {
        if (value is CsvNullLiteral) return new CsvLiteral<T>(default!);
        if (value is CsvNativeValue<T> typed) return typed;
        throw new InvalidOperationException("CSV planning found incompatible concrete comparison types.");
    }
}

internal sealed class CsvStringColumn(string name) : CsvNativeValue<string?>(StringComparer.Ordinal)
{
    internal override string? Read(ref CsvFieldReader reader) => reader.Read<string?>(name);
}
internal sealed class CsvNumericColumn<T>(string name) : CsvNativeValue<decimal?> where T : struct, INumber<T>
{
    internal override decimal? Read(ref CsvFieldReader reader) => decimal.CreateChecked(reader.Read<T>(name));
}
internal sealed class CsvNullableNumericColumn<T>(string name) : CsvNativeValue<decimal?> where T : struct, INumber<T>
{
    internal override decimal? Read(ref CsvFieldReader reader)
    {
        var value = reader.Read<T?>(name);
        return value.HasValue ? decimal.CreateChecked(value.Value) : null;
    }
}
internal sealed class CsvScalarColumn<T>(string name) : CsvNativeValue<T?> where T : struct
{
    internal override T? Read(ref CsvFieldReader reader) => reader.Read<T>(name);
}
internal sealed class CsvNullableScalarColumn<T>(string name) : CsvNativeValue<T?> where T : struct
{
    internal override T? Read(ref CsvFieldReader reader) => reader.Read<T?>(name);
}
internal sealed class CsvLiteral<T>(T value, IComparer<T>? comparer = null) : CsvNativeValue<T>(comparer)
{
    internal override T Read(ref CsvFieldReader reader) => value;
}
internal sealed class CsvNullLiteral : CsvNativeValue
{
    internal override CsvNativePredicate CreateComparison(CsvNativeValue right, SourcePredicateComparisonOperator operation)
    {
        if (right is CsvNullLiteral)
            return new CsvConstantPredicate(operation == SourcePredicateComparisonOperator.Equal);
        return right.CreateComparison(this, Reverse(operation));
    }
    internal override CsvNativePredicate CreateMembership(CsvNativeValue[] members, bool negated)
        => new CsvConstantPredicate(members.Any(value => value is CsvNullLiteral) != negated);
    internal override CsvNativePredicate CreateNullCheck(bool negated) => new CsvConstantPredicate(!negated);
    internal override int CompareRows(ref CsvFieldReader left, ref CsvFieldReader right) => 0;
    private static SourcePredicateComparisonOperator Reverse(SourcePredicateComparisonOperator operation) => operation switch
    {
        SourcePredicateComparisonOperator.GreaterThan => SourcePredicateComparisonOperator.LessThan,
        SourcePredicateComparisonOperator.GreaterOrEqual => SourcePredicateComparisonOperator.LessOrEqual,
        SourcePredicateComparisonOperator.LessThan => SourcePredicateComparisonOperator.GreaterThan,
        SourcePredicateComparisonOperator.LessOrEqual => SourcePredicateComparisonOperator.GreaterOrEqual,
        _ => operation
    };
}
internal sealed class CsvComparisonPredicate<T>(CsvNativeValue<T> left, CsvNativeValue<T> right,
    SourcePredicateComparisonOperator operation, IComparer<T> comparer) : CsvNativePredicate
{
    internal override bool Matches(ref CsvFieldReader reader)
    {
        var first = left.Read(ref reader);
        var second = right.Read(ref reader);
        if (operation == SourcePredicateComparisonOperator.Equal) return EqualityComparer<T>.Default.Equals(first, second);
        if (operation == SourcePredicateComparisonOperator.NotEqual) return !EqualityComparer<T>.Default.Equals(first, second);
        if (first is null || second is null) return false;
        var comparison = comparer.Compare(first, second);
        return operation switch
        {
            SourcePredicateComparisonOperator.GreaterThan => comparison > 0,
            SourcePredicateComparisonOperator.GreaterOrEqual => comparison >= 0,
            SourcePredicateComparisonOperator.LessThan => comparison < 0,
            SourcePredicateComparisonOperator.LessOrEqual => comparison <= 0,
            _ => throw new InvalidOperationException("CSV has an unsupported comparison operator.")
        };
    }
}
internal sealed class CsvMembershipPredicate<T>(CsvNativeValue<T> value, CsvNativeValue<T>[] members, bool negated) : CsvNativePredicate
{
    internal override bool Matches(ref CsvFieldReader reader)
    {
        var current = value.Read(ref reader);
        foreach (var member in members)
            if (EqualityComparer<T>.Default.Equals(current, member.Read(ref reader))) return !negated;
        return negated;
    }
}
internal sealed class CsvNullPredicate<T>(CsvNativeValue<T> value, bool negated) : CsvNativePredicate
{
    internal override bool Matches(ref CsvFieldReader reader) => (value.Read(ref reader) is null) != negated;
}
internal sealed class CsvLogicalPredicate(CsvNativePredicate left, CsvNativePredicate right,
    SourcePredicateLogicalOperator operation) : CsvNativePredicate
{
    internal override bool Matches(ref CsvFieldReader reader)
    {
        if (operation == SourcePredicateLogicalOperator.And) return left.Matches(ref reader) && right.Matches(ref reader);
        return left.Matches(ref reader) || right.Matches(ref reader);
    }
}
internal sealed class CsvConstantPredicate(bool value) : CsvNativePredicate
{
    internal override bool Matches(ref CsvFieldReader reader) => value;
}
internal sealed class CsvNativeOrdering(CsvNativeValue value, bool descending)
{
    internal int Compare(ref CsvFieldReader left, ref CsvFieldReader right)
    {
        if (descending) return value.CompareRows(ref right, ref left);
        return value.CompareRows(ref left, ref right);
    }
}
