using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class StoredAggregateReductionSyntax
{
    internal static string MethodName(int index) => "Reduce" + index.ToString(CultureInfo.InvariantCulture);

    internal static IEnumerable<MemberDeclarationSyntax> CreateMembers(StoredNumericAggregates aggregates)
    {
        for (int index = 0; index < aggregates.Reductions.Count; index++)
        {
            var reduction = aggregates.Reductions[index];
            if (reduction.Filter is { } filter)
                yield return StoredNumericMaskSyntax.Create(filter, MethodName(index));
            else if (reduction.Input is { } column)
                yield return CreateExtremum(MethodName(index), column, reduction.Kind);
        }
    }

    private static MemberDeclarationSyntax CreateExtremum(string name, NumericColumn column, AggregateInlineKind kind)
    {
        var type = CreateTypeSyntax(column.Type.RequireClrType());
        var field = ColumnarStoredTableSyntaxFactory.CreateColumnName(column.Index);
        var op = kind == AggregateInlineKind.Min ? "Min" : "Max";
        const string vector = "System.Runtime.Intrinsics.Vector256";
        // Calls have at least one populated element. Seed with an input to preserve both integer extremes.
        return SyntaxFactory.ParseMemberDeclaration(
            $$"""
            public {{type}} {{name}}(int start, int length)
            {
                var values = {{field}}.GetSpan(start, length);
                var result = values[0];
                int i = 0;
                int width = {{vector}}<{{type}}>.Count;
                if ({{vector}}.IsHardwareAccelerated && length >= width)
                {
                    var lanes = {{vector}}.Create<{{type}}>(values.Slice(0, width));
                    for (i = width; i <= length - width; i += width)
                        lanes = {{vector}}.{{op}}(lanes, {{vector}}.Create<{{type}}>(values.Slice(i, width)));
                    for (int lane = 0; lane < width; lane++)
                        result = Math.{{op}}(result, {{vector}}.GetElement(lanes, lane));
                }
                for (; i < length; i++)
                    result = Math.{{op}}(result, values[i]);
                return result;
            }
            """
        )!;
    }
}
