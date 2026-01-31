#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
///     Helper class for processing InterpretCallNode, ParseCallNode, and InterpretAtCallNode
///     operations in ToCSharpRewriteTreeVisitor.
///     Handles interpreter instantiation and invocation expression generation.
/// </summary>
public static class InterpretCallNodeProcessor
{
    /// <summary>
    ///     Processes an InterpretCallNode and generates the appropriate syntax for
    ///     calling the compiled interpreter's Interpret method.
    /// </summary>
    /// <param name="node">The InterpretCallNode to process</param>
    /// <param name="generator">The syntax generator</param>
    /// <param name="nodes">The stack of syntax nodes containing the data source expression</param>
    /// <param name="statements">The list of statements to add interpreter instantiation to</param>
    /// <param name="interpreterInstances">Dictionary tracking interpreter instances by schema name</param>
    /// <param name="addNamespaceAction">Action to add required namespaces</param>
    /// <param name="schemaRegistry">Optional schema registry with compiled interpreter types</param>
    /// <returns>The generated syntax node for the interpreter invocation</returns>
    public static SyntaxNode ProcessInterpretCallNode(
        InterpretCallNode node,
        SyntaxGenerator generator,
        Stack<SyntaxNode> nodes,
        List<StatementSyntax> statements,
        Dictionary<string, string> interpreterInstances,
        Action<string> addNamespaceAction,
        SchemaRegistry? schemaRegistry = null)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        if (generator == null) throw new ArgumentNullException(nameof(generator));
        if (nodes == null) throw new ArgumentNullException(nameof(nodes));
        if (statements == null) throw new ArgumentNullException(nameof(statements));
        if (interpreterInstances == null) throw new ArgumentNullException(nameof(interpreterInstances));


        var dataSourceExpr = (ExpressionSyntax)nodes.Pop();


        var interpreterVarName = GetOrCreateInterpreterInstance(
            node.SchemaName,
            statements,
            interpreterInstances,
            addNamespaceAction,
            schemaRegistry);


        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(interpreterVarName),
                    SyntaxFactory.IdentifierName("Interpret")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(dataSourceExpr))));

        return invocation;
    }

    /// <summary>
    ///     Processes a ParseCallNode and generates the appropriate syntax for
    ///     calling the compiled interpreter's Parse method.
    /// </summary>
    public static SyntaxNode ProcessParseCallNode(
        ParseCallNode node,
        SyntaxGenerator generator,
        Stack<SyntaxNode> nodes,
        List<StatementSyntax> statements,
        Dictionary<string, string> interpreterInstances,
        Action<string> addNamespaceAction,
        SchemaRegistry? schemaRegistry = null)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        if (generator == null) throw new ArgumentNullException(nameof(generator));
        if (nodes == null) throw new ArgumentNullException(nameof(nodes));
        if (statements == null) throw new ArgumentNullException(nameof(statements));
        if (interpreterInstances == null) throw new ArgumentNullException(nameof(interpreterInstances));


        var dataSourceExpr = (ExpressionSyntax)nodes.Pop();


        var interpreterVarName = GetOrCreateInterpreterInstance(
            node.SchemaName,
            statements,
            interpreterInstances,
            addNamespaceAction,
            schemaRegistry);


        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(interpreterVarName),
                    SyntaxFactory.IdentifierName("Parse")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(dataSourceExpr))));

        return invocation;
    }

    /// <summary>
    ///     Processes an InterpretAtCallNode and generates the appropriate syntax for
    ///     calling the compiled interpreter's InterpretAt method.
    /// </summary>
    public static SyntaxNode ProcessInterpretAtCallNode(
        InterpretAtCallNode node,
        SyntaxGenerator generator,
        Stack<SyntaxNode> nodes,
        List<StatementSyntax> statements,
        Dictionary<string, string> interpreterInstances,
        Action<string> addNamespaceAction,
        SchemaRegistry? schemaRegistry = null)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        if (generator == null) throw new ArgumentNullException(nameof(generator));
        if (nodes == null) throw new ArgumentNullException(nameof(nodes));
        if (statements == null) throw new ArgumentNullException(nameof(statements));
        if (interpreterInstances == null) throw new ArgumentNullException(nameof(interpreterInstances));


        var offsetExpr = (ExpressionSyntax)nodes.Pop();


        var dataSourceExpr = (ExpressionSyntax)nodes.Pop();


        var interpreterVarName = GetOrCreateInterpreterInstance(
            node.SchemaName,
            statements,
            interpreterInstances,
            addNamespaceAction,
            schemaRegistry);


        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(interpreterVarName),
                    SyntaxFactory.IdentifierName("InterpretAt")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(dataSourceExpr),
                        SyntaxFactory.Argument(offsetExpr)
                    })));

        return invocation;
    }

    /// <summary>
    ///     Processes a TryInterpretCallNode and generates the appropriate syntax for
    ///     safely calling the compiled interpreter's Interpret method, returning null on failure.
    /// </summary>
    public static SyntaxNode ProcessTryInterpretCallNode(
        TryInterpretCallNode node,
        SyntaxGenerator generator,
        Stack<SyntaxNode> nodes,
        List<StatementSyntax> statements,
        Dictionary<string, string> interpreterInstances,
        Action<string> addNamespaceAction,
        SchemaRegistry? schemaRegistry = null)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        if (generator == null) throw new ArgumentNullException(nameof(generator));
        if (nodes == null) throw new ArgumentNullException(nameof(nodes));
        if (statements == null) throw new ArgumentNullException(nameof(statements));
        if (interpreterInstances == null) throw new ArgumentNullException(nameof(interpreterInstances));


        var dataSourceExpr = (ExpressionSyntax)nodes.Pop();


        var interpreterVarName = GetOrCreateInterpreterInstance(
            node.SchemaName,
            statements,
            interpreterInstances,
            addNamespaceAction,
            schemaRegistry);


        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(interpreterVarName),
                    SyntaxFactory.IdentifierName("Interpret")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(dataSourceExpr))));


        string? typeName = null;
        if (schemaRegistry != null && schemaRegistry.TryGetSchema(node.SchemaName, out var registration))
        {
            typeName = registration?.GeneratedTypeName;
            if (typeName == null && registration?.GeneratedType != null)
                typeName = EvaluationHelper.GetCastableType(registration.GeneratedType);
        }


        if (typeName == null && node.ReturnType != null) typeName = EvaluationHelper.GetCastableType(node.ReturnType);

        return WrapInTryCatchReturningNullByTypeName(invocation, typeName);
    }

    /// <summary>
    ///     Processes a TryParseCallNode and generates the appropriate syntax for
    ///     safely calling the compiled interpreter's Parse method, returning null on failure.
    /// </summary>
    public static SyntaxNode ProcessTryParseCallNode(
        TryParseCallNode node,
        SyntaxGenerator generator,
        Stack<SyntaxNode> nodes,
        List<StatementSyntax> statements,
        Dictionary<string, string> interpreterInstances,
        Action<string> addNamespaceAction,
        SchemaRegistry? schemaRegistry = null)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        if (generator == null) throw new ArgumentNullException(nameof(generator));
        if (nodes == null) throw new ArgumentNullException(nameof(nodes));
        if (statements == null) throw new ArgumentNullException(nameof(statements));
        if (interpreterInstances == null) throw new ArgumentNullException(nameof(interpreterInstances));


        var dataSourceExpr = (ExpressionSyntax)nodes.Pop();


        var interpreterVarName = GetOrCreateInterpreterInstance(
            node.SchemaName,
            statements,
            interpreterInstances,
            addNamespaceAction,
            schemaRegistry);


        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(interpreterVarName),
                    SyntaxFactory.IdentifierName("Parse")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(dataSourceExpr))));


        string? typeName = null;
        if (schemaRegistry != null && schemaRegistry.TryGetSchema(node.SchemaName, out var registration))
        {
            typeName = registration?.GeneratedTypeName;
            if (typeName == null && registration?.GeneratedType != null)
                typeName = EvaluationHelper.GetCastableType(registration.GeneratedType);
        }


        if (typeName == null && node.ReturnType != null) typeName = EvaluationHelper.GetCastableType(node.ReturnType);

        return WrapInTryCatchReturningNullByTypeName(invocation, typeName);
    }

    /// <summary>
    ///     Processes a PartialInterpretCallNode and generates the appropriate syntax for
    ///     calling the compiled interpreter's PartialInterpret method, returning partial results.
    /// </summary>
    public static SyntaxNode ProcessPartialInterpretCallNode(
        PartialInterpretCallNode node,
        SyntaxGenerator generator,
        Stack<SyntaxNode> nodes,
        List<StatementSyntax> statements,
        Dictionary<string, string> interpreterInstances,
        Action<string> addNamespaceAction,
        SchemaRegistry? schemaRegistry = null)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        if (generator == null) throw new ArgumentNullException(nameof(generator));
        if (nodes == null) throw new ArgumentNullException(nameof(nodes));
        if (statements == null) throw new ArgumentNullException(nameof(statements));
        if (interpreterInstances == null) throw new ArgumentNullException(nameof(interpreterInstances));


        var dataSourceExpr = (ExpressionSyntax)nodes.Pop();


        var interpreterVarName = GetOrCreateInterpreterInstance(
            node.SchemaName,
            statements,
            interpreterInstances,
            addNamespaceAction,
            schemaRegistry);


        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(interpreterVarName),
                    SyntaxFactory.IdentifierName("PartialInterpret")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(dataSourceExpr))));

        return invocation;
    }

    private static ExpressionSyntax WrapInTryCatchReturningNull(ExpressionSyntax expression, Type? returnType)
    {
        var typeName = returnType != null
            ? EvaluationHelper.GetCastableType(returnType)
            : null;
        return WrapInTryCatchReturningNullByTypeName(expression, typeName);
    }

    private static ExpressionSyntax WrapInTryCatchReturningNullByTypeName(ExpressionSyntax expression, string? typeName)
    {
        var tryBlock = SyntaxFactory.Block(
            SyntaxFactory.ReturnStatement(expression));


        var catchClause = SyntaxFactory.CatchClause()
            .WithBlock(SyntaxFactory.Block(
                SyntaxFactory.ReturnStatement(
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))));


        var tryCatch = SyntaxFactory.TryStatement()
            .WithBlock(tryBlock)
            .WithCatches(SyntaxFactory.SingletonList(catchClause));


        var lambda = SyntaxFactory.ParenthesizedLambdaExpression()
            .WithBlock(SyntaxFactory.Block(tryCatch));


        TypeSyntax funcReturnType;
        if (typeName != null)
            funcReturnType = SyntaxFactory.NullableType(SyntaxFactory.ParseTypeName(typeName));
        else
            funcReturnType =
                SyntaxFactory.NullableType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)));


        var funcType = SyntaxFactory.GenericName("Func")
            .WithTypeArgumentList(
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(funcReturnType)));

        var funcInstantiation = SyntaxFactory.ObjectCreationExpression(funcType)
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(lambda))));


        var invocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.ParenthesizedExpression(funcInstantiation));

        return invocation;
    }

    private static string GetOrCreateInterpreterInstance(
        string schemaName,
        List<StatementSyntax> statements,
        Dictionary<string, string> interpreterInstances,
        Action<string> addNamespaceAction,
        SchemaRegistry? schemaRegistry = null)
    {
        if (interpreterInstances.TryGetValue(schemaName, out var existingVarName)) return existingVarName;


        var varName = $"_interpreter_{schemaName}";


        string interpreterTypeName;
        string? interpreterNamespace = null;


        if (schemaRegistry != null)
        {
            if (schemaRegistry.TryGetSchema(schemaName, out var registration) && registration != null)
            {
                if (!string.IsNullOrEmpty(registration.GeneratedTypeName))
                {
                    interpreterTypeName = registration.GeneratedTypeName;

                    var lastDotIndex = interpreterTypeName.LastIndexOf('.');
                    if (lastDotIndex > 0) interpreterNamespace = interpreterTypeName.Substring(0, lastDotIndex);
                }

                else if (registration.GeneratedType != null)
                {
                    interpreterTypeName = registration.GeneratedType.FullName ?? registration.GeneratedType.Name;
                    interpreterNamespace = registration.GeneratedType.Namespace;
                }
                else
                {
                    throw UnknownInterpretationSchemaException.CreateForTypeGenerationFailed(schemaName);
                }
            }
            else
            {
                throw UnknownInterpretationSchemaException.CreateForSchemaNotInRegistry(schemaName);
            }
        }
        else
        {
            interpreterTypeName = schemaName;
            interpreterNamespace = "Musoq.Generated.Interpreters";
        }


        var declaration = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(varName))
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.ObjectCreationExpression(
                                            SyntaxFactory.IdentifierName(interpreterTypeName))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList()))))));

        statements.Add(declaration);
        interpreterInstances[schemaName] = varName;


        if (!string.IsNullOrEmpty(interpreterNamespace)) addNamespaceAction?.Invoke(interpreterNamespace);

        return varName;
    }
}
