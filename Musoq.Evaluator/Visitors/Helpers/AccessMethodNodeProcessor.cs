using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Helper class for processing AccessMethodNode operations in ToCSharpRewriteTreeVisitor.
/// Handles method library instantiation, parameter injection, and invocation expression generation.
/// </summary>
public static class AccessMethodNodeProcessor
{
    /// <summary>
    /// Processes an AccessMethodNode and generates the appropriate syntax node.
    /// </summary>
    /// <param name="node">The AccessMethodNode to process</param>
    /// <param name="generator">The syntax generator</param>
    /// <param name="nodes">The stack of syntax nodes</param>
    /// <param name="statements">The list of statements to add to</param>
    /// <param name="typesToInstantiate">Dictionary of types to instantiate</param>
    /// <param name="scope">The current scope</param>
    /// <param name="type">The method access type</param>
    /// <param name="isInsideJoinOrApply">Whether we're inside a join or apply operation</param>
    /// <param name="nullSuspiciousNodes">List of null suspicious node stacks</param>
    /// <param name="addNamespaceAction">Action to add namespaces</param>
    /// <returns>The generated syntax node for the method invocation</returns>
    public static SyntaxNode ProcessAccessMethodNode(
        AccessMethodNode node,
        SyntaxGenerator generator,
        Stack<SyntaxNode> nodes,
        List<StatementSyntax> statements,
        Dictionary<string, Type> typesToInstantiate,
        Scope scope,
        MethodAccessType type,
        bool isInsideJoinOrApply,
        List<Stack<SyntaxNode>> nullSuspiciousNodes,
        Action<string> addNamespaceAction)
    {
        ValidateParameters(node, generator, nodes, statements, typesToInstantiate, scope, addNamespaceAction);

        var method = node.Method;
        var variableName = $"{node.Alias}{method.ReflectedType!.Name}Lib";

        // Handle library instantiation
        HandleLibraryInstantiation(node, statements, typesToInstantiate, variableName, addNamespaceAction);

        // Add symbol to scope
        scope.ScopeSymbolTable.AddSymbolIfNotExist(
            method.ReflectedType.Name,
            new TypeSymbol(method.ReflectedType));

        // Process method parameters and build arguments
        var args = ProcessMethodParameters(node, scope, type, isInsideJoinOrApply);

        // Add user-provided arguments from the stack
        var tmpArgs = (ArgumentListSyntax)nodes.Pop();
        foreach (var item in tmpArgs.Arguments)
        {
            args.Add(item);
        }

        // Generate method invocation expression
        var accessMethodExpr = GenerateMethodInvocation(node, generator, variableName, args);

        // Handle null suspicion tracking
        if (!node.ReturnType.IsTrueValueType() && nullSuspiciousNodes.Count > 0)
        {
            nullSuspiciousNodes[^1].Push(accessMethodExpr);
        }

        return accessMethodExpr;
    }

    private static void ValidateParameters(
        AccessMethodNode node,
        SyntaxGenerator generator,
        Stack<SyntaxNode> nodes,
        List<StatementSyntax> statements,
        Dictionary<string, Type> typesToInstantiate,
        Scope scope,
        Action<string> addNamespaceAction)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        if (generator == null) throw new ArgumentNullException(nameof(generator));
        if (nodes == null) throw new ArgumentNullException(nameof(nodes));
        if (statements == null) throw new ArgumentNullException(nameof(statements));
        if (typesToInstantiate == null) throw new ArgumentNullException(nameof(typesToInstantiate));
        if (scope == null) throw new ArgumentNullException(nameof(scope));
        if (addNamespaceAction == null) throw new ArgumentNullException(nameof(addNamespaceAction));
    }

    private static void HandleLibraryInstantiation(
        AccessMethodNode node,
        List<StatementSyntax> statements,
        Dictionary<string, Type> typesToInstantiate,
        string variableName,
        Action<string> addNamespaceAction)
    {
        if (!typesToInstantiate.ContainsKey(variableName))
        {
            var method = node.Method;
            typesToInstantiate.Add(variableName, method.ReflectedType);
            addNamespaceAction(method.ReflectedType.Namespace);

            statements.Add(
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxHelper.CreateAssignment(
                        variableName,
                        SyntaxHelper.CreateObjectOf(
                            method.ReflectedType.FullName!.Replace("+", "."),
                            SyntaxFactory.ArgumentList()))));
        }
    }

    private static List<ArgumentSyntax> ProcessMethodParameters(
        AccessMethodNode node,
        Scope scope,
        MethodAccessType type,
        bool isInsideJoinOrApply)
    {
        var args = new List<ArgumentSyntax>();
        var parameters = node.Method.GetParameters().GetParametersWithAttribute<InjectTypeAttribute>();

        foreach (var parameterInfo in parameters)
        {
            var attribute = parameterInfo.GetCustomAttributeThatInherits<InjectTypeAttribute>();

            switch (attribute)
            {
                case InjectSpecificSourceAttribute _:
                case InjectSourceAttribute _:
                    ProcessInjectSourceAttribute(node, scope, type, isInsideJoinOrApply, args, parameterInfo);
                    break;

                case InjectGroupAttribute _:
                    ProcessInjectGroupAttribute(type, args);
                    break;

                case InjectGroupAccessName _:
                    // No action needed for this attribute type
                    break;

                case InjectQueryStatsAttribute _:
                    ProcessInjectQueryStatsAttribute(args);
                    break;
            }
        }

        return args;
    }

    private static void ProcessInjectSourceAttribute(
        AccessMethodNode node,
        Scope scope,
        MethodAccessType type,
        bool isInsideJoinOrApply,
        List<ArgumentSyntax> args,
        System.Reflection.ParameterInfo parameterInfo)
    {
        if (node.CanSkipInjectSource)
            return;

        var componentsOfComplexTable = scope[MetaAttributes.Contexts].Split(',');
        var objectName = GetObjectNameForMethodAccessType(type, componentsOfComplexTable, node.Alias);

        var typeIdentifier = SyntaxFactory.IdentifierName(
            EvaluationHelper.GetCastableType(parameterInfo.ParameterType));

        if (parameterInfo.ParameterType == typeof(ExpandoObject))
        {
            typeIdentifier = SyntaxFactory.IdentifierName("dynamic");
        }

        var currentContext = GetCurrentContext(scope, isInsideJoinOrApply, node.Alias);

        args.Add(CreateContextAccessArgument(typeIdentifier, objectName, currentContext));
    }

    private static string GetObjectNameForMethodAccessType(
        MethodAccessType type,
        string[] componentsOfComplexTable,
        string alias)
    {
        return type switch
        {
            MethodAccessType.TransformingQuery => $"{componentsOfComplexTable.First(f => f.Contains(alias))}Row",
            MethodAccessType.ResultQuery or MethodAccessType.CaseWhen => "score",
            _ => throw new NotSupportedException($"Unrecognized method access type ({type})")
        };
    }

    private static int GetCurrentContext(Scope scope, bool isInsideJoinOrApply, string alias)
    {
        if (isInsideJoinOrApply)
        {
            var preformattedContexts = (IndexBasedContextsPositionsSymbol)scope.ScopeSymbolTable
                .GetSymbol(MetaAttributes.PreformatedContexts);
            var orderNumber = int.Parse(scope[MetaAttributes.OrderNumber]);
            return preformattedContexts.GetIndexFor(orderNumber, alias);
        }
        else
        {
            var aliases = scope.Parent.ScopeSymbolTable
                .GetSymbol<AliasesPositionsSymbol>(MetaAttributes.AllQueryContexts);
            return aliases.GetContextIndexOf(alias);
        }
    }

    private static ArgumentSyntax CreateContextAccessArgument(
        IdentifierNameSyntax typeIdentifier,
        string objectName,
        int currentContext)
    {
        return SyntaxFactory.Argument(
            SyntaxFactory.CastExpression(
                typeIdentifier,
                SyntaxFactory.ElementAccessExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(objectName),
                        SyntaxFactory.IdentifierName(nameof(IObjectResolver.Contexts))),
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SeparatedList(
                        [
                            SyntaxFactory.Argument(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(currentContext)))
                        ])))));
    }

    private static void ProcessInjectGroupAttribute(MethodAccessType type, List<ArgumentSyntax> args)
    {
        switch (type)
        {
            case MethodAccessType.ResultQuery:
                // Do not inject in result query
                break;
            default:
                args.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("group")));
                break;
        }
    }

    private static void ProcessInjectQueryStatsAttribute(List<ArgumentSyntax> args)
    {
        args.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("currentRowStats")));
    }

    private static SyntaxNode GenerateMethodInvocation(
        AccessMethodNode node,
        SyntaxGenerator generator,
        string variableName,
        List<ArgumentSyntax> args)
    {
        var method = node.Method;

        if (method.IsGenericMethod && method.GetCustomAttribute<AggregationMethodAttribute>() != null)
        {
            return GenerateGenericMethodInvocation(node, generator, variableName, args);
        }
        else
        {
            return generator.InvocationExpression(
                generator.MemberAccessExpression(
                    generator.IdentifierName(variableName),
                    generator.IdentifierName(node.Name)),
                args);
        }
    }

    private static SyntaxNode GenerateGenericMethodInvocation(
        AccessMethodNode node,
        SyntaxGenerator generator,
        string variableName,
        List<ArgumentSyntax> args)
    {
        var genericArgs = node.Method.GetGenericArguments();

        if (genericArgs.Length == 0)
            throw new NotSupportedException("Generic method without generic arguments.");

        var syntaxArgs = new List<SyntaxNodeOrToken>();

        for (var i = 0; i < genericArgs.Length - 1; ++i)
        {
            syntaxArgs.Add(SyntaxFactory.IdentifierName(genericArgs[i].FullName!));
            syntaxArgs.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
        }

        syntaxArgs.Add(SyntaxFactory.IdentifierName(genericArgs[^1].FullName!));

        TypeArgumentListSyntax typeArgs;
        if (syntaxArgs.Count < 2)
        {
            var syntaxArg = (IdentifierNameSyntax)syntaxArgs[0];
            typeArgs = SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(syntaxArg!)
            );
        }
        else
        {
            typeArgs = SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SeparatedList<TypeSyntax>(
                    syntaxArgs.ToArray()));
        }

        var genericName = SyntaxFactory
            .GenericName(node.Name)
            .WithTypeArgumentList(
                typeArgs
                    .WithLessThanToken(
                        SyntaxFactory.Token(SyntaxKind.LessThanToken))
                    .WithGreaterThanToken(
                        SyntaxFactory.Token(SyntaxKind.GreaterThanToken)));

        return generator.InvocationExpression(
            generator.MemberAccessExpression(
                generator.IdentifierName(variableName),
                genericName),
            args);
    }
}