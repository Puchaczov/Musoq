using System;
using System.Dynamic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Nodes;
using Musoq.Schema.Helpers;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Handles code generation for column access expressions.
/// </summary>
public static class AccessColumnEmitter
{
    /// <summary>
    ///     Generates column access code based on the method access type.
    /// </summary>
    /// <param name="node">The access column node.</param>
    /// <param name="methodAccessType">The current method access type context.</param>
    /// <param name="generator">The Roslyn syntax generator.</param>
    /// <returns>Result containing the expression and required types.</returns>
    public static AccessColumnResult GenerateColumnAccess(
        AccessColumnNode node,
        MethodAccessType methodAccessType,
        SyntaxGenerator generator)
    {
        var variableName = GetVariableName(node.Alias, methodAccessType);

        var accessExpression = CreateElementAccessExpression(variableName, node.Name, generator);
        var types = EvaluationHelper.GetNestedTypes(node.ReturnType);
        var typeIdentifier = GetTypeIdentifier(node.ReturnType);
        var castExpression = generator.CastExpression(typeIdentifier, accessExpression);

        return new AccessColumnResult
        {
            Expression = castExpression,
            RequiredTypes = types,
            ShouldTrackForNullCheck = !node.ReturnType.IsTrueValueType()
        };
    }

    /// <summary>
    ///     Gets the variable name based on the method access type.
    /// </summary>
    private static string GetVariableName(string alias, MethodAccessType methodAccessType)
    {
        return methodAccessType switch
        {
            MethodAccessType.TransformingQuery => $"{alias}Row",
            MethodAccessType.ResultQuery or MethodAccessType.CaseWhen => "score",
            _ => throw new NotSupportedException($"Unrecognized method access type ({methodAccessType})")
        };
    }

    /// <summary>
    ///     Creates the element access expression for column lookup.
    /// </summary>
    private static SyntaxNode CreateElementAccessExpression(
        string variableName,
        string columnName,
        SyntaxGenerator generator)
    {
        return generator.ElementAccessExpression(
            generator.IdentifierName(variableName),
            SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal($"@\"{columnName}\"", columnName))));
    }

    /// <summary>
    ///     Gets the appropriate type identifier for casting.
    /// </summary>
    private static IdentifierNameSyntax GetTypeIdentifier(Type returnType)
    {
        if (returnType is NullNode.NullType) return SyntaxFactory.IdentifierName("object");

        if (typeof(IDynamicMetaObjectProvider).IsAssignableFrom(returnType))
            return SyntaxFactory.IdentifierName("dynamic");

        return SyntaxFactory.IdentifierName(EvaluationHelper.GetCastableType(returnType));
    }

    /// <summary>
    ///     Result of processing an AccessColumnNode.
    /// </summary>
    public readonly struct AccessColumnResult
    {
        /// <summary>
        ///     The generated access expression.
        /// </summary>
        public SyntaxNode Expression { get; init; }

        /// <summary>
        ///     Types that need namespace tracking.
        /// </summary>
        public Type[] RequiredTypes { get; init; }

        /// <summary>
        ///     Whether this expression should be tracked for null checking.
        /// </summary>
        public bool ShouldTrackForNullCheck { get; init; }
    }
}