using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private const int WideGeneratedRowColumnMapThreshold = 16;

    private MemberDeclarationSyntax RenderGeneratedRowClass(
        GeneratedRowShape shape,
        IReadOnlySet<GeneratedRowContextConstructor>? usedConstructors,
        ExecutionRenderContext context)
    {
        if (TryGetColumnarStoredTableResultForRowShape(shape.TypeName, context, out var columnarResult))
            return ColumnarStoredTableSyntaxFactory.CreateRowView(shape, columnarResult.TableIndex);

        shape = GeneratedRowCarrierClassifier.Apply(
            shape,
            ResolveGeneratedRowCarrierBoundary(shape, context),
            ResolveGeneratedRowContextCarrierKind(shape, usedConstructors, context),
            context.Session.GeneratedRowTypesRequiringRowBase.Contains(shape.TypeName));

        if (!shape.RequiresRowBase && shape.EmitAsValueType)
            return RenderRowCarrierStruct(shape.TypeName, shape.Fields, GetGeneratedFieldName);

        if (!shape.RequiresRowBase)
            return RenderGeneratedRowCarrierClass(shape, usedConstructors);

        var members = new List<MemberDeclarationSyntax>();
        members.AddRange(shape.Fields.Select(field => CreateRowCarrierProperty(
            field,
            GetGeneratedFieldName,
            includePrivateSetter: true)));
        members.AddRange(CreateGeneratedRowContextFields(usedConstructors, shape.Contexts));
        members.AddRange(CreateGeneratedRowConstructors(shape.TypeName, shape.Fields, usedConstructors, shape.Contexts));
        members.Add(CreateGeneratedRowCountProperty(shape.Fields.Count));
        members.Add(GeneratedRowColumnVisitation.Create(shape.Fields, GetGeneratedFieldName, CreateGeneratedFieldTypeSyntax));
        members.Add(GeneratedRowColumnVisitation.CreateRead(shape.Fields, GetGeneratedFieldName, CreateGeneratedFieldTypeSyntax));
        if (RequiresWideGeneratedRowColumnMap(shape.Fields))
        {
            members.AddRange(CreateWideGeneratedRowColumnMapMembers(shape.Fields));
            members.Add(CreateWideGeneratedRowHasColumnMethod());
        }
        else
        {
            members.Add(CreateGeneratedRowHasColumnMethod(shape.Fields));
        }

        members.Add(GeneratedRowColumnVisitation.CreateWrite(shape.Fields, GetGeneratedFieldName, CreateGeneratedFieldTypeSyntax));

        return SyntaxFactory.ClassDeclaration(shape.TypeName)
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.SealedKeyword))
            .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName(nameof(Row))))))
            .WithMembers(SyntaxFactory.List(members));
    }

    private static string GetGeneratedFieldName(FieldBinding field)
    {
        return GeneratedRowNamingPolicy.GetGeneratedFieldName(field);
    }

    private GeneratedRowCarrierBoundary ResolveGeneratedRowCarrierBoundary(
        GeneratedRowShape shape,
        ExecutionRenderContext context)
    {
        if (shape.IsQueryScopedRow)
            return GeneratedRowCarrierBoundary.Internal;

        if (context.Session.GeneratedRowTypesUsedAtPublicBoundary.Contains(shape.TypeName))
            return GeneratedRowCarrierBoundary.Public;

        if (shape.EmitAsValueType)
            return GeneratedRowCarrierBoundary.Internal;

        return context.Session.TypedStoredTableResults.Values.Any(result =>
            string.Equals(result.RowShape.TypeName, shape.TypeName, StringComparison.Ordinal))
                ? GeneratedRowCarrierBoundary.Internal
                : GeneratedRowCarrierBoundary.Public;
    }

    private GeneratedRowContextCarrierKind ResolveGeneratedRowContextCarrierKind(
        GeneratedRowShape shape,
        IReadOnlySet<GeneratedRowContextConstructor>? usedConstructors,
        ExecutionRenderContext context)
    {
        if (context.Session.GeneratedRowTypesUsedAsRowContexts.Contains(shape.TypeName))
            return GeneratedRowContextCarrierKind.RequiresRowContexts;

        var constructors = GetGeneratedRowConstructors(usedConstructors);
        return constructors.All(static constructor => constructor is
            GeneratedRowContextConstructor.NoContext or
            GeneratedRowContextConstructor.SingleContext or
            GeneratedRowContextConstructor.SingleContexts or
            GeneratedRowContextConstructor.TwoSingleContexts)
                ? GeneratedRowContextCarrierKind.DirectFields
                : GeneratedRowContextCarrierKind.RequiresRowContexts;
    }

    private static ClassDeclarationSyntax RenderGeneratedRowCarrierClass(
        GeneratedRowShape shape,
        IReadOnlySet<GeneratedRowContextConstructor>? usedConstructors)
    {
        var members = new List<MemberDeclarationSyntax>();
        members.AddRange(shape.Fields.Select(field => CreateRowCarrierProperty(field, GetGeneratedFieldName)));
        members.AddRange(CreateGeneratedRowContextFields(usedConstructors, shape.Contexts));
        members.AddRange(CreateGeneratedRowConstructors(shape.TypeName, shape.Fields, usedConstructors, shape.Contexts));

        return SyntaxFactory.ClassDeclaration(shape.TypeName)
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.SealedKeyword))
            .WithMembers(SyntaxFactory.List(members));
    }
}
