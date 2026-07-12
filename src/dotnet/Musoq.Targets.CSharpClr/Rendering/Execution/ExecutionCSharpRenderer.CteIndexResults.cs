using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private const string CteIndexResultsFieldName = "_cteIndexResults";
    private const string CteIndexResultsTypeName = "CteIndexResults";

    private static IEnumerable<MemberDeclarationSyntax> CreateCteIndexResultMembers(ExecutionPlan plan)
    {
        var slots = CollectCteIndexSlots(plan.Body);
        if (slots.Count == 0)
            return [];

        return
        [
            CreateCteIndexResultsClass(slots)
        ];
    }

    private static IReadOnlyList<CteIndexSlotDescriptor> CollectCteIndexSlots(ExecutionBlock block)
    {
        var slots = new SortedDictionary<int, CteIndexSlotDescriptor>();

        foreach (var node in FlattenNodes(block))
        {
            CteIndexSlotDescriptor? descriptor = node switch
            {
                ExecutionStoreCteIndex store => new CteIndexSlotDescriptor(
                    store.IndexSlot,
                    store.Kind,
                    store.KeyType.RequireClrType(),
                    store.RowType.RequireOptionalClrType(),
                    store.GeneratedRowTypeName),
                ExecutionLoadCteIndex load => new CteIndexSlotDescriptor(
                    load.IndexSlot,
                    load.Kind,
                    load.KeyType.RequireClrType(),
                    load.RowType.RequireOptionalClrType(),
                    load.GeneratedRowTypeName),
                _ => null
            };

            if (descriptor == null)
                continue;

            if (slots.TryGetValue(descriptor.Slot, out var existing))
            {
                ValidateCteIndexSlotDescriptor(existing, descriptor);
                continue;
            }

            slots.Add(descriptor.Slot, descriptor);
        }

        return slots.Values.ToArray();
    }

    private static void ValidateCteIndexSlotDescriptor(
        CteIndexSlotDescriptor existing,
        CteIndexSlotDescriptor candidate)
    {
        if (existing.Kind == candidate.Kind &&
            existing.KeyType == candidate.KeyType &&
            existing.RowType == candidate.RowType &&
            string.Equals(existing.GeneratedRowTypeName, candidate.GeneratedRowTypeName, StringComparison.Ordinal))
        {
            return;
        }

        throw new InvalidOperationException(
            $"CTE sidecar index slot {existing.Slot.ToString(CultureInfo.InvariantCulture)} has inconsistent generated types.");
    }

    private static ClassDeclarationSyntax CreateCteIndexResultsClass(IReadOnlyList<CteIndexSlotDescriptor> slots)
    {
        return SyntaxFactory.ClassDeclaration(CteIndexResultsTypeName)
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.SealedKeyword)))
            .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(slots.Select(CreateCteIndexSlotField)));
    }

    private static FieldDeclarationSyntax CreateCteIndexSlotField(CteIndexSlotDescriptor slot)
    {
        return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(CreateCteIndexTypeSyntax(
                        slot.Kind,
                        slot.KeyType,
                        slot.RowType,
                        slot.GeneratedRowTypeName))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(CreateCteIndexSlotFieldName(slot.Slot)))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
    }

    private static TypeSyntax CreateCteIndexResultsTypeSyntax()
    {
        return SyntaxFactory.IdentifierName(CteIndexResultsTypeName);
    }

    private static MemberAccessExpressionSyntax CreateCteIndexSlotAccess(int slot)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(CteIndexResultsFieldName),
            SyntaxFactory.IdentifierName(CreateCteIndexSlotFieldName(slot)));
    }

    private static string CreateCteIndexSlotFieldName(int slot)
    {
        return $"Slot{slot.ToString(CultureInfo.InvariantCulture)}";
    }

    private sealed record CteIndexSlotDescriptor(
        int Slot,
        ExecutionCteSidecarIndexKind Kind,
        Type KeyType,
        Type? RowType,
        string? GeneratedRowTypeName);
}
