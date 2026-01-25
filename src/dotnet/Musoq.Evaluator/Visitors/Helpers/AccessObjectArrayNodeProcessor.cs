using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
///     Specialized helper for processing AccessObjectArrayNode visitor methods.
///     Handles complex array/object access logic with type checking and safe access patterns.
/// </summary>
public static class AccessObjectArrayNodeProcessor
{
    /// <summary>
    ///     Processes an AccessObjectArrayNode and generates the appropriate C# syntax expression.
    /// </summary>
    /// <param name="node">The AccessObjectArrayNode to process</param>
    /// <param name="nodes">The syntax node stack</param>
    /// <param name="methodAccessType">The method access type context (optional, used for column-based access)</param>
    /// <returns>Processing result containing the expression and required namespace</returns>
    /// <exception cref="ArgumentNullException">Thrown when node or nodes is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when property access is attempted without a parent expression</exception>
    public static AccessObjectArrayProcessingResult ProcessAccessObjectArrayNode(
        AccessObjectArrayNode node,
        Stack<SyntaxNode> nodes,
        MethodAccessType methodAccessType = MethodAccessType.ResultQuery)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));

        var requiredNamespace = typeof(SafeArrayAccess).Namespace;
        ExpressionSyntax resultExpression;

        if (node.IsColumnAccess)
            resultExpression = ProcessColumnBasedAccess(node, methodAccessType);
        else
            resultExpression = ProcessPropertyBasedAccess(node, nodes);

        return new AccessObjectArrayProcessingResult(resultExpression, requiredNamespace);
    }

    /// <summary>
    ///     Processes column-based indexed access (e.g., Name[0], f.Name[0]).
    /// </summary>
    private static ExpressionSyntax ProcessColumnBasedAccess(AccessObjectArrayNode node,
        MethodAccessType methodAccessType)
    {
        var variableName = methodAccessType switch
        {
            MethodAccessType.TransformingQuery => $"{node.TableAlias}Row",
            MethodAccessType.ResultQuery or MethodAccessType.CaseWhen => "score",
            _ => "score"
        };

        var columnAccess = SyntaxFactory.CastExpression(
            GetCSharpType(node.ColumnType),
            SyntaxFactory.ParenthesizedExpression(
                SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName(variableName))
                    .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(node.ObjectName))))))));


        if (node.ColumnType == typeof(string)) return CreateStringCharacterAccess(columnAccess, node.Token.Index);

        if (node.ColumnType.IsArray)
            return CreateArrayElementAccessWithIntendedType(columnAccess, node.ColumnType.GetElementType(),
                node.Token.Index, node.IntendedTypeName);

        return CreateDirectElementAccess(columnAccess, node.Token.Index);
    }

    /// <summary>
    ///     Processes property-based access (original logic).
    /// </summary>
    private static ExpressionSyntax ProcessPropertyBasedAccess(AccessObjectArrayNode node, Stack<SyntaxNode> nodes)
    {
        if (nodes.Count > 0 && nodes.Peek() is ExpressionSyntax)
        {
            var exp = SyntaxFactory.ParenthesizedExpression((ExpressionSyntax)nodes.Pop());


            return SyntaxFactory.ElementAccessExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        exp,
                        SyntaxFactory.IdentifierName(node.Name)))
                .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            SyntaxFactory.Literal(node.Token.Index))))));
        }


        throw new InvalidOperationException(
            $"Cannot generate code for array access {node} - no parent expression available");
    }

    /// <summary>
    ///     Creates string character access using SafeArrayAccess.GetStringCharacter.
    /// </summary>
    private static ExpressionSyntax CreateStringCharacterAccess(ExpressionSyntax columnAccess, int index)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(SafeArrayAccess)),
                    SyntaxFactory.IdentifierName(nameof(SafeArrayAccess.GetStringCharacter))))
            .WithArgumentList(SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList([
                    SyntaxFactory.Argument(columnAccess),
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(index)))
                ])));
    }

    /// <summary>
    ///     Creates array element access using SafeArrayAccess.GetArrayElement&lt;T&gt;.
    /// </summary>
    private static ExpressionSyntax CreateArrayElementAccess(ExpressionSyntax columnAccess, Type elementType, int index)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(SafeArrayAccess)),
                    SyntaxFactory.GenericName(nameof(SafeArrayAccess.GetArrayElement))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                GetCSharpType(elementType))))))
            .WithArgumentList(SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList([
                    SyntaxFactory.Argument(columnAccess),
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(index)))
                ])));
    }

    /// <summary>
    ///     Creates array element access with support for IntendedTypeName.
    ///     When element type is object but we have an IntendedTypeName, we cast the result to the intended type.
    /// </summary>
    private static ExpressionSyntax CreateArrayElementAccessWithIntendedType(
        ExpressionSyntax columnAccess, Type elementType, int index, string intendedTypeName)
    {
        var basicAccess = CreateArrayElementAccess(columnAccess, elementType, index);


        if (elementType == typeof(object) && !string.IsNullOrEmpty(intendedTypeName))
            return SyntaxFactory.CastExpression(
                SyntaxFactory.ParseTypeName(intendedTypeName),
                SyntaxFactory.ParenthesizedExpression(basicAccess));

        return basicAccess;
    }

    /// <summary>
    ///     Creates direct element access for other indexable types.
    /// </summary>
    private static ExpressionSyntax CreateDirectElementAccess(ExpressionSyntax columnAccess, int index)
    {
        return SyntaxFactory.ElementAccessExpression(
                SyntaxFactory.ParenthesizedExpression(columnAccess))
            .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(index))))));
    }

    /// <summary>
    ///     Helper method to convert .NET types to C# syntax.
    /// </summary>
    /// <param name="type">The .NET type to convert</param>
    /// <returns>The corresponding C# TypeSyntax</returns>
    /// <exception cref="ArgumentNullException">Thrown when type is null</exception>
    public static TypeSyntax GetCSharpType(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (type == typeof(string))
            return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword));
        if (type == typeof(int))
            return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword));
        if (type == typeof(double))
            return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword));
        if (type == typeof(bool))
            return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword));
        if (type == typeof(decimal))
            return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DecimalKeyword));
        if (type == typeof(long))
            return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.LongKeyword));
        if (type == typeof(object))
            return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));


        return SyntaxFactory.IdentifierName(type.Name);
    }

    /// <summary>
    ///     Result containing the generated expression and required namespace.
    /// </summary>
    public class AccessObjectArrayProcessingResult
    {
        public AccessObjectArrayProcessingResult(ExpressionSyntax expression, string requiredNamespace)
        {
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            RequiredNamespace = requiredNamespace ?? throw new ArgumentNullException(nameof(requiredNamespace));
        }

        public ExpressionSyntax Expression { get; init; }
        public string RequiredNamespace { get; init; }
    }
}
