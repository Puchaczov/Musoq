using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private List<StatementSyntax> RenderSourceScan(
        ExecutionSourceScan sourceScan,
        ExecutionRenderContext context)
    {
        ValidateSourceScan(sourceScan);

        var localName = CreateSourceScanLocalName(sourceScan);
        var infoTableName = localName.ToInfoTable();
        var schemaName = CreateSchemaVariableName(localName);
        var arguments = sourceScan.Binding.Arguments.Select(argument => RenderExpression(argument, context)).ToArray();
        var metadata = ResolveSourceSchemaColumnMetadata(sourceScan);
        var infoTableExpressionName = TryGetStaticMetadataFieldName(metadata, context, out var fieldName)
            ? fieldName
            : infoTableName;
        var statements = new List<StatementSyntax>();

        if (fieldName == null)
        {
            statements.Add(SyntaxFactory.LocalDeclarationStatement(
                SchemaNodeEmitter.CreateTableInfoDeclaration(infoTableName, CreateSchemaColumns(sourceScan.Binding.Fields))));
        }

        statements.Add(CreateSchemaDeclaration(schemaName, sourceScan.Binding.SchemaName));
        statements.AddRange(CreateRowSourceDeclarations(sourceScan, schemaName, infoTableExpressionName, arguments));

        return statements;
    }
}
