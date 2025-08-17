using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Utils;
using Musoq.Parser.Nodes;
using Musoq.Plugins;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors.Helpers
{
    /// <summary>
    /// Processes CaseNode visitor logic by generating complex if-else chains with method declarations
    /// </summary>
    public static class CaseNodeProcessor
    {
        /// <summary>
        /// Processes a CaseNode by creating a complex if-else chain and generates a method declaration
        /// </summary>
        public static ProcessCaseNodeResult ProcessCaseNode(
            CaseNode node,
            Stack<SyntaxNode> nodes,
            Dictionary<string, Type> typesToInstantiate,
            MethodAccessType oldType,
            string queryAlias,
            ref int caseWhenMethodIndex)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));
            if (typesToInstantiate == null)
                throw new ArgumentNullException(nameof(typesToInstantiate));

            // Build the if-else chain
            var ifStatements = BuildIfElseChain(node, nodes);
            
            // Chain the if statements together
            var finalIfStatement = ChainIfStatements(ifStatements);
            
            // Generate method declaration
            var methodName = $"CaseWhen_{caseWhenMethodIndex++}";
            var (parameters, callParameters) = BuildMethodParameters(typesToInstantiate, oldType, queryAlias);
            var method = CreateCaseMethod(methodName, node.ReturnType, parameters, finalIfStatement);
            
            // Create method invocation
            var methodInvocation = SyntaxHelper.CreateMethodInvocation("this", methodName, callParameters.ToArray());
            
            return new ProcessCaseNodeResult
            {
                Method = method,
                MethodInvocation = methodInvocation,
                RequiredNamespaces = new[] { node.ReturnType.Namespace, typeof(IObjectResolver).Namespace }
            };
        }

        /// <summary>
        /// Builds the initial if-else chain from when-then pairs
        /// </summary>
        private static List<IfStatementSyntax> BuildIfElseChain(CaseNode node, Stack<SyntaxNode> nodes)
        {
            var ifStatements = new List<IfStatementSyntax>();
            
            // Process the first when-then pair
            var then = nodes.Pop();
            var when = nodes.Pop();
            
            var ifStatement = SyntaxFactory.IfStatement(
                (ExpressionSyntax)when,
                SyntaxFactory.Block(
                    SyntaxFactory.SingletonList<StatementSyntax>(
                        SyntaxFactory.ReturnStatement((ExpressionSyntax)then))));
            
            ifStatements.Add(ifStatement);
            
            // Process remaining when-then pairs
            for (int i = 1; i < node.WhenThenPairs.Length; i++)
            {
                then = nodes.Pop();
                when = nodes.Pop();
                
                ifStatements.Add(
                    SyntaxFactory.IfStatement(
                        (ExpressionSyntax)when,
                        SyntaxFactory.Block(
                            SyntaxFactory.SingletonList<StatementSyntax>(
                                SyntaxFactory.ReturnStatement((ExpressionSyntax)then)))));
            }
            
            // Add the else clause to the last if statement
            var elseNode = nodes.Pop();
            ifStatements[^1] = ifStatements[^1].WithElse(
                SyntaxFactory.ElseClause(
                    SyntaxFactory.Block(
                        SyntaxFactory.SingletonList<StatementSyntax>(
                            SyntaxFactory.ReturnStatement((ExpressionSyntax)elseNode)))));
            
            return ifStatements;
        }

        /// <summary>
        /// Chains multiple if statements into a nested if-else structure
        /// </summary>
        private static IfStatementSyntax ChainIfStatements(List<IfStatementSyntax> ifStatements)
        {
            if (ifStatements.Count == 1)
                return ifStatements[0];
            
            IfStatementSyntax newIfStatement = null;
            
            // Chain from the end backwards
            for (var i = ifStatements.Count - 2; i >= 1; i -= 1)
            {
                var first = ifStatements[i];
                var second = ifStatements[i + 1];
                
                ifStatements.RemoveAt(i + 1);
                ifStatements.RemoveAt(i);
                
                newIfStatement = first.WithElse(SyntaxFactory.ElseClause(second));
                ifStatements.Add(newIfStatement);
            }
            
            // Handle the final two statements
            if (ifStatements.Count == 2)
            {
                var first = ifStatements[0];
                var second = ifStatements[1];
                
                ifStatements.RemoveAt(1);
                ifStatements.RemoveAt(0);
                
                newIfStatement = first.WithElse(SyntaxFactory.ElseClause(second));
            }
            else
            {
                newIfStatement = ifStatements[0];
                ifStatements.RemoveAt(0);
            }
            
            return newIfStatement ?? throw new InvalidOperationException("Failed to chain if statements");
        }

        /// <summary>
        /// Builds method parameters and call arguments for the case method
        /// </summary>
        private static (List<ParameterSyntax> parameters, List<ArgumentSyntax> callParameters) 
            BuildMethodParameters(Dictionary<string, Type> typesToInstantiate, MethodAccessType oldType, string queryAlias)
        {
            var parameters = new List<ParameterSyntax>();
            var callParameters = new List<ArgumentSyntax>();
            
            // Add score parameter
            parameters.Add(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("score"))
                    .WithType(SyntaxFactory.IdentifierName(nameof(IObjectResolver))));
            
            // Determine row variable name based on method access type
            var rowVariableName = oldType switch
            {
                MethodAccessType.TransformingQuery => $"{queryAlias}Row",
                MethodAccessType.ResultQuery => "score",
                _ => string.Empty
            };
            
            callParameters.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(rowVariableName)));
            
            // Add instantiated type parameters
            foreach (var variableNameTypePair in typesToInstantiate)
            {
                parameters.Add(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(variableNameTypePair.Key))
                        .WithType(SyntaxFactory.IdentifierName(variableNameTypePair.Value.Name)));
                
                callParameters.Add(
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName(variableNameTypePair.Key)));
            }
            
            return (parameters, callParameters);
        }

        /// <summary>
        /// Creates the method declaration for the case logic
        /// </summary>
        private static MethodDeclarationSyntax CreateCaseMethod(
            string methodName, 
            Type returnType, 
            List<ParameterSyntax> parameters, 
            IfStatementSyntax ifStatement)
        {
            return SyntaxFactory
                .MethodDeclaration(
                    SyntaxFactory.IdentifierName(EvaluationHelper.GetCastableType(returnType)),
                    SyntaxFactory.Identifier(methodName))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
                .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters.ToArray())))
                .WithBody(SyntaxFactory.Block(SyntaxFactory.SingletonList<StatementSyntax>(ifStatement)));
        }
    }

    /// <summary>
    /// Result of processing a CaseNode
    /// </summary>
    public class ProcessCaseNodeResult
    {
        /// <summary>
        /// The generated method declaration
        /// </summary>
        public MethodDeclarationSyntax Method { get; set; }
        
        /// <summary>
        /// The method invocation syntax to replace the case node
        /// </summary>
        public InvocationExpressionSyntax MethodInvocation { get; set; }
        
        /// <summary>
        /// Required namespaces to be added
        /// </summary>
        public string[] RequiredNamespaces { get; set; }
    }
}