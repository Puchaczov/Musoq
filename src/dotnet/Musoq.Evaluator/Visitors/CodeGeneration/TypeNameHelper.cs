using System;
using System.Dynamic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Helper for generating C# type name strings from System.Type.
/// </summary>
internal static class TypeNameHelper
{
    /// <summary>
    ///     Gets the type identifier for casting expressions.
    ///     Handles NullType, dynamic types, and regular types.
    /// </summary>
    /// <param name="returnType">The return type to get the identifier for.</param>
    /// <returns>An IdentifierNameSyntax for use in cast expressions.</returns>
    public static IdentifierNameSyntax GetTypeIdentifier(Type returnType)
    {
        if (returnType is NullNode.NullType)
            return SyntaxFactory.IdentifierName("object");

        if (typeof(IDynamicMetaObjectProvider).IsAssignableFrom(returnType))
            return SyntaxFactory.IdentifierName("dynamic");

        return SyntaxFactory.IdentifierName(EvaluationHelper.GetCastableType(returnType));
    }
}
