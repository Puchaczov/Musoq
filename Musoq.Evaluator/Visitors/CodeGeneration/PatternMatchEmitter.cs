using System;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using Musoq.Parser;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
/// Emitter for pattern matching operations (LIKE, RLIKE).
/// </summary>
public static class PatternMatchEmitter
{
    /// <summary>
    /// Processes a pattern match node (LIKE or RLIKE) and generates the appropriate method call.
    /// </summary>
    /// <param name="left">The left operand node.</param>
    /// <param name="right">The right operand node.</param>
    /// <param name="operatorName">The name of the operator (e.g., "Like", "RLike").</param>
    /// <param name="method">The MethodInfo for the operator method.</param>
    /// <param name="nodes">The syntax node stack.</param>
    /// <param name="visitAccessMethod">Action to visit the AccessMethodNode.</param>
    public static void ProcessPatternMatch(
        Node left,
        Node right,
        string operatorName,
        MethodInfo method,
        Stack<SyntaxNode> nodes,
        Action<AccessMethodNode> visitAccessMethod)
    {
        var b = nodes.Pop();
        var a = nodes.Pop();

        var arg = SyntaxGenerationHelper.CreateArgumentList(
            (ExpressionSyntax)a,
            (ExpressionSyntax)b);

        nodes.Push(arg);

        var accessMethodNode = new AccessMethodNode(
            new FunctionToken(operatorName, TextSpan.Empty),
            new ArgsListNode([left, right]), 
            null, 
            false,
            method);

        visitAccessMethod(accessMethodNode);
    }
}
