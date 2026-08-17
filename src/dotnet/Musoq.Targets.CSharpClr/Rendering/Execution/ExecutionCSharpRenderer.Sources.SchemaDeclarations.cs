using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
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
        ExpressionSyntax[] arguments)
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

        statements.Add(CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), rowSourceName, invocation));
        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            sourceScan.Rows.Name,
            IsInstrumentationEnabled
                ? CreateProfiledChunksExpression(rows, sourceProfileName, CreateTypeSyntax(sourceType))
                : rows));

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

}
