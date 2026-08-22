using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static LocalDeclarationStatementSyntax CreateSchemaDeclaration(
        string schemaVariableName,
        string schemaName)
    {
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("provider"),
                    SyntaxFactory.IdentifierName("GetSchema")))
            .WithArgumentList(CreateArgumentList(CreateStringLiteral(schemaName)));

        return CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), schemaVariableName, invocation);
    }

    private IReadOnlyList<StatementSyntax> CreateRowSourceDeclarations(
        ExecutionSourceScan sourceScan,
        string schemaVariableName,
        string infoTableName,
        ExpressionSyntax[] arguments,
        ExecutionRenderContext context)
    {
        ExpressionSyntax argsExpression = arguments.Length == 0
            ? SyntaxHelper.ArrayEmptyOf("object")
            : CreateArrayCreation("object", arguments);
        var statements = new List<StatementSyntax>();
        var sourceProfileName = CreateSourceProfileRecorderVariableName(sourceScan.Rows.Name);

        if (IsInstrumentationEnabled)
        {
            statements.Add(CreateSourceProfileRecorderDeclaration(
                sourceProfileName,
                sourceScan.Source.Name,
                _instrumentationMode == QueryInstrumentationMode.SourceBoundaries));
        }

        var runtimeContext = SchemaNodeEmitter.CreateRuntimeContext(
            sourceScan.Binding.RuntimeContextId,
            sourceScan.Binding.SchemaFromIndex,
            SyntaxFactory.IdentifierName(infoTableName),
            IsInstrumentationEnabled ? CreateSourceDiagnosticsExpression(sourceProfileName) : null);

        if (sourceScan.Binding.QueryRowSourceTransfer is { } queryRowTransfer)
        {
            var carrierTypeName = QueryRowSourceNaming.CreateCarrierTypeName(
                queryRowTransfer.ShapeFingerprint,
                queryRowTransfer.Carrier);
            var materializerTypeName = QueryRowSourceNaming.CreateMaterializerTypeName(
                queryRowTransfer.ShapeFingerprint,
                queryRowTransfer.Carrier);
            var querySchemaVariableName = $"{schemaVariableName}QueryRows";
            var querySchemaAsExpression = SyntaxFactory.BinaryExpression(
                SyntaxKind.AsExpression,
                SyntaxFactory.IdentifierName(schemaVariableName),
                CreateTypeSyntax(typeof(IQueryScopedRowSourceSchema)));
            var querySchemaGuard = SyntaxFactory.BinaryExpression(
                SyntaxKind.CoalesceExpression,
                querySchemaAsExpression,
                SyntaxFactory.ThrowExpression(CreateObjectCreation(
                    nameof(InvalidOperationException),
                    CreateStringLiteral(
                        $"Source '{sourceScan.Binding.SchemaName}.{sourceScan.Binding.MethodName}' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape {queryRowTransfer.ShapeFingerprint})."))));
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                querySchemaVariableName,
                querySchemaGuard));

            var getQueryRowSourceName = SyntaxFactory.GenericName("GetQueryScopedRowSource")
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList<TypeSyntax>(new TypeSyntax[]
                    {
                        SyntaxFactory.ParseTypeName(carrierTypeName),
                        SyntaxFactory.ParseTypeName(materializerTypeName)
                    })));
            var queryInvocation = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(querySchemaVariableName),
                        getQueryRowSourceName))
                .WithArgumentList(CreateArgumentList(
                    CreateStringLiteral(sourceScan.Binding.MethodName),
                    CreateObjectCreation(
                        nameof(QueryScopedRowSourceRequest),
                        runtimeContext,
                        SyntaxFactory.IdentifierName(
                            QueryRowSourceNaming.CreateShapeFieldName(queryRowTransfer.ShapeFingerprint))),
                    argsExpression));
            var queryRowSourceName = CreateRowSourceVariableName(sourceScan.Rows.Name);
            var queryRows = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(queryRowSourceName),
                    SyntaxFactory.IdentifierName("Chunks"));
            var carrierType = SyntaxFactory.ParseTypeName(carrierTypeName);

            statements.Add(CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), queryRowSourceName, queryInvocation));
            var queryProgressRows = CreateProgressChunksExpression(
                queryRows,
                carrierType,
                sourceScan.Binding.RuntimeContextId,
                context);
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                sourceScan.Rows.Name,
                IsInstrumentationEnabled
                    ? CreateProfiledChunksExpression(queryProgressRows, sourceProfileName, carrierType)
                    : queryProgressRows));

            return statements;
        }

        var sourceType = sourceScan.Binding.SourceType?.RequireClrType() ?? sourceScan.Source.Type.RequireClrType();
        if (!CanReferenceType(sourceType))
            throw new InvalidOperationException(
                $"Generated execution source '{sourceScan.Binding.MethodName}' has non-referenceable type '{sourceType.FullName ?? sourceType.Name}'.");

        var getRowSourceName = SyntaxFactory.GenericName("GetRowSource")
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SingletonSeparatedList(CreateTypeSyntax(sourceType))));
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(schemaVariableName),
                    getRowSourceName))
            .WithArgumentList(CreateArgumentList(
                CreateStringLiteral(sourceScan.Binding.MethodName),
                runtimeContext,
                argsExpression));
        var rowSourceName = CreateRowSourceVariableName(sourceScan.Rows.Name);
        var rows = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(rowSourceName),
                SyntaxFactory.IdentifierName("Chunks"));
        var progressRows = CreateProgressChunksExpression(
            rows,
            CreateTypeSyntax(sourceType),
            sourceScan.Binding.RuntimeContextId,
            context);

        statements.Add(CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), rowSourceName, invocation));
        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            sourceScan.Rows.Name,
            IsInstrumentationEnabled
                ? CreateProfiledChunksExpression(progressRows, sourceProfileName, CreateTypeSyntax(sourceType))
                : progressRows));

        return statements;
    }

    private static SchemaColumn[] CreateSchemaColumns(IEnumerable<FieldBinding> fields)
    {
        return fields
            .Select(field => new SchemaColumn(field.Name, field.OutputIndex, field.Type.RequireClrType(), field.ReadModifiers))
            .ToArray();
    }

    private static string CreateSchemaVariableName(string alias)
    {
        return $"__{SyntaxHelper.ToCamelCase(alias)}Schema";
    }

    private static string CreateRowSourceVariableName(string rowsVariableName)
    {
        return $"{rowsVariableName}Source";
    }

    private ExpressionSyntax CreateProgressChunksExpression(
        ExpressionSyntax chunks,
        TypeSyntax elementType,
        string sourceContextId,
        ExecutionRenderContext context)
    {
        var queryContext = SyntaxFactory.IdentifierName("__musoqProgressContext");
        var wrapMethod = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(nameof(QueryProgressRuntime)),
            SyntaxFactory.GenericName(nameof(QueryProgressRuntime.WrapChunks))
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(elementType))));
        var wrapped = SyntaxFactory.InvocationExpression(wrapMethod)
            .WithArgumentList(CreateArgumentList(
                chunks,
                queryContext,
                CreateStringLiteral(sourceContextId)));

        var progressCondition = SyntaxFactory.BinaryExpression(
            SyntaxKind.NotEqualsExpression,
            queryContext,
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

        return SyntaxFactory.ConditionalExpression(
            progressCondition,
            wrapped,
            chunks);
    }

}
