using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private sealed record KeyLocalRendering(
        ExecutionValueTupleKey? ValueTupleKey,
        bool HasNullableValueTupleParts);

    private void AddKeyBuildLocal(
        List<StatementSyntax> statements,
        ExecutionExpression key,
        Type keyType,
        string keyVariableName)
    {
        if (key is ExecutionValueTupleKey valueTupleKey &&
            HasNullableValueTuplePart(valueTupleKey))
        {
            statements.AddRange(CreateValueTupleKeyPartDeclarations(valueTupleKey));
            statements.Add(CreateContinueIfAnyValueTuplePartNull(valueTupleKey));
            statements.Add(CreateValueTupleKeyLocalDeclaration(valueTupleKey, keyVariableName));
            return;
        }

        statements.Add(CreateLocalDeclaration(
            CreateHashKeyLocalType(keyType),
            keyVariableName,
            RenderExpression(key)));

        if (CanBeNull(keyType))
            statements.Add(CreateContinueIfNull(keyVariableName));
    }

    private KeyLocalRendering AddKeyProbeLocal(
        List<StatementSyntax> statements,
        ExecutionExpression key,
        Type keyType,
        string keyVariableName)
    {
        var valueTupleKey = key as ExecutionValueTupleKey;
        var hasNullableValueTupleParts = valueTupleKey != null && HasNullableValueTuplePart(valueTupleKey);

        if (valueTupleKey != null && hasNullableValueTupleParts)
        {
            statements.AddRange(CreateValueTupleKeyPartDeclarations(valueTupleKey));
            statements.Add(CreateValueTupleKeyLocalDeclaration(valueTupleKey, keyVariableName));
            return new KeyLocalRendering(valueTupleKey, hasNullableValueTupleParts);
        }

        statements.Add(CreateLocalDeclaration(
            CreateHashKeyLocalType(keyType),
            keyVariableName,
            RenderExpression(key)));
        return new KeyLocalRendering(valueTupleKey, hasNullableValueTupleParts);
    }

    private static ExpressionSyntax CreateKeyProbeCondition(
        KeyLocalRendering keyLocal,
        Type keyType,
        string keyVariableName,
        ExpressionSyntax lookupExpression)
    {
        if (keyLocal is { HasNullableValueTupleParts: true, ValueTupleKey: { } valueTupleKey })
        {
            return SyntaxFactory.BinaryExpression(
                SyntaxKind.LogicalAndExpression,
                CreateAllValueTuplePartsNotNullCondition(valueTupleKey),
                lookupExpression);
        }

        if (CanBeNull(keyType))
        {
            return SyntaxFactory.BinaryExpression(
                SyntaxKind.LogicalAndExpression,
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.NotEqualsExpression,
                    SyntaxFactory.IdentifierName(keyVariableName),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                lookupExpression);
        }

        return lookupExpression;
    }

    private static bool HasNullableValueTuplePart(ExecutionValueTupleKey valueTupleKey)
    {
        return valueTupleKey.Parts.Any(static part => CanBeNull(part.ReturnType));
    }

    private IEnumerable<StatementSyntax> CreateValueTupleKeyPartDeclarations(ExecutionValueTupleKey valueTupleKey)
    {
        for (var index = 0; index < valueTupleKey.Parts.Count; index++)
        {
            yield return CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                CreateValueTupleKeyPartVariableName(index),
                RenderExpression(valueTupleKey.Parts[index]));
        }
    }

    private static IfStatementSyntax CreateContinueIfAnyValueTuplePartNull(ExecutionValueTupleKey valueTupleKey)
    {
        return SyntaxFactory.IfStatement(
            CreateAnyValueTuplePartNullCondition(valueTupleKey),
            SyntaxFactory.ContinueStatement());
    }

    private static ExpressionSyntax CreateAnyValueTuplePartNullCondition(ExecutionValueTupleKey valueTupleKey)
    {
        return CreateValueTuplePartNullConditions(valueTupleKey, isNullCheck: true)
            .Aggregate(static (left, right) => SyntaxFactory.BinaryExpression(
                SyntaxKind.LogicalOrExpression,
                left,
                right));
    }

    private static ExpressionSyntax CreateAllValueTuplePartsNotNullCondition(ExecutionValueTupleKey valueTupleKey)
    {
        return CreateValueTuplePartNullConditions(valueTupleKey, isNullCheck: false)
            .Aggregate(static (left, right) => SyntaxFactory.BinaryExpression(
                SyntaxKind.LogicalAndExpression,
                left,
                right));
    }

    private static IEnumerable<ExpressionSyntax> CreateValueTuplePartNullConditions(
        ExecutionValueTupleKey valueTupleKey,
        bool isNullCheck)
    {
        for (var index = 0; index < valueTupleKey.Parts.Count; index++)
        {
            if (!CanBeNull(valueTupleKey.Parts[index].ReturnType))
                continue;

            yield return SyntaxFactory.BinaryExpression(
                isNullCheck ? SyntaxKind.EqualsExpression : SyntaxKind.NotEqualsExpression,
                SyntaxFactory.IdentifierName(CreateValueTupleKeyPartVariableName(index)),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
        }
    }

    private static LocalDeclarationStatementSyntax CreateValueTupleKeyLocalDeclaration(
        ExecutionValueTupleKey valueTupleKey,
        string keyVariableName)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            keyVariableName,
            SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(
                Enumerable.Range(0, valueTupleKey.Parts.Count)
                    .Select(index => SyntaxFactory.Argument(
                        SyntaxFactory.IdentifierName(CreateValueTupleKeyPartVariableName(index)))))));
    }

    private static string CreateValueTupleKeyPartVariableName(int index)
    {
        return $"key{index.ToString(CultureInfo.InvariantCulture)}";
    }

    private static TypeSyntax CreateHashKeyLocalType(Type keyType)
    {
        return IsValueTupleType(keyType)
            ? SyntaxFactory.IdentifierName("var")
            : CreateTypeSyntax(keyType);
    }

    private static bool IsValueTupleType(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition().FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true;
    }
}
