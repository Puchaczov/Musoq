using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private const string CteRowResultsFieldName = "_cteRowResults";
    private const string CteRowResultsTypeName = "CteRowResults";
    private static IReadOnlyDictionary<int, TypedStoredTableResult> CreateTypedStoredTableResults(ExecutionPlan plan)
    {
        return plan.StoredTableRepresentations
            .ToDictionary(
                static representation => representation.TableIndex,
                static representation => new TypedStoredTableResult(
                    representation.TableIndex,
                    representation.RowShape,
                    representation.Kind));
    }

    private static IReadOnlyDictionary<string, TypedStoredTableResult> CreateColumnarStoredTableResultsByTableName(
        ExecutionPlan plan,
        IReadOnlyDictionary<int, TypedStoredTableResult> typedResults)
    {
        return ExecutionIrAnalysis.FlattenNodes(plan.Body)
            .OfType<ExecutionStoreTable>()
            .Where(store => typedResults.TryGetValue(store.TableIndex, out var result) &&
                            result.Kind == ExecutionStoredTableRepresentationKind.GeneratedColumnarBuffer)
            .GroupBy(static store => store.Table.Name, StringComparer.Ordinal)
            .Where(static group => group.Select(static store => store.TableIndex).Distinct().Count() == 1)
            .ToDictionary(
                static group => group.Key,
                group => typedResults[group.First().TableIndex],
                StringComparer.Ordinal);
    }
    private static IEnumerable<MemberDeclarationSyntax> CreateCteRowResultMembers(
        IReadOnlyDictionary<int, TypedStoredTableResult> typedResults)
    {
        if (typedResults.Count == 0)
            return [];
        var slots = typedResults.Values
            .OrderBy(static result => result.TableIndex)
            .ToArray();

        var columnHelpers = slots.Any(static slot => slot.Kind == ExecutionStoredTableRepresentationKind.GeneratedColumnarBuffer)
            ? new MemberDeclarationSyntax[] { ColumnarStoredTableSyntaxFactory.CreateColumnClass() }
            : [];
        return
        [
            CreateCteRowResultsClass(slots),
            ..columnHelpers,
            ..slots
                .Where(static slot => slot.Kind == ExecutionStoredTableRepresentationKind.GeneratedColumnarBuffer)
                .Select(ColumnarStoredTableSyntaxFactory.CreateBufferClass)
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
                SyntaxFactory.VariableDeclaration(CreateCteRowResultSlotTypeSyntax(slot))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(CreateCteRowResultSlotFieldName(slot.TableIndex)))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
    }

    private static TypeSyntax CreateCteRowResultsTypeSyntax()
    {
        return SyntaxFactory.IdentifierName(CteRowResultsTypeName);
    }

    private static TypeSyntax CreateCteRowResultSlotTypeSyntax(TypedStoredTableResult result)
    {
        return result.Kind switch
        {
            ExecutionStoredTableRepresentationKind.GeneratedRowList => CreateListTypeSyntax(result.RowShape.TypeName),
            ExecutionStoredTableRepresentationKind.GeneratedColumnarBuffer =>
                SyntaxFactory.IdentifierName(ColumnarStoredTableSyntaxFactory.CreateBufferTypeName(result.TableIndex)),
            _ => throw new InvalidOperationException($"Unsupported stored-table representation {result.Kind}.")
        };
    }
    private static MemberAccessExpressionSyntax CreateCteRowResultSlotAccess(int tableIndex)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(CteRowResultsFieldName),
            SyntaxFactory.IdentifierName(CreateCteRowResultSlotFieldName(tableIndex)));
    }

    internal bool TryGetTypedStoredTableResult(
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

    private bool TryGetColumnarStoredTableResult(
        string tableName,
        ExecutionRenderContext context,
        out TypedStoredTableResult result)
    {
        return context.Session.ColumnarStoredTableResultsByTableName.TryGetValue(tableName, out result!);
    }

    private bool TryGetColumnarStoredTableResultForRowShape(
        string rowTypeName,
        ExecutionRenderContext context,
        out TypedStoredTableResult result)
    {
        result = context.Session.TypedStoredTableResults.Values.FirstOrDefault(candidate =>
            candidate.Kind == ExecutionStoredTableRepresentationKind.GeneratedColumnarBuffer &&
            string.Equals(candidate.RowShape.TypeName, rowTypeName, StringComparison.Ordinal))!;
        return result != null;
    }

    private static string CreateCteRowResultSlotFieldName(int tableIndex)
    {
        return $"Slot{tableIndex.ToString(CultureInfo.InvariantCulture)}";
    }
}
