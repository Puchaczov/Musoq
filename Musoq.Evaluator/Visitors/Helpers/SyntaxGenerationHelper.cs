using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Helper class for common C# syntax generation patterns.
/// Reduces repetitive SyntaxFactory usage and improves code readability.
/// </summary>
public static class SyntaxGenerationHelper
{
    /// <summary>
    /// Creates an argument list with the specified expressions.
    /// </summary>
    /// <param name="expressions">The expressions to include as arguments.</param>
    /// <returns>An ArgumentListSyntax with the specified expressions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when expressions is null.</exception>
    public static ArgumentListSyntax CreateArgumentList(params ExpressionSyntax[] expressions)
    {
        if (expressions == null)
            throw new ArgumentNullException(nameof(expressions));

        var arguments = expressions.Select(SyntaxFactory.Argument);
        return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments));
    }

    /// <summary>
    /// Creates an argument list with the specified arguments.
    /// </summary>
    /// <param name="arguments">The arguments to include.</param>
    /// <returns>An ArgumentListSyntax with the specified arguments.</returns>
    /// <exception cref="ArgumentNullException">Thrown when arguments is null.</exception>
    public static ArgumentListSyntax CreateArgumentList(params ArgumentSyntax[] arguments)
    {
        if (arguments == null)
            throw new ArgumentNullException(nameof(arguments));

        return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments));
    }

    /// <summary>
    /// Creates an argument list with the specified expressions as a collection.
    /// </summary>
    /// <param name="expressions">The expressions to include as arguments.</param>
    /// <returns>An ArgumentListSyntax with the specified expressions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when expressions is null.</exception>
    public static ArgumentListSyntax CreateArgumentList(IEnumerable<ExpressionSyntax> expressions)
    {
        if (expressions == null)
            throw new ArgumentNullException(nameof(expressions));

        var arguments = expressions.Select(SyntaxFactory.Argument);
        return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments));
    }

    /// <summary>
    /// Creates a method invocation expression with the specified target and method name.
    /// </summary>
    /// <param name="target">The target object or type.</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="arguments">The arguments to pass to the method.</param>
    /// <returns>An InvocationExpressionSyntax.</returns>
    /// <exception cref="ArgumentNullException">Thrown when target or methodName is null.</exception>
    /// <exception cref="ArgumentException">Thrown when methodName is empty.</exception>
    public static InvocationExpressionSyntax CreateMethodInvocation(
        ExpressionSyntax target, 
        string methodName, 
        params ExpressionSyntax[] arguments)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        if (methodName == null)
            throw new ArgumentNullException(nameof(methodName));
        if (string.IsNullOrEmpty(methodName))
            throw new ArgumentException("Method name cannot be empty", nameof(methodName));

        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            target,
            SyntaxFactory.IdentifierName(methodName));

        return SyntaxFactory.InvocationExpression(memberAccess)
            .WithArgumentList(CreateArgumentList(arguments));
    }

    /// <summary>
    /// Creates a method invocation expression with the specified target identifier and method name.
    /// </summary>
    /// <param name="targetIdentifier">The name of the target object or type.</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="arguments">The arguments to pass to the method.</param>
    /// <returns>An InvocationExpressionSyntax.</returns>
    /// <exception cref="ArgumentNullException">Thrown when targetIdentifier or methodName is null.</exception>
    /// <exception cref="ArgumentException">Thrown when targetIdentifier or methodName is empty.</exception>
    public static InvocationExpressionSyntax CreateMethodInvocation(
        string targetIdentifier, 
        string methodName, 
        params ExpressionSyntax[] arguments)
    {
        if (targetIdentifier == null)
            throw new ArgumentNullException(nameof(targetIdentifier));
        if (string.IsNullOrEmpty(targetIdentifier))
            throw new ArgumentException("Target identifier cannot be empty", nameof(targetIdentifier));

        return CreateMethodInvocation(SyntaxFactory.IdentifierName(targetIdentifier), methodName, arguments);
    }

    /// <summary>
    /// Creates a method invocation expression for static methods.
    /// </summary>
    /// <param name="typeName">The name of the type containing the static method.</param>
    /// <param name="methodName">The name of the static method to invoke.</param>
    /// <param name="arguments">The arguments to pass to the method.</param>
    /// <returns>An InvocationExpressionSyntax.</returns>
    /// <exception cref="ArgumentNullException">Thrown when typeName or methodName is null.</exception>
    /// <exception cref="ArgumentException">Thrown when typeName or methodName is empty.</exception>
    public static InvocationExpressionSyntax CreateStaticMethodInvocation(
        string typeName, 
        string methodName, 
        params ExpressionSyntax[] arguments)
    {
        if (typeName == null)
            throw new ArgumentNullException(nameof(typeName));
        if (string.IsNullOrEmpty(typeName))
            throw new ArgumentException("Type name cannot be empty", nameof(typeName));

        return CreateMethodInvocation(SyntaxFactory.IdentifierName(typeName), methodName, arguments);
    }

    /// <summary>
    /// Creates a string literal expression.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>A LiteralExpressionSyntax for the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public static LiteralExpressionSyntax CreateStringLiteral(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(value));
    }

    /// <summary>
    /// Creates a numeric literal expression.
    /// </summary>
    /// <param name="value">The numeric value.</param>
    /// <returns>A LiteralExpressionSyntax for the number.</returns>
    public static LiteralExpressionSyntax CreateNumericLiteral(int value)
    {
        return SyntaxFactory.LiteralExpression(
            SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(value));
    }

    /// <summary>
    /// Creates a boolean literal expression.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <returns>A LiteralExpressionSyntax for the boolean.</returns>
    public static LiteralExpressionSyntax CreateBooleanLiteral(bool value)
    {
        return SyntaxFactory.LiteralExpression(
            value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
    }

    /// <summary>
    /// Creates a null literal expression.
    /// </summary>
    /// <returns>A LiteralExpressionSyntax for null.</returns>
    public static LiteralExpressionSyntax CreateNullLiteral()
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
    }

    /// <summary>
    /// Creates an identifier name expression.
    /// </summary>
    /// <param name="identifier">The identifier name.</param>
    /// <returns>An IdentifierNameSyntax.</returns>
    /// <exception cref="ArgumentNullException">Thrown when identifier is null.</exception>
    /// <exception cref="ArgumentException">Thrown when identifier is empty.</exception>
    public static IdentifierNameSyntax CreateIdentifier(string identifier)
    {
        if (identifier == null)
            throw new ArgumentNullException(nameof(identifier));
        if (string.IsNullOrEmpty(identifier))
            throw new ArgumentException("Identifier cannot be empty", nameof(identifier));

        return SyntaxFactory.IdentifierName(identifier);
    }

    /// <summary>
    /// Creates a member access expression (e.g., object.property).
    /// </summary>
    /// <param name="target">The target object or expression.</param>
    /// <param name="memberName">The name of the member to access.</param>
    /// <returns>A MemberAccessExpressionSyntax.</returns>
    /// <exception cref="ArgumentNullException">Thrown when target or memberName is null.</exception>
    /// <exception cref="ArgumentException">Thrown when memberName is empty.</exception>
    public static MemberAccessExpressionSyntax CreateMemberAccess(ExpressionSyntax target, string memberName)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        if (memberName == null)
            throw new ArgumentNullException(nameof(memberName));
        if (string.IsNullOrEmpty(memberName))
            throw new ArgumentException("Member name cannot be empty", nameof(memberName));

        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            target,
            SyntaxFactory.IdentifierName(memberName));
    }

    /// <summary>
    /// Creates a member access expression with a string target identifier.
    /// </summary>
    /// <param name="targetIdentifier">The name of the target object.</param>
    /// <param name="memberName">The name of the member to access.</param>
    /// <returns>A MemberAccessExpressionSyntax.</returns>
    /// <exception cref="ArgumentNullException">Thrown when targetIdentifier or memberName is null.</exception>
    /// <exception cref="ArgumentException">Thrown when targetIdentifier or memberName is empty.</exception>
    public static MemberAccessExpressionSyntax CreateMemberAccess(string targetIdentifier, string memberName)
    {
        if (targetIdentifier == null)
            throw new ArgumentNullException(nameof(targetIdentifier));
        if (string.IsNullOrEmpty(targetIdentifier))
            throw new ArgumentException("Target identifier cannot be empty", nameof(targetIdentifier));

        return CreateMemberAccess(SyntaxFactory.IdentifierName(targetIdentifier), memberName);
    }
}