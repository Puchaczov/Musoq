using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static LocalDeclarationStatementSyntax CreateEnumerableChunksDeclaration(
        string rowsVariableName,
        ExpressionSyntax sourceExpression,
        TypeSyntax elementType)
    {
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.GenericName(nameof(EvaluationHelper.ConvertEnumerableOutputToChunks))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList(elementType)))))
            .WithArgumentList(CreateArgumentList(sourceExpression));

        return CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), rowsVariableName, invocation);
    }

    private static LocalDeclarationStatementSyntax CreateTypedEnumerableChunksDeclaration(
        string rowsVariableName,
        ExpressionSyntax sourceExpression,
        Type enumerableType,
        ExecutionEnumerableChunkMode chunkMode,
        string? enumerableTypeName = null)
    {
        var generatedElementTypeName = ResolveGeneratedEnumerableElementTypeName(enumerableTypeName);
        if (!string.IsNullOrWhiteSpace(generatedElementTypeName))
        {
            return CreateEnumerableChunksDeclaration(
                rowsVariableName,
                sourceExpression,
                SyntaxFactory.ParseTypeName(generatedElementTypeName));
        }

        var elementType = ResolveEnumerableElementType(enumerableType);
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.GenericName(ResolveTypedChunksMethodName(elementType, chunkMode))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList(CreateTypeSyntax(elementType))))))
            .WithArgumentList(CreateArgumentList(sourceExpression));

        return CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), rowsVariableName, invocation);
    }

    private static string? ResolveGeneratedEnumerableElementTypeName(string? enumerableTypeName)
    {
        if (string.IsNullOrWhiteSpace(enumerableTypeName))
            return null;

        var typeName = enumerableTypeName.Trim();
        if (typeName.EndsWith("[]", StringComparison.Ordinal))
            return typeName[..^2];

        const string enumerablePrefix = "System.Collections.Generic.IEnumerable<";
        if (typeName.StartsWith(enumerablePrefix, StringComparison.Ordinal) &&
            typeName.EndsWith(">", StringComparison.Ordinal))
        {
            return typeName[enumerablePrefix.Length..^1];
        }

        return ResolveEnumerableElementTypeNameFromAlias(typeName, "IEnumerable") ??
               ResolveEnumerableElementTypeNameFromAlias(typeName, "IReadOnlyList") ??
               typeName;
    }

    private static string? ResolveEnumerableElementTypeNameFromAlias(string typeName, string alias)
    {
        var prefix = $"{alias}<";
        return typeName.StartsWith(prefix, StringComparison.Ordinal) &&
               typeName.EndsWith(">", StringComparison.Ordinal)
            ? typeName[prefix.Length..^1]
            : null;
    }

    private static string ResolveTypedChunksMethodName(Type elementType, ExecutionEnumerableChunkMode chunkMode)
    {
        if (chunkMode == ExecutionEnumerableChunkMode.DirectScalar)
            return nameof(EvaluationHelper.ConvertEnumerableOutputToChunks);

        return IsScalarEnumerableElement(elementType)
            ? nameof(EvaluationHelper.ConvertScalarEnumerableToTypedChunks)
            : nameof(EvaluationHelper.ConvertEnumerableOutputToChunks);
    }

    private static Type ResolveEnumerableElementType(Type enumerableType)
    {
        if (enumerableType.IsArray)
            return enumerableType.GetElementType()!;

        if (enumerableType.IsGenericType && enumerableType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return enumerableType.GetGenericArguments()[0];

        if (enumerableType != typeof(string))
        {
            var enumerableInterface = enumerableType
                .GetInterfaces()
                .FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerableInterface != null)
                return enumerableInterface.GetGenericArguments()[0];
        }

        return enumerableType;
    }

    private static bool IsScalarEnumerableElement(Type elementType)
    {
        return elementType.IsPrimitive ||
               elementType == typeof(string) ||
               elementType == typeof(decimal) ||
               elementType == typeof(DateTime) ||
               elementType == typeof(Guid);
    }
}
