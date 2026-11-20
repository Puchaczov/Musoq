using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Targets.CSharpClr;

internal static class StoredNumericMaskSyntax
{
    // Each call is confined to one populated 64-row segment of a 1024-row storage chunk.
    internal static MemberDeclarationSyntax Create(NumericPredicate predicate, string name)
    {
        var body = new StringBuilder();
        var columns = new HashSet<int>();
        var comparisons = 0;
        var expression = Emit(predicate);
        return SyntaxFactory.ParseMemberDeclaration(
            $"public ulong {name}(int start, int length) {{ ulong active = ulong.MaxValue >> (64 - length); {body} return {expression}; }}"
        )!;

        void Bind(NumericColumn column)
        {
            if (!columns.Add(column.Index))
                return;
            var name = ColumnarStoredTableSyntaxFactory.CreateColumnName(column.Index);
            var nullable = Nullable.GetUnderlyingType(column.Type.RequireClrType());
            if (nullable == null)
            {
                body.AppendLine(CultureInfo.InvariantCulture, $"var c{column.Index} = {name}.GetSpan(start, length);");
                return;
            }
            var type = CreateTypeSyntax(nullable);
            body.AppendLine(
                CultureInfo.InvariantCulture,
                $$"""
                var source{{column.Index}} = {{name}}.GetSpan(start, length);
                Span<{{type}}> c{{column.Index}} = stackalloc {{type}}[length];
                ulong valid{{column.Index}} = 0;
                for (int i = 0; i < length; ++i) {
                    var value = source{{column.Index}}[i];
                    c{{column.Index}}[i] = value.GetValueOrDefault();
                    if (value.HasValue) valid{{column.Index}} |= 1UL << i;
                }
                """
            );
        }

        string Valid(NumericOperand value) =>
            value is NumericColumn field && Nullable.GetUnderlyingType(field.Type.RequireClrType()) != null
                ? FormattableString.Invariant($"valid{field.Index}")
                : "active";

        string Emit(NumericPredicate predicate)
        {
            if (predicate is NumericJunction junction)
                return $"({Emit(junction.Left)} {(junction.Kind == BinaryOpKind.And ? "&" : "|")} {Emit(junction.Right)})";
            if (predicate is NumericNullTest test)
            {
                Bind(test.Column);
                return test.IsNegated ? Valid(test.Column) : $"(active & ~{Valid(test.Column)})";
            }
            var comparison = (NumericComparison)predicate;
            var type = CreateTypeSyntax(comparison.ElementType.RequireClrType()).ToString();
            const string vector = "System.Runtime.Intrinsics.Vector256";
            string Load(NumericOperand value, bool vectorized)
            {
                if (value is NumericColumn column)
                {
                    Bind(column);
                    var span = FormattableString.Invariant($"c{column.Index}");
                    return vectorized ? $"{vector}.Create<{type}>({span}.Slice(i, width))" : $"{span}[i]";
                }
                var literal = ExecutionCSharpRenderer.RenderLiteral(((NumericConstant)value).Literal.Value);
                return vectorized ? $"{vector}.Create(({type})({literal}))" : $"({type})({literal})";
            }
            var (operation, symbol) = comparison.Kind switch
            {
                BinaryOpKind.Equal => ("Equals", "=="),
                BinaryOpKind.NotEqual => ("Equals", "!="),
                BinaryOpKind.LessThan => ("LessThan", "<"),
                BinaryOpKind.LessOrEqual => ("LessThanOrEqual", "<="),
                BinaryOpKind.GreaterThan => ("GreaterThan", ">"),
                BinaryOpKind.GreaterOrEqual => ("GreaterThanOrEqual", ">="),
                _ => throw new InvalidOperationException(),
            };
            var bits = $"{vector}.{operation}({Load(comparison.Left, true)}, {Load(comparison.Right, true)})";
            if (comparison.Kind == BinaryOpKind.NotEqual)
                bits = $"{vector}.OnesComplement({bits})";
            var mask = FormattableString.Invariant($"mask{comparisons++}");
            body.AppendLine(
                CultureInfo.InvariantCulture,
                $$"""
                ulong {{mask}} = 0;
                {
                    int i = 0;
                    int width = {{vector}}<{{type}}>.Count;
                    if ({{vector}}.IsHardwareAccelerated)
                        for (; i <= length - width; i += width)
                            {{mask}} |= (ulong){{vector}}.ExtractMostSignificantBits({{bits}}) << i;
                    for (; i < length; ++i)
                        if ({{Load(comparison.Left, false)}} {{symbol}} {{Load(comparison.Right, false)}})
                            {{mask}} |= 1UL << i;
                }
                """
            );
            return $"({mask} & {Valid(comparison.Left)} & {Valid(comparison.Right)})";
        }
    }
}
