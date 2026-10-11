using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class StructuralLiteralSyntaxFactory
{
    internal static ExpressionSyntax RenderRecord(ExecutionStructuralRecord record,
        ExecutionRenderContext context, ExecutionCSharpRenderer renderer)
    {
        var name = record.GeneratedTypeName ?? throw new InvalidOperationException(
            "A structural result requires a compiler-selected native carrier.");
        var values = record.Fields.Select(field => renderer.RenderExpression(field.Value, context)).ToList();
        for (var word = 0; word < Math.Max(1, (record.Fields.Count + 63) / 64); word++)
        {
            ulong presence = 0;
            for (var index = word * 64; index < Math.Min(record.Fields.Count, (word + 1) * 64); index++)
                if (record.Fields[index].IsPresent)
                    presence |= 1UL << (index % 64);
            values.Add(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(presence)));
        }
        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(name))
            .WithArgumentList(ExecutionSyntaxFactory.CreateArgumentList(values.ToArray()));
    }
}
