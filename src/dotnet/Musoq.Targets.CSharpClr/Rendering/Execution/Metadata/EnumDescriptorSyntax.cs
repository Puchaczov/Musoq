using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Schema;

namespace Musoq.Targets.CSharpClr;

internal static class EnumDescriptorSyntax
{
    private const string SchemaNamespace = "global::Musoq.Schema";

    public static ExpressionSyntax Create(EnumTypeDescriptor? descriptor)
    {
        if (descriptor == null)
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.ParseTypeName($"{SchemaNamespace}.{nameof(EnumTypeDescriptor)}"))
            .WithArgumentList(ExecutionSyntaxFactory.CreateArgumentList(
                ExecutionSyntaxFactory.CreateStringLiteral(descriptor.DisplayName),
                CreateEnumMemberAccess(nameof(EnumTypeOrigin), descriptor.Origin.ToString()),
                CreateEnumMemberAccess(nameof(EnumUnderlyingKind), descriptor.UnderlyingKind.ToString()),
                SyntaxFactory.LiteralExpression(
                    descriptor.IsFlags ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression),
                ExecutionSyntaxFactory.CreateArrayCreation(
                    $"{SchemaNamespace}.{nameof(EnumMemberDescriptor)}",
                    descriptor.Members.Select(CreateMember))));
    }

    private static ExpressionSyntax CreateMember(EnumMemberDescriptor member)
    {
        var value = SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseExpression($"{SchemaNamespace}.{nameof(EnumScalarValue)}.{nameof(EnumScalarValue.FromRaw)}"))
            .WithArgumentList(ExecutionSyntaxFactory.CreateArgumentList(
                CreateEnumMemberAccess(nameof(EnumUnderlyingKind), member.Value.Kind.ToString()),
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(member.Value.RawValue))));

        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.ParseTypeName($"{SchemaNamespace}.{nameof(EnumMemberDescriptor)}"))
            .WithArgumentList(ExecutionSyntaxFactory.CreateArgumentList(
                ExecutionSyntaxFactory.CreateStringLiteral(member.Name),
                value));
    }

    private static ExpressionSyntax CreateEnumMemberAccess(string typeName, string memberName)
    {
        return SyntaxFactory.ParseExpression($"{SchemaNamespace}.{typeName}.{memberName}");
    }
}
