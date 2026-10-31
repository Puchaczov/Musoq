using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static IEnumerable<ConstructorDeclarationSyntax> CreateGeneratedRowConstructors(
        string typeName,
        IReadOnlyList<FieldBinding> fields,
        IReadOnlySet<GeneratedRowContextConstructor>? usedConstructors,
        IReadOnlyList<FieldBinding> contexts)
    {
        var constructors = GetGeneratedRowConstructors(usedConstructors);

        foreach (var constructor in constructors)
        {
            var signature = CreateGeneratedRowConstructorSignature(
                constructor,
                contexts);
            yield return CreateGeneratedRowConstructor(typeName, fields, signature.Parameters, signature.ContextAssignments);
        }
    }

    private static GeneratedRowContextConstructor[] GetGeneratedRowConstructors(
        IReadOnlySet<GeneratedRowContextConstructor>? usedConstructors)
    {
        return usedConstructors is { Count: > 0 }
            ? usedConstructors.OrderBy(static constructor => constructor).ToArray()
            : [GeneratedRowContextConstructor.NoContext];
    }

    private static GeneratedRowConstructorSignature CreateGeneratedRowConstructorSignature(
        GeneratedRowContextConstructor constructor,
        IReadOnlyList<FieldBinding> contexts)
    {
        var contextParameters = CreateGeneratedRowContextParameters(constructor, contexts);
        var parameters = contextParameters
            .Select(static contextParameter => CreateParameter(contextParameter.ParameterName, contextParameter.Type))
            .ToArray();

        var contextAssignments = contextParameters
            .Select(static contextParameter => CreateGeneratedRowThisAssignment(
                contextParameter.FieldName,
                SyntaxFactory.IdentifierName(contextParameter.ParameterName)))
            .ToArray();

        return new(parameters, contextAssignments);
    }

    private static ConstructorDeclarationSyntax CreateGeneratedRowConstructor(
        string typeName,
        IReadOnlyList<FieldBinding> fields,
        IReadOnlyList<ParameterSyntax> contextParameters,
        IReadOnlyList<StatementSyntax> contextAssignments)
    {
        var valueParameters = fields.Select(CreateGeneratedRowValueParameter);
        var assignments = fields.Select(CreateGeneratedRowFieldAssignment);
        return SyntaxFactory.ConstructorDeclaration(typeName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(valueParameters.Concat(contextParameters))))
            .WithBody(StatementEmitter.CreateBlock(assignments.Concat(contextAssignments)));
    }

    private static ParameterSyntax CreateGeneratedRowValueParameter(FieldBinding field, int index)
    {
        return CreateParameter(CreateGeneratedRowValueParameterName(index), CreateGeneratedFieldTypeSyntax(field));
    }

    private static StatementSyntax CreateGeneratedRowFieldAssignment(FieldBinding field, int index)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateIdentifierName(GetGeneratedFieldName(field)),
                SyntaxFactory.IdentifierName(CreateGeneratedRowValueParameterName(index))));
    }

    private static string CreateGeneratedRowValueParameterName(int index)
    {
        return $"__value{index.ToString(CultureInfo.InvariantCulture)}";
    }

    private static ParameterSyntax CreateParameter(string name, TypeSyntax type)
    {
        return SyntaxFactory.Parameter(SyntaxFactory.Identifier(name))
            .WithType(type);
    }

    private static ExpressionStatementSyntax CreateGeneratedRowThisAssignment(string fieldName, ExpressionSyntax value)
    {
        return SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ThisExpression(),
                SyntaxFactory.IdentifierName(fieldName)),
            value));
    }
}
