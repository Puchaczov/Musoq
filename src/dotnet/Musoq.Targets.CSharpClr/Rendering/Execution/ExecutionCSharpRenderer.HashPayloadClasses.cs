using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static StructDeclarationSyntax RenderHashPayloadStruct(HashPayloadShape shape)
    {
        var members = new List<MemberDeclarationSyntax>();
        members.AddRange(GetHashPayloadFields(shape).Select(CreateHashPayloadField));
        members.Add(CreateHashPayloadConstructor(shape));

        return SyntaxFactory.StructDeclaration(shape.TypeName)
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
            .WithMembers(SyntaxFactory.List(members));
    }

    private static FieldDeclarationSyntax CreateHashPayloadField(FieldBinding field)
    {
        return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(CreateTypeSyntax(field.Type))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(EscapeIdentifier(GetGeneratedFieldName(field))))))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
    }

    private static ConstructorDeclarationSyntax CreateHashPayloadConstructor(HashPayloadShape shape)
    {
        var fields = GetHashPayloadFields(shape);
        var parameters = fields.Select(field =>
        {
            var fieldName = GetGeneratedFieldName(field);
            return SyntaxFactory.Parameter(SyntaxFactory.Identifier(EscapeIdentifier(fieldName)))
                .WithType(CreateTypeSyntax(field.Type));
        });
        var assignments = fields.Select(field =>
        {
            var fieldName = GetGeneratedFieldName(field);
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ThisExpression(),
                        CreateIdentifierName(fieldName)),
                    CreateIdentifierName(fieldName)));
        });

        return SyntaxFactory.ConstructorDeclaration(shape.TypeName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)))
            .WithBody(StatementEmitter.CreateBlock(assignments));
    }

    private static FieldBinding[] GetHashPayloadFields(HashPayloadShape shape)
    {
        return shape.Fields.Concat(shape.Contexts).ToArray();
    }
}
