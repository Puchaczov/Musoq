using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;

namespace Musoq.Evaluator.Visitors;

internal static class CommonColumnTypeResolver
{
    public static Type Resolve(
        string columnName,
        IReadOnlyList<Node> expressions,
        TextSpan errorSpan,
        CommonColumnTypeDiagnosticKind diagnosticKind)
    {
        var hasNull = expressions.Any(expression => IsExplicitNullType(expression.ReturnType) || IsNullableValueType(expression.ReturnType));
        var nonNullTypes = expressions
            .Select(expression => expression.ReturnType)
            .Where(type => !IsExplicitNullType(type))
            .OfType<Type>()
            .Select(StripNullable)
            .Distinct()
            .ToArray();

        if (nonNullTypes.Length == 0)
            return typeof(object);

        var typeErrorSpan = FindTypeErrorSpan(expressions, nonNullTypes, errorSpan);

        Type columnType;
        if (nonNullTypes.Length == 1)
        {
            columnType = nonNullTypes[0];
        }
        else if (nonNullTypes.All(BinaryOperatorTypeRules.IsNumericType))
        {
            columnType = ResolveNumericColumnType(columnName, nonNullTypes, typeErrorSpan, diagnosticKind);
        }
        else
        {
            throw CreateTypeFailure(
                CreateIncompatibleTypesMessage(columnName, nonNullTypes, diagnosticKind),
                columnName,
                nonNullTypes,
                typeErrorSpan,
                diagnosticKind,
                "incompatible-types");
        }

        return hasNull && columnType.IsValueType
            ? MakeTypeNullable(columnType)
            : columnType;
    }

    public static bool IsExplicitNullType(Type? type)
    {
        return type is NullNode.NullType ||
               string.Equals(type?.FullName, typeof(NullNode.NullType).FullName, StringComparison.Ordinal);
    }

    private static Type ResolveNumericColumnType(
        string columnName,
        IReadOnlyList<Type> types,
        TextSpan errorSpan,
        CommonColumnTypeDiagnosticKind diagnosticKind)
    {
        if (types.Contains(typeof(decimal)))
        {
            if (types.Any(type => type == typeof(float) || type == typeof(double)))
                throw CreateTypeFailure(
                    CreateDecimalFloatingPointMessage(columnName, diagnosticKind),
                    columnName,
                    types,
                    errorSpan,
                    diagnosticKind,
                    "decimal-floating-point-mix");

            return typeof(decimal);
        }

        if (types.Contains(typeof(double)))
            return typeof(double);

        if (types.Contains(typeof(float)))
            return typeof(float);

        if (types.Contains(typeof(ulong)))
        {
            if (types.Any(type => type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long)))
                throw CreateTypeFailure(
                    CreateUlongSignedMessage(columnName, diagnosticKind),
                    columnName,
                    types,
                    errorSpan,
                    diagnosticKind,
                    "ulong-signed-mix");

            return typeof(ulong);
        }

        if (types.Contains(typeof(long)))
            return typeof(long);

        if (types.Contains(typeof(uint)))
            return typeof(uint);

        return typeof(int);
    }

    private static bool IsNullableValueType(Type? type)
    {
        return type != null && Nullable.GetUnderlyingType(type) != null;
    }

    private static ValuesSourceException CreateTypeFailure(
        string message,
        string columnName,
        IReadOnlyList<Type> types,
        TextSpan span,
        CommonColumnTypeDiagnosticKind diagnosticKind,
        string constraint)
    {
        return diagnosticKind == CommonColumnTypeDiagnosticKind.Values
            ? ValuesSourceDiagnostics.Error(
                message,
                span,
                ("constraint", constraint),
                ("field", columnName),
                ("actualTypes", string.Join(", ", types.Select(static type => type.Name))))
            : new ValuesSourceException(message, span);
    }

    private static TextSpan FindTypeErrorSpan(
        IReadOnlyList<Node> expressions,
        IReadOnlyList<Type> nonNullTypes,
        TextSpan fallback)
    {
        if (nonNullTypes.Count < 2)
            return fallback;

        var firstType = nonNullTypes[0];
        foreach (var expression in expressions)
        {
            var type = expression.ReturnType;
            if (type == null || IsExplicitNullType(type) || StripNullable(type) == firstType)
                continue;

            if (expression.HasSpan)
                return expression.Span;
        }

        return fallback;
    }

    private static string CreateIncompatibleTypesMessage(
        string columnName,
        IReadOnlyList<Type> types,
        CommonColumnTypeDiagnosticKind diagnosticKind)
    {
        var typeNames = string.Join(", ", types.Select(type => type.Name));
        return diagnosticKind switch
        {
            CommonColumnTypeDiagnosticKind.Values =>
                $"VALUES field '{columnName}' mixes incompatible types: {typeNames}. Use consistent literal types or explicit conversion functions.",
            CommonColumnTypeDiagnosticKind.Unpivot =>
                $"UNPIVOT value column '{columnName}' mixes incompatible types: {typeNames}. Use consistent expression types or explicit conversion functions.",
            _ => throw new ArgumentOutOfRangeException(nameof(diagnosticKind), diagnosticKind, null)
        };
    }

    private static string CreateDecimalFloatingPointMessage(
        string columnName,
        CommonColumnTypeDiagnosticKind diagnosticKind)
    {
        return diagnosticKind switch
        {
            CommonColumnTypeDiagnosticKind.Values =>
                $"VALUES field '{columnName}' cannot mix decimal with floating point values. Use decimal literals consistently or convert values explicitly.",
            CommonColumnTypeDiagnosticKind.Unpivot =>
                $"UNPIVOT value column '{columnName}' cannot mix decimal with floating point values. Use consistent numeric types or convert values explicitly.",
            _ => throw new ArgumentOutOfRangeException(nameof(diagnosticKind), diagnosticKind, null)
        };
    }

    private static string CreateUlongSignedMessage(
        string columnName,
        CommonColumnTypeDiagnosticKind diagnosticKind)
    {
        return diagnosticKind switch
        {
            CommonColumnTypeDiagnosticKind.Values =>
                $"VALUES field '{columnName}' cannot safely mix ulong with signed integer values. Use consistent unsigned suffixes or convert values explicitly.",
            CommonColumnTypeDiagnosticKind.Unpivot =>
                $"UNPIVOT value column '{columnName}' cannot safely mix ulong with signed integer values. Use consistent unsigned values or convert values explicitly.",
            _ => throw new ArgumentOutOfRangeException(nameof(diagnosticKind), diagnosticKind, null)
        };
    }
}
