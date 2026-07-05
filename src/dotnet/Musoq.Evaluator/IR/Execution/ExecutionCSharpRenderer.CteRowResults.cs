using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private const string CteRowResultsFieldName = "_cteRowResults";
    private const string CteRowResultsTypeName = "CteRowResults";

    private static IReadOnlyDictionary<int, TypedStoredTableResult> CreateTypedStoredTableResults(ExecutionPlan plan)
    {
        return TypedStoredTableResultResolver.Resolve(plan);
    }

    private static IEnumerable<MemberDeclarationSyntax> CreateCteRowResultMembers(
        IReadOnlyDictionary<int, TypedStoredTableResult> typedResults)
    {
        if (typedResults.Count == 0)
            return [];

        var slots = typedResults.Values
            .OrderBy(static result => result.TableIndex)
            .ToArray();

        return
        [
            CreateCteRowResultsClass(slots)
        ];
    }

    private static ClassDeclarationSyntax CreateCteRowResultsClass(
        IReadOnlyList<TypedStoredTableResult> slots)
    {
        return SyntaxFactory.ClassDeclaration(CteRowResultsTypeName)
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.SealedKeyword)))
            .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(slots.Select(CreateCteRowResultSlotField)));
    }

    private static FieldDeclarationSyntax CreateCteRowResultSlotField(TypedStoredTableResult slot)
    {
        return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(CreateCteRowResultSlotTypeSyntax(slot.RowShape))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(CreateCteRowResultSlotFieldName(slot.TableIndex)))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
    }

    private static TypeSyntax CreateCteRowResultsTypeSyntax()
    {
        return SyntaxFactory.IdentifierName(CteRowResultsTypeName);
    }

    private static TypeSyntax CreateCteRowResultSlotTypeSyntax(GeneratedRowShape rowShape)
    {
        return CreateListTypeSyntax(rowShape.TypeName);
    }

    private static MemberAccessExpressionSyntax CreateCteRowResultSlotAccess(int tableIndex)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(CteRowResultsFieldName),
            SyntaxFactory.IdentifierName(CreateCteRowResultSlotFieldName(tableIndex)));
    }

    private bool TryGetTypedStoredTableResult(
        int tableIndex,
        ExecutionRenderContext context,
        out TypedStoredTableResult result)
    {
        return context.Session.TypedStoredTableResults.TryGetValue(tableIndex, out result!);
    }

    private bool TryGetTypedStoredTableResult(
        int tableIndex,
        GeneratedRowShape rowShape,
        ExecutionRenderContext context,
        out TypedStoredTableResult result)
    {
        return TryGetTypedStoredTableResult(tableIndex, context, out result) &&
               string.Equals(result.RowShape.TypeName, rowShape.TypeName, StringComparison.Ordinal);
    }

    private bool TryGetTypedRowBufferShape(
        string variableName,
        ExecutionRenderContext context,
        out GeneratedRowShape rowShape)
    {
        return context.Session.TypedRowBufferVariables.TryGetValue(variableName, out rowShape!);
    }

    private static string CreateCteRowResultSlotFieldName(int tableIndex)
    {
        return $"Slot{tableIndex.ToString(CultureInfo.InvariantCulture)}";
    }
}
