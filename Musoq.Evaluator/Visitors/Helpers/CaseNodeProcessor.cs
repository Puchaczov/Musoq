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
            ref int caseWhenMethodIndex,
            IReadOnlyList<(string VariableName, Type VariableType, string ExpressionId)> cseVariables = null)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));
            if (typesToInstantiate == null)
                throw new ArgumentNullException(nameof(typesToInstantiate));

            var ifStatements = BuildIfElseChain(node, nodes);
            
            var finalIfStatement = ChainIfStatements(ifStatements);
            
            var methodName = $"CaseWhen_{caseWhenMethodIndex++}";
            var (parameters, callParameters) = BuildMethodParameters(typesToInstantiate, oldType, queryAlias, cseVariables);
            var method = CreateCaseMethod(methodName, node.ReturnType, parameters, finalIfStatement);
            
            var methodInvocation = SyntaxHelper.CreateMethodInvocation("this", methodName, callParameters.ToArray());
            
            return new ProcessCaseNodeResult
            {
                Method = method,
                MethodInvocation = methodInvocation,
                RequiredNamespaces = new[] { node.ReturnType.Namespace, typeof(IObjectResolver).Namespace }
            };
        }

        private static List<IfStatementSyntax> BuildIfElseChain(CaseNode node, Stack<SyntaxNode> nodes)
        {
            var ifStatements = new List<IfStatementSyntax>();
            var returnTypeIdentifier = SyntaxFactory.IdentifierName(EvaluationHelper.GetCastableType(node.ReturnType));
            
            var then = CastToReturnType(nodes.Pop(), returnTypeIdentifier);
            var when = nodes.Pop();
            
            var ifStatement = SyntaxFactory.IfStatement(
                (ExpressionSyntax)when,
                SyntaxFactory.Block(
                    SyntaxFactory.SingletonList<StatementSyntax>(
                        SyntaxFactory.ReturnStatement((ExpressionSyntax)then))));
            
            ifStatements.Add(ifStatement);
            
            for (int i = 1; i < node.WhenThenPairs.Length; i++)
            {
                then = CastToReturnType(nodes.Pop(), returnTypeIdentifier);
                when = nodes.Pop();
                
                ifStatements.Add(
                    SyntaxFactory.IfStatement(
                        (ExpressionSyntax)when,
                        SyntaxFactory.Block(
                            SyntaxFactory.SingletonList<StatementSyntax>(
                                SyntaxFactory.ReturnStatement((ExpressionSyntax)then)))));
            }
            
            var elseNode = CastToReturnType(nodes.Pop(), returnTypeIdentifier);
            ifStatements[^1] = ifStatements[^1].WithElse(
                SyntaxFactory.ElseClause(
                    SyntaxFactory.Block(
                        SyntaxFactory.SingletonList<StatementSyntax>(
                            SyntaxFactory.ReturnStatement((ExpressionSyntax)elseNode)))));
            
            return ifStatements;
        }

        private static SyntaxNode CastToReturnType(SyntaxNode expression, TypeSyntax returnType)
        {
            return SyntaxFactory.CastExpression(returnType, (ExpressionSyntax)expression);
        }

        private static IfStatementSyntax ChainIfStatements(List<IfStatementSyntax> ifStatements)
        {
            if (ifStatements.Count == 1)
                return ifStatements[0];
            
            IfStatementSyntax newIfStatement = null;
            
            for (var i = ifStatements.Count - 2; i >= 1; i -= 1)
            {
                var first = ifStatements[i];
                var second = ifStatements[i + 1];
                
                ifStatements.RemoveAt(i + 1);
                ifStatements.RemoveAt(i);
                
                newIfStatement = first.WithElse(SyntaxFactory.ElseClause(second));
                ifStatements.Add(newIfStatement);
            }
            
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

        private static (List<ParameterSyntax> parameters, List<ArgumentSyntax> callParameters) BuildMethodParameters(
            Dictionary<string, Type> typesToInstantiate,
            MethodAccessType oldType,
            string queryAlias,
            IReadOnlyList<(string VariableName, Type VariableType, string ExpressionId)> cseVariables
        )
        {
            var parameters = new List<ParameterSyntax>();
            var callParameters = new List<ArgumentSyntax>();
            
            parameters.Add(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("score"))
                    .WithType(SyntaxFactory.IdentifierName(nameof(IObjectResolver))));
            
            var rowVariableName = oldType switch
            {
                MethodAccessType.TransformingQuery => $"{queryAlias}Row",
                MethodAccessType.ResultQuery => "score",
                _ => string.Empty
            };
            
            callParameters.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(rowVariableName)));
            
            foreach (var variableNameTypePair in typesToInstantiate)
            {
                parameters.Add(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(variableNameTypePair.Key))
                        .WithType(SyntaxFactory.IdentifierName(variableNameTypePair.Value.Name)));
                
                callParameters.Add(
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName(variableNameTypePair.Key)));
            }

            if (cseVariables == null) 
                return (parameters, callParameters);
            
            foreach (var (variableName, variableType, _) in cseVariables)
            {
                parameters.Add(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(variableName))
                        .WithType(SyntaxFactory.IdentifierName(GetTypeName(variableType))));
                    
                callParameters.Add(
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName(variableName)));
            }

            return (parameters, callParameters);
        }
        
        private static string GetTypeName(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return $"{type.GetGenericArguments()[0].Name}?";
            }
            
            return type.Name;
        }

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

        /// <summary>
        /// Result of processing a CaseNode
        /// </summary>
        public class ProcessCaseNodeResult
        {
            /// <summary>
            /// The generated method declaration
            /// </summary>
            public MethodDeclarationSyntax Method { get; init; }
        
            /// <summary>
            /// The method invocation syntax to replace the case node
            /// </summary>
            public InvocationExpressionSyntax MethodInvocation { get; init; }
        
            /// <summary>
            /// Required namespaces to be added
            /// </summary>
            public string[] RequiredNamespaces { get; init; }
        }
    }
}