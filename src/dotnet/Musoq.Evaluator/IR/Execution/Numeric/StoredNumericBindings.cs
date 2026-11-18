using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Schema;
using Musoq.Targets.Abstractions;

namespace Musoq.Evaluator.IR.Execution;

internal abstract record NumericPredicate;

internal sealed record NumericJunction(BinaryOpKind Kind, NumericPredicate Left, NumericPredicate Right)
    : NumericPredicate;

internal sealed record NumericNullTest(NumericColumn Column, bool IsNegated) : NumericPredicate;

internal sealed record NumericComparison(
    BinaryOpKind Kind,
    NumericOperand Left,
    NumericOperand Right,
    ExecutionTypeRef ElementType
) : NumericPredicate;

internal abstract record NumericOperand;

internal sealed record NumericColumn(int Index, ExecutionTypeRef Type) : NumericOperand;

internal sealed record NumericConstant(ExecutionLiteral Literal) : NumericOperand;

internal sealed class StoredNumericBindings(GeneratedRowShape shape, string alias)
{
    private readonly Dictionary<string, NumericOperand> locals = new(StringComparer.Ordinal);

    internal bool Bind(ExecutionLet let) =>
        Operand(let.Value) is { } operand && locals.TryAdd(let.Variable.Name, operand);

    internal NumericOperand? Operand(ExecutionExpression expression)
    {
        if (expression is ExecutionVariableRead variable)
            return locals.GetValueOrDefault(variable.Variable.Name);
        if (
            expression is ExecutionLiteral
            {
                Value.Kind: ExecutionConstantKind.SignedInteger or ExecutionConstantKind.FloatingPoint
            } literal
        )
            return new NumericConstant(literal);
        if (
            expression
                is not ExecutionFieldRead
                {
                    Stability: ColumnStability.Stable,
                    AccessStrategy: GeneratedFieldAccess or GeneratedRowTypeAccess
                } read
            || read.Alias != alias
            || read.SourceReadType != null && read.SourceReadType != read.ReturnType
        )
            return null;
        var field = shape.Fields.FirstOrDefault(field =>
            field.Name == read.FieldName && field.Type.StableId == read.ReturnType.StableId
        );
        if (field == null || !Supported(field.Type))
            return null;
        return new NumericColumn(field.OutputIndex, field.Type);
    }

    internal NumericPredicate? Predicate(ExecutionExpression expression)
    {
        if (expression is ExecutionIsNullCheck check && Operand(check.Expression) is NumericColumn column)
            return new NumericNullTest(column, check.IsNegated);
        if (expression is not ExecutionBinary binary)
            return null;
        if (binary.Kind is BinaryOpKind.And or BinaryOpKind.Or)
            return Predicate(binary.Left) is { } l && Predicate(binary.Right) is { } r
                ? new NumericJunction(binary.Kind, l, r)
                : null;
        if (
            binary.Kind
                is not (
                    BinaryOpKind.GreaterThan
                    or BinaryOpKind.GreaterOrEqual
                    or BinaryOpKind.LessThan
                    or BinaryOpKind.LessOrEqual
                    or BinaryOpKind.Equal
                    or BinaryOpKind.NotEqual
                )
            || Operand(binary.Left) is not { } left
            || Operand(binary.Right) is not { } right
            || (left as NumericColumn ?? right as NumericColumn) is not { } basis
        )
            return null;
        var element = ElementType(basis.Type);
        bool Compatible(NumericOperand value) =>
            value switch
            {
                NumericColumn field => ElementType(field.Type) == element
                    && (
                        field.Type.Descriptor.Kind != ExecutionPortableTypeKind.Nullable || binary.UsesSqlNullSemantics
                    ),
                NumericConstant { Literal.Value.Kind: ExecutionConstantKind.FloatingPoint } floating => element.StableId
                    == "primitive:float64"
                    || element.StableId == "primitive:float32" && floating.Literal.Value.BitWidth == 32,
                NumericConstant constant => constant.Literal.Value.BitWidth <= 32
                    || element.StableId != "primitive:int32",
                _ => false,
            };
        return Compatible(left) && Compatible(right) ? new NumericComparison(binary.Kind, left, right, element) : null;
    }

    internal bool SafeProjection(ExecutionExpression expression) =>
        Operand(expression) != null
        || expression
            is ExecutionBinary { Kind: BinaryOpKind.Add or BinaryOpKind.Subtract or BinaryOpKind.Multiply } binary
            && Supported(binary.ReturnType)
            && SafeProjection(binary.Left)
            && SafeProjection(binary.Right);

    private static ExecutionTypeRef ElementType(ExecutionTypeRef type) =>
        type.Descriptor is { Kind: ExecutionPortableTypeKind.Nullable, Arguments: [var element] }
            ? new ExecutionTypeRef(element)
            : type;

    internal static bool Supported(ExecutionTypeRef type)
    {
        return ElementType(type).StableId
            is "primitive:int32"
                or "primitive:int64"
                or "primitive:float32"
                or "primitive:float64";
    }
}
