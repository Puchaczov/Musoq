using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using TextSpan = Musoq.Parser.TextSpan;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
/// Handles code generation for the IN clause (ContainsNode) processing.
/// </summary>
public static class ContainsEmitter
{
    /// <summary>
    /// Result of processing a ContainsNode.
    /// </summary>
    public readonly struct ContainsNodeResult
    {
        /// <summary>
        /// The argument list to push to the stack.
        /// </summary>
        public ArgumentListSyntax ArgumentList { get; init; }
        
        /// <summary>
        /// The method node that should be visited after processing.
        /// </summary>
        public AccessMethodNode MethodNode { get; init; }
    }

    /// <summary>
    /// Processes a ContainsNode and prepares arguments for the Contains method call.
    /// </summary>
    /// <param name="node">The contains node representing an IN clause.</param>
    /// <param name="valueExpression">The left-hand expression to check.</param>
    /// <param name="comparisonValues">The argument list of comparison values.</param>
    /// <param name="containsMethod">The Contains method info.</param>
    /// <returns>Result containing the argument list and method node to visit.</returns>
    public static ContainsNodeResult ProcessContainsNode(
        ContainsNode node,
        SyntaxNode valueExpression,
        ArgumentListSyntax comparisonValues,
        MethodInfo containsMethod)
    {
        var expressions = ExtractExpressions(comparisonValues);
        var objExpression = SyntaxHelper.CreateArrayOfObjects(node.ReturnType.Name, expressions);

        var argumentList = SyntaxGenerationHelper.CreateArgumentList(
            (ExpressionSyntax)valueExpression,
            objExpression);

        var methodNode = CreateAccessMethodNode(node, containsMethod);

        return new ContainsNodeResult
        {
            ArgumentList = argumentList,
            MethodNode = methodNode
        };
    }

    /// <summary>
    /// Extracts expressions from the argument list.
    /// </summary>
    private static ExpressionSyntax[] ExtractExpressions(ArgumentListSyntax comparisonValues)
    {
        var expressions = new ExpressionSyntax[comparisonValues.Arguments.Count];
        for (var index = 0; index < comparisonValues.Arguments.Count; index++)
        {
            var argument = comparisonValues.Arguments[index];
            expressions[index] = argument.Expression;
        }
        return expressions;
    }

    /// <summary>
    /// Creates the AccessMethodNode for the Contains call.
    /// </summary>
    private static AccessMethodNode CreateAccessMethodNode(ContainsNode node, MethodInfo containsMethod)
    {
        return new AccessMethodNode(
            new FunctionToken(nameof(Operators.Contains), TextSpan.Empty),
            new ArgsListNode([node.Left, node.Right]), 
            null, 
            false,
            containsMethod);
    }
}
