#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Emitter for DESC (describe) statements.
///     Handles code generation for DESCRIBE queries that return table/schema metadata.
/// </summary>
public class DescStatementEmitter(SyntaxGenerator generator)
{
    /// <summary>
    ///     Emits code for a DESC node based on its type.
    /// </summary>
    /// <param name="node">The DESC node to process</param>
    /// <param name="statements">The list to add generated statements to</param>
    /// <param name="members">The list to add generated members to</param>
    /// <param name="methodNames">The stack to push the generated method name to</param>
    /// <param name="addNamespace">Action to add required namespaces</param>
    public void EmitDescStatement(
        DescNode node,
        List<StatementSyntax> statements,
        IList<SyntaxNode> members,
        Stack<string> methodNames,
        Action<string?> addNamespace)
    {
        addNamespace(typeof(EvaluationHelper).Namespace);

        switch (node.Type)
        {
            case DescForType.SpecificConstructor:
                EmitDescForSpecificConstructor(node, statements, members, methodNames);
                break;
            case DescForType.Constructors:
                EmitDescForConstructors(node, statements, members, methodNames);
                break;
            case DescForType.Schema:
                EmitDescForSchema(node, statements, members, methodNames);
                break;
            case DescForType.FunctionsForSchema:
                EmitDescForFunctionsForSchema(node, statements, members, methodNames);
                break;
            case DescForType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(node.Type), node.Type, "Unknown DESC type");
        }

        statements.Clear();
    }

    private void EmitDescForSpecificConstructor(
        DescNode node,
        List<StatementSyntax> statements,
        IList<SyntaxNode> members,
        Stack<string> methodNames)
    {
        var invocation = CreateHelperInvocation(
            nameof(EvaluationHelper.GetSpecificTableDescription),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("schemaTable")));

        EmitDescMethod(node, invocation, true, statements, members, methodNames);
    }

    private void EmitDescForSchema(
        DescNode node,
        List<StatementSyntax> statements,
        IList<SyntaxNode> members,
        Stack<string> methodNames)
    {
        var originallyInferredColumns = SyntaxHelper.CreateEmptyColumnArray();

        var invocation = CreateHelperInvocation(
            nameof(EvaluationHelper.GetSpecificSchemaDescriptions),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("desc")),
            SyntaxFactory.Argument(CreateRuntimeContext((SchemaFromNode)node.From, originallyInferredColumns)));

        EmitDescMethod(node, invocation, false, statements, members, methodNames);
    }

    private void EmitDescForConstructors(
        DescNode node,
        List<StatementSyntax> statements,
        IList<SyntaxNode> members,
        Stack<string> methodNames)
    {
        var originallyInferredColumns = SyntaxHelper.CreateEmptyColumnArray();
        var schemaNode = (SchemaFromNode)node.From;

        var invocation = CreateHelperInvocation(
            nameof(EvaluationHelper.GetConstructorsForSpecificMethod),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("desc")),
            SyntaxHelper.StringLiteralArgument(schemaNode.Method),
            SyntaxFactory.Argument(CreateRuntimeContext(schemaNode, originallyInferredColumns)));

        EmitDescMethod(node, invocation, false, statements, members, methodNames);
    }

    private void EmitDescForFunctionsForSchema(
        DescNode node,
        List<StatementSyntax> statements,
        IList<SyntaxNode> members,
        Stack<string> methodNames)
    {
        var originallyInferredColumns = SyntaxHelper.CreateEmptyColumnArray();

        var invocation = CreateHelperInvocation(
            nameof(EvaluationHelper.GetMethodsForSchema),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("desc")),
            SyntaxFactory.Argument(CreateRuntimeContext((SchemaFromNode)node.From, originallyInferredColumns)));

        EmitDescMethod(node, invocation, false, statements, members, methodNames);
    }

    private void EmitDescMethod(
        DescNode node,
        InvocationExpressionSyntax invocationExpression,
        bool useProvidedTable,
        List<StatementSyntax> statements,
        IList<SyntaxNode> members,
        Stack<string> methodNames)
    {
        var schemaNode = (SchemaFromNode)node.From;

        var createdSchema = SyntaxHelper.CreateAssignmentByMethodCall(
            "desc",
            "provider",
            nameof(ISchemaProvider.GetSchema),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                SyntaxFactory.SeparatedList([SyntaxHelper.StringLiteralArgument(schemaNode.Schema)]),
                SyntaxFactory.Token(SyntaxKind.CloseParenToken)));

        if (useProvidedTable)
            EmitDescWithProvidedTable(schemaNode, createdSchema, invocationExpression, statements, members,
                methodNames);
        else
            EmitDescWithoutTable(createdSchema, invocationExpression, statements, members, methodNames);
    }

    private void EmitDescWithProvidedTable(
        SchemaFromNode schemaNode,
        VariableDeclarationSyntax createdSchema,
        InvocationExpressionSyntax invocationExpression,
        List<StatementSyntax> statements,
        IList<SyntaxNode> members,
        Stack<string> methodNames)
    {
        var args = schemaNode.Parameters.Args
            .Select(arg => (ExpressionSyntax)generator.LiteralExpression(((ConstantValueNode)arg).ObjValue))
            .ToArray();

        var originallyInferredColumns = SyntaxHelper.CreateEmptyColumnArray();

        var getTable = SyntaxHelper.CreateAssignmentByMethodCall(
            "schemaTable",
            "desc",
            nameof(ISchema.GetTableByName),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                SyntaxFactory.SeparatedList([
                    SyntaxHelper.StringLiteralArgument(schemaNode.Method),
                    SyntaxFactory.Argument(CreateRuntimeContext(schemaNode, originallyInferredColumns)),
                    SyntaxFactory.Argument(SyntaxHelper.CreateArrayOf(nameof(Object), args))
                ]),
                SyntaxFactory.Token(SyntaxKind.CloseParenToken)));

        var returnStatement = SyntaxFactory.ReturnStatement(invocationExpression);

        statements.AddRange([
            SyntaxFactory.LocalDeclarationStatement(createdSchema),
            SyntaxFactory.LocalDeclarationStatement(getTable),
            returnStatement
        ]);

        CreateDescMethodDeclaration(statements, members, methodNames);
    }

    private static void EmitDescWithoutTable(
        VariableDeclarationSyntax createdSchema,
        InvocationExpressionSyntax invocationExpression,
        List<StatementSyntax> statements,
        IList<SyntaxNode> members,
        Stack<string> methodNames)
    {
        var returnStatement = SyntaxFactory.ReturnStatement(invocationExpression);

        statements.AddRange([
            SyntaxFactory.LocalDeclarationStatement(createdSchema),
            returnStatement
        ]);

        CreateDescMethodDeclaration(statements, members, methodNames);
    }

    private static void CreateDescMethodDeclaration(
        List<StatementSyntax> statements,
        IList<SyntaxNode> members,
        Stack<string> methodNames)
    {
        const string methodName = "GetTableDesc";

        var method = SyntaxFactory.MethodDeclaration(
            [],
            SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
            SyntaxFactory.IdentifierName("Table").WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            null,
            SyntaxFactory.Identifier(methodName),
            null,
            CreateDescMethodParameterList(),
            [],
            SyntaxFactory.Block(statements),
            null);

        members.Add(method);
        methodNames.Push(methodName);
    }

    private static ParameterListSyntax CreateDescMethodParameterList()
    {
        return SyntaxFactory.ParameterList(
            SyntaxFactory.SeparatedList([
                SyntaxFactory.Parameter(
                    [],
                    SyntaxTokenList.Create(new SyntaxToken()),
                    SyntaxFactory.IdentifierName(nameof(ISchemaProvider))
                        .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                    SyntaxFactory.Identifier("provider"),
                    null),


                CreatePositionalEnvVarsParameter(),


                CreateQueriesInformationParameter(),


                SyntaxFactory.Parameter(
                    [],
                    SyntaxTokenList.Create(new SyntaxToken()),
                    SyntaxFactory.IdentifierName(nameof(ILogger))
                        .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                    SyntaxFactory.Identifier("logger"),
                    null),


                SyntaxFactory.Parameter(
                    [],
                    SyntaxTokenList.Create(new SyntaxToken()),
                    SyntaxFactory.IdentifierName(nameof(CancellationToken))
                        .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                    SyntaxFactory.Identifier("token"),
                    null)
            ]));
    }

    private static ParameterSyntax CreatePositionalEnvVarsParameter()
    {
        return SyntaxFactory.Parameter(
            [],
            SyntaxTokenList.Create(new SyntaxToken()),
            SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("IReadOnlyDictionary"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SeparatedList<TypeSyntax>(
                            new SyntaxNodeOrToken[]
                            {
                                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UIntKeyword)),
                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                SyntaxFactory.GenericName(
                                        SyntaxFactory.Identifier("IReadOnlyDictionary"))
                                    .WithTypeArgumentList(
                                        SyntaxFactory.TypeArgumentList(
                                            SyntaxFactory.SeparatedList<TypeSyntax>(
                                                new SyntaxNodeOrToken[]
                                                {
                                                    SyntaxFactory.PredefinedType(
                                                        SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                    SyntaxFactory.PredefinedType(
                                                        SyntaxFactory.Token(SyntaxKind.StringKeyword))
                                                })))
                            })))
                .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            SyntaxFactory.Identifier("positionalEnvironmentVariables"),
            null);
    }

    private static ParameterSyntax CreateQueriesInformationParameter()
    {
        return SyntaxFactory.Parameter(
                SyntaxFactory.Identifier("queriesInformation"))
            .WithType(
                SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier("IReadOnlyDictionary"))
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SeparatedList<TypeSyntax>(
                                new SyntaxNodeOrToken[]
                                {
                                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                    SyntaxFactory.TupleType(
                                        SyntaxFactory.SeparatedList<TupleElementSyntax>(
                                            new SyntaxNodeOrToken[]
                                            {
                                                SyntaxFactory
                                                    .TupleElement(SyntaxFactory.IdentifierName("SchemaFromNode"))
                                                    .WithIdentifier(SyntaxFactory.Identifier("FromNode")),
                                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                SyntaxFactory.TupleElement(
                                                        SyntaxFactory.GenericName(
                                                                SyntaxFactory.Identifier("IReadOnlyCollection"))
                                                            .WithTypeArgumentList(
                                                                SyntaxFactory.TypeArgumentList(
                                                                    SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                                                        SyntaxFactory
                                                                            .IdentifierName("ISchemaColumn")))))
                                                    .WithIdentifier(SyntaxFactory.Identifier("UsedColumns")),
                                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                SyntaxFactory.TupleElement(SyntaxFactory.IdentifierName("WhereNode"))
                                                    .WithIdentifier(SyntaxFactory.Identifier("WhereNode")),
                                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                SyntaxFactory.TupleElement(SyntaxFactory.IdentifierName("bool"))
                                                    .WithIdentifier(
                                                        SyntaxFactory.Identifier("HasExternallyProvidedTypes"))
                                            }))
                                }))));
    }

    private static InvocationExpressionSyntax CreateHelperInvocation(string methodName,
        params ArgumentSyntax[] arguments)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.IdentifierName(methodName)))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(arguments)));
    }

    private static ObjectCreationExpressionSyntax CreateRuntimeContext(SchemaFromNode node,
        ExpressionSyntax originallyInferredColumns)
    {
        const int schemaFromIndex = 0;

        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.IdentifierName(nameof(RuntimeContext)))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList([
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(node.Id))),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("token")),
                        SyntaxFactory.Argument(originallyInferredColumns),
                        SyntaxFactory.Argument(
                            SyntaxFactory.ElementAccessExpression(
                                    SyntaxFactory.IdentifierName("positionalEnvironmentVariables"))
                                .WithArgumentList(
                                    SyntaxFactory.BracketedArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.LiteralExpression(
                                                    SyntaxKind.NumericLiteralExpression,
                                                    SyntaxFactory.Literal(schemaFromIndex))))))),
                        SyntaxFactory.Argument(
                            SyntaxFactory.ElementAccessExpression(
                                    SyntaxFactory.IdentifierName("queriesInformation"))
                                .WithArgumentList(
                                    SyntaxFactory.BracketedArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    SyntaxFactory.Literal(node.Id))))))),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("logger")),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("OnDataSourceProgress"))
                    ])));
    }
}