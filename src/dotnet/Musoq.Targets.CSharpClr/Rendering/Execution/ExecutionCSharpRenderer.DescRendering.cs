using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderReturnDesc(ExecutionReturnDesc desc, ExecutionRenderContext context)
    {
        ValidateDesc(desc);

        if (desc.Type == DescType.Query)
        {
            yield return StatementEmitter.CreateReturn(CreateDescReturnExpression(desc, context));
            yield break;
        }

        yield return CreateSchemaDeclaration(DescSchemaVariableName, desc.SchemaName);
        yield return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            DescEmptyInferredColumnsVariableName,
            SyntaxHelper.CreateEmptyColumnArray());
        yield return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            DescRuntimeContextVariableName,
            CreateDescRuntimeContext(desc));

        if (RequiresSchemaTable(desc.Type))
            yield return CreateDescSchemaTableDeclaration(desc, context);

        yield return StatementEmitter.CreateReturn(CreateDescReturnExpression(desc, context));
    }

    private LocalDeclarationStatementSyntax CreateDescSchemaTableDeclaration(
        ExecutionReturnDesc desc,
        ExecutionRenderContext context)
    {
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(DescSchemaVariableName),
                    SyntaxFactory.IdentifierName("GetTableByName")))
            .WithArgumentList(CreateArgumentList(
                CreateStringLiteral(desc.MethodName),
                SyntaxFactory.IdentifierName(DescRuntimeContextVariableName),
                CreateDescArgumentsExpression(desc, context)));

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            DescSchemaTableVariableName,
            invocation);
    }

    private ObjectCreationExpressionSyntax CreateDescRuntimeContext(ExecutionReturnDesc desc)
    {
        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(nameof(SourceExecutionContext)))
            .WithArgumentList(CreateArgumentList(
                CreateStringLiteral(desc.RuntimeContextId),
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(nameof(SourceExecutionPlan)),
                            SyntaxFactory.IdentifierName(nameof(SourceExecutionPlan.Empty))))
                    .WithArgumentList(CreateArgumentList(
                        SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.IdentifierName(nameof(SourceIdentity)))
                            .WithArgumentList(CreateArgumentList(
                                CreateStringLiteral(desc.SchemaName),
                                CreateStringLiteral(desc.MethodName),
                                CreateStringLiteral(desc.RuntimeContextId),
                                CreateStringLiteral(string.Empty))))),
                SyntaxFactory.IdentifierName("token"),
                SyntaxFactory.IdentifierName(DescEmptyInferredColumnsVariableName),
                CreateDescSourceRuntimeSettingsExpression(desc.RuntimeContextId),
                SyntaxFactory.IdentifierName("logger"),
                SyntaxFactory.IdentifierName("OnDataSourceProgress")));
    }

    private static ConditionalExpressionSyntax CreateDescSourceRuntimeSettingsExpression(string runtimeContextId)
    {
        var tryGetValue = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("sourceRuntimeSettingsBySourceContextId"),
                    SyntaxFactory.IdentifierName("TryGetValue")))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                SyntaxFactory.Argument(CreateStringLiteral(runtimeContextId)),
                SyntaxFactory.Argument(
                    SyntaxFactory.DeclarationExpression(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.SingleVariableDesignation(
                            SyntaxFactory.Identifier("descSourceRuntimeSettings"))))
                .WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))
            ])));

        return SyntaxFactory.ConditionalExpression(
            tryGetValue,
            SyntaxFactory.IdentifierName("descSourceRuntimeSettings"),
            SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName("Dictionary<string, string>"))
                .WithArgumentList(SyntaxFactory.ArgumentList()));
    }

    private static ConditionalExpressionSyntax CreateDescSourceRuntimeSettingDescriptionsExpression(string runtimeContextId)
    {
        var tryGetValue = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("SourceRuntimeSettingDescriptionsBySourceContextId"),
                    SyntaxFactory.IdentifierName("TryGetValue")))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                SyntaxFactory.Argument(CreateStringLiteral(runtimeContextId)),
                SyntaxFactory.Argument(
                    SyntaxFactory.DeclarationExpression(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.SingleVariableDesignation(
                            SyntaxFactory.Identifier("descSourceRuntimeSettingDescriptions"))))
                .WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))
            ])));

        return SyntaxFactory.ConditionalExpression(
            tryGetValue,
            SyntaxFactory.IdentifierName("descSourceRuntimeSettingDescriptions"),
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Array"),
                        SyntaxFactory.GenericName(SyntaxFactory.Identifier("Empty"))
                            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                    SyntaxFactory.IdentifierName("SourceRuntimeSettingDescription"))))))
                .WithArgumentList(SyntaxFactory.ArgumentList()));
    }

    private ExpressionSyntax CreateDescArgumentsExpression(ExecutionReturnDesc desc, ExecutionRenderContext context)
    {
        var arguments = desc.Arguments.Select(argument => RenderExpression(argument, context)).ToArray();
        return arguments.Length == 0
            ? SyntaxHelper.ArrayEmptyOf("object")
            : CreateArrayCreation("object", arguments);
    }

    private InvocationExpressionSyntax CreateDescReturnExpression(ExecutionReturnDesc desc, ExecutionRenderContext context)
    {
        return desc.Type switch
        {
            DescType.Schema => CreateEvaluationHelperInvocation(
                nameof(EvaluationHelper.GetSpecificSchemaDescriptions),
                SyntaxFactory.IdentifierName(DescSchemaVariableName),
                SyntaxFactory.IdentifierName(DescRuntimeContextVariableName)),
            DescType.Constructors => CreateEvaluationHelperInvocation(
                nameof(EvaluationHelper.GetConstructorsForSpecificMethod),
                SyntaxFactory.IdentifierName(DescSchemaVariableName),
                CreateStringLiteral(desc.MethodName),
                SyntaxFactory.IdentifierName(DescRuntimeContextVariableName)),
            DescType.Functions => CreateEvaluationHelperInvocation(
                nameof(EvaluationHelper.GetMethodsForSchema),
                SyntaxFactory.IdentifierName(DescSchemaVariableName),
                SyntaxFactory.IdentifierName(DescRuntimeContextVariableName)),
            DescType.Table => CreateEvaluationHelperInvocation(
                nameof(EvaluationHelper.GetSpecificTableDescription),
                SyntaxFactory.IdentifierName(DescSchemaTableVariableName)),
            DescType.Column => CreateEvaluationHelperInvocation(
                nameof(EvaluationHelper.GetSpecificColumnDescription),
                SyntaxFactory.IdentifierName(DescSchemaTableVariableName),
                CreateStringLiteral(desc.Column!)),
            DescType.Settings => CreateEvaluationHelperInvocation(
                nameof(EvaluationHelper.GetSourceRuntimeSettingsDescription),
                CreateDescSourceRuntimeSettingDescriptionsExpression(desc.RuntimeContextId),
                SyntaxFactory.IdentifierName("token")),
            DescType.Query => CreateEvaluationHelperInvocation(
                nameof(EvaluationHelper.GetQueryDescription),
                CreateDescQueryColumnsExpression(desc, context)),
            _ => throw UnsupportedShape.Of($"DESC type {desc.Type}")
        };
    }

    private ExpressionSyntax CreateDescQueryColumnsExpression(
        ExecutionReturnDesc desc,
        ExecutionRenderContext context)
    {
        if (desc.QueryColumnMetadata == null)
            throw new ArgumentException("DESC QUERY execution rendering requires query column metadata.", nameof(desc));

        if (!TryGetStaticMetadataFieldName(desc.QueryColumnMetadata, context, out var fieldName))
            throw new InvalidOperationException("DESC QUERY static column metadata was not registered.");

        return SyntaxFactory.IdentifierName(fieldName);
    }

    private static InvocationExpressionSyntax CreateEvaluationHelperInvocation(
        string methodName,
        params ExpressionSyntax[] arguments)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.IdentifierName(methodName)))
            .WithArgumentList(CreateArgumentList(arguments));
    }

    private static void ValidateDesc(ExecutionReturnDesc desc)
    {
        if (desc.Type == DescType.Query)
        {
            if (desc.QueryColumnMetadata == null)
                throw new ArgumentException("DESC QUERY execution rendering requires query column metadata.", nameof(desc));

            return;
        }

        if (string.IsNullOrWhiteSpace(desc.SchemaName))
            throw new ArgumentException("DESC execution rendering requires a schema name.", nameof(desc));

        if (!RequiresMethodName(desc.Type))
            return;

        if (string.IsNullOrWhiteSpace(desc.MethodName))
            throw new ArgumentException($"DESC execution rendering requires a method name for {desc.Type}.", nameof(desc));

        if (desc.Type == DescType.Column && string.IsNullOrWhiteSpace(desc.Column))
            throw new ArgumentException("DESC column execution rendering requires a column path.", nameof(desc));
    }

    private static bool RequiresMethodName(DescType descType)
    {
        return descType is DescType.Constructors or DescType.Table or DescType.Column or DescType.Settings;
    }

    private static bool RequiresSchemaTable(DescType descType)
    {
        return descType is DescType.Table or DescType.Column;
    }
}
