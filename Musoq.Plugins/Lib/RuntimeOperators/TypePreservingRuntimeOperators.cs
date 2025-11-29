using System;
using Musoq.Plugins.Lib.TypeConversion;

namespace Musoq.Plugins.Lib.RuntimeOperators;

/// <summary>
/// Runtime operators that preserve operand types using smart type promotion rules.
/// Priority: decimal > double > long.
/// </summary>
internal class TypePreservingRuntimeOperators : IRuntimeOperators
{
    private readonly NumericOnlyTypeConverter _numericConverter;
    private readonly ComparisonTypeConverter _comparisonConverter;
    private readonly StrictTypeConverter _strictConverter;

    /// <summary>
    /// Initializes a new instance of the TypePreservingRuntimeOperators class.
    /// </summary>
    /// <param name="numericConverter">Converter for arithmetic operations (numeric-only mode).</param>
    /// <param name="comparisonConverter">Converter for comparison operations (lossy mode).</param>
    /// <param name="strictConverter">Converter for equality operations (strict mode).</param>
    public TypePreservingRuntimeOperators(
        NumericOnlyTypeConverter numericConverter,
        ComparisonTypeConverter comparisonConverter,
        StrictTypeConverter strictConverter)
    {
        _numericConverter = numericConverter ?? throw new ArgumentNullException(nameof(numericConverter));
        _comparisonConverter = comparisonConverter ?? throw new ArgumentNullException(nameof(comparisonConverter));
        _strictConverter = strictConverter ?? throw new ArgumentNullException(nameof(strictConverter));
    }

    /// <inheritdoc />
    public object? Add(object? left, object? right)
    {
        return ApplyArithmeticOperator(left, right, (l, r) => l + r, (l, r) => l + r, (l, r) => l + r);
    }

    /// <inheritdoc />
    public object? Subtract(object? left, object? right)
    {
        return ApplyArithmeticOperator(left, right, (l, r) => l - r, (l, r) => l - r, (l, r) => l - r);
    }

    /// <inheritdoc />
    public object? Multiply(object? left, object? right)
    {
        return ApplyArithmeticOperator(left, right, (l, r) => l * r, (l, r) => l * r, (l, r) => l * r);
    }

    /// <inheritdoc />
    public object? Divide(object? left, object? right)
    {
        return ApplyArithmeticOperator(left, right, (l, r) => l / r, (l, r) => l / r, (l, r) => l / r);
    }

    /// <inheritdoc />
    public object? Modulo(object? left, object? right)
    {
        return ApplyArithmeticOperator(left, right, (l, r) => l % r, (l, r) => l % r, (l, r) => l % r);
    }

    /// <inheritdoc />
    public bool? GreaterThan(object? left, object? right)
    {
        var leftDecimal = _comparisonConverter.TryConvertToDecimal(left);
        var rightDecimal = _comparisonConverter.TryConvertToDecimal(right);
        
        if (!leftDecimal.HasValue || !rightDecimal.HasValue)
            return null;
            
        return leftDecimal.Value > rightDecimal.Value;
    }

    /// <inheritdoc />
    public bool? LessThan(object? left, object? right)
    {
        var leftDecimal = _comparisonConverter.TryConvertToDecimal(left);
        var rightDecimal = _comparisonConverter.TryConvertToDecimal(right);
        
        if (!leftDecimal.HasValue || !rightDecimal.HasValue)
            return null;
            
        return leftDecimal.Value < rightDecimal.Value;
    }

    /// <inheritdoc />
    public bool? GreaterThanOrEqual(object? left, object? right)
    {
        var leftDecimal = _comparisonConverter.TryConvertToDecimal(left);
        var rightDecimal = _comparisonConverter.TryConvertToDecimal(right);
        
        if (!leftDecimal.HasValue || !rightDecimal.HasValue)
            return null;
            
        return leftDecimal.Value >= rightDecimal.Value;
    }

    /// <inheritdoc />
    public bool? LessThanOrEqual(object? left, object? right)
    {
        var leftDecimal = _comparisonConverter.TryConvertToDecimal(left);
        var rightDecimal = _comparisonConverter.TryConvertToDecimal(right);
        
        if (!leftDecimal.HasValue || !rightDecimal.HasValue)
            return null;
            
        return leftDecimal.Value <= rightDecimal.Value;
    }

    /// <inheritdoc />
    public bool? Equal(object? left, object? right)
    {
        var leftDecimal = _strictConverter.TryConvertToDecimal(left);
        var rightDecimal = _strictConverter.TryConvertToDecimal(right);
        
        if (leftDecimal.HasValue && rightDecimal.HasValue)
            return leftDecimal.Value == rightDecimal.Value;
        
        if (left is string leftStr && right is string rightStr)
            return leftStr == rightStr;
        
        return false;
    }

    /// <inheritdoc />
    public bool? NotEqual(object? left, object? right)
    {
        var leftDecimal = _strictConverter.TryConvertToDecimal(left);
        var rightDecimal = _strictConverter.TryConvertToDecimal(right);
        
        if (leftDecimal.HasValue && rightDecimal.HasValue)
            return leftDecimal.Value != rightDecimal.Value;
        
        if (left is string leftStr && right is string rightStr)
            return leftStr != rightStr;
        
        return true;
    }

    private object? ApplyArithmeticOperator(object? left, object? right, 
        Func<long, long, long> longOp, 
        Func<double, double, double> doubleOp,
        Func<decimal, decimal, decimal> decimalOp)
    {
        if (left == null || right == null)
            return null;

        if (left is string || right is string)
            return null;

        var targetType = DetermineArithmeticTargetType(left, right);

        return targetType switch
        {
            ArithmeticType.Long => ConvertAndApply(left, right, longOp, _numericConverter.TryConvertToInt64),
            ArithmeticType.Double => ConvertAndApply(left, right, doubleOp, _numericConverter.TryConvertToDouble),
            ArithmeticType.Decimal => ConvertAndApply(left, right, decimalOp, _numericConverter.TryConvertToDecimal),
            _ => null
        };
    }

    private enum ArithmeticType
    {
        Long,
        Double,
        Decimal
    }

    private ArithmeticType DetermineArithmeticTargetType(object left, object right)
    {
        if (left is decimal || right is decimal)
            return ArithmeticType.Decimal;
        
        if (left is double || right is double || left is float || right is float)
            return ArithmeticType.Double;
        
        return ArithmeticType.Long;
    }

    private T? ConvertAndApply<T>(object? left, object? right, Func<T, T, T> operation, Func<object?, T?> converter) where T : struct
    {
        var leftConverted = converter(left);
        var rightConverted = converter(right);

        if (!leftConverted.HasValue || !rightConverted.HasValue)
            return null;

        return operation(leftConverted.Value, rightConverted.Value);
    }
}
