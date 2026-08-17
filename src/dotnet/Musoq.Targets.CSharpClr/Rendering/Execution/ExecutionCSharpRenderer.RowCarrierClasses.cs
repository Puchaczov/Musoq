using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static ClassDeclarationSyntax RenderExpandoAdapterClass(ExpandoAdapterShape shape)
    {
        return RenderRowCarrierClass(shape.TypeName, shape.Fields, field => field.Name);
    }

    private static MemberDeclarationSyntax RenderGeneratedRecordClass(GeneratedRecordShape shape)
    {
        if (shape.EmitAsValueType)
            return RenderRowCarrierStruct(shape.TypeName, shape.Fields, GetGeneratedFieldName);

        return RenderRowCarrierClass(shape.TypeName, shape.Fields, GetGeneratedFieldName);
    }

    private static ClassDeclarationSyntax RenderRowCarrierClass(
        string typeName,
        IReadOnlyList<FieldBinding> fields,
        Func<FieldBinding, string> resolvePropertyName)
    {
        return SyntaxFactory.ClassDeclaration(typeName)
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.SealedKeyword))
            .WithMembers(SyntaxFactory.List(CreateRowCarrierMembers(typeName, fields, resolvePropertyName)));
    }

    private static StructDeclarationSyntax RenderRowCarrierStruct(
        string typeName,
        IReadOnlyList<FieldBinding> fields,
        Func<FieldBinding, string> resolvePropertyName)
    {
        return SyntaxFactory.StructDeclaration(typeName)
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
            .WithMembers(SyntaxFactory.List(CreateRowCarrierMembers(typeName, fields, resolvePropertyName)));
    }

    private static List<MemberDeclarationSyntax> CreateRowCarrierMembers(
        string typeName,
        IReadOnlyList<FieldBinding> fields,
        Func<FieldBinding, string> resolvePropertyName)
    {
        var members = new List<MemberDeclarationSyntax>
        {
            CreateRowCarrierConstructor(typeName, fields, resolvePropertyName)
        };
        members.AddRange(fields.Select(field => CreateRowCarrierProperty(field, resolvePropertyName)));
        return members;
    }

    private static ConstructorDeclarationSyntax CreateRowCarrierConstructor(
        string typeName,
        IReadOnlyList<FieldBinding> fields,
        Func<FieldBinding, string> resolvePropertyName)
    {
        var parameters = fields.Select(field => SyntaxFactory.Parameter(SyntaxFactory.Identifier(EscapeIdentifier(resolvePropertyName(field))))
            .WithType(CreateGeneratedFieldTypeSyntax(field)));
        var assignments = fields.Select(field =>
        {
            var propertyName = resolvePropertyName(field);
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ThisExpression(),
                        CreateIdentifierName(propertyName)),
                    CreateIdentifierName(propertyName)));
        });

        return SyntaxFactory.ConstructorDeclaration(typeName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)))
            .WithBody(StatementEmitter.CreateBlock(assignments));
    }

    private static PropertyDeclarationSyntax CreateRowCarrierProperty(
        FieldBinding field,
        Func<FieldBinding, string> resolvePropertyName,
        bool includePrivateSetter = false)
    {
        var accessors = new List<AccessorDeclarationSyntax>
        {
            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
        };

        if (includePrivateSetter)
        {
            accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        }

        return SyntaxFactory.PropertyDeclaration(CreateGeneratedFieldTypeSyntax(field), EscapeIdentifier(resolvePropertyName(field)))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)));
    }
}
