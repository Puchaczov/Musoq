using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(CreateTableNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (_sourceBinding.ExplicitlyDefinedTables.Keys.Any(
                existingName => string.Equals(existingName, node.Name, StringComparison.Ordinal)))
            TryReportInvalidSchemaDefinition(
                $"TABLE '{node.Name}' is already defined in this query batch. Table names must be unique.",
                node.SpanOrEmpty());

        if (node.Columns.Count == 0)
        {
            if (TryReportInvalidSchemaDefinition(
                    $"TABLE '{node.Name}' must declare at least one column.",
                    node.SpanOrEmpty()))
            {
                PushSemanticNode(((CreateTableNode)new CreateTableNode(node.Name, node.Columns))
                    .WithSpan(node.Span)
                    .WithFullSpan(node.FullSpan));
                return;
            }
        }

        var columnNames = new HashSet<string>(StringComparer.Ordinal);
        var tableColumns = new List<ISchemaColumn>();

        for (var i = 0; i < node.Columns.Count; i++)
        {
            var column = node.Columns[i];
            if (!columnNames.Add(column.ColumnName))
            {
                var duplicateSpan = column.ColumnNameSpan.IsEmpty ? node.SpanOrEmpty() : column.ColumnNameSpan;
                if (TryReportInvalidSchemaDefinition(
                        $"TABLE '{node.Name}' declares duplicate column '{column.ColumnName}'. Column names must be unique.",
                        duplicateSpan))
                    continue;
            }

            // TABLE nullability is a schema contract marker. Reference types are
            // already nullable in the CLR, so do not turn `string?` into the
            // impossible CLR type Nullable<string> while resolving the column.
            var declaredTypeName = column.TypeName.EndsWith("?", StringComparison.Ordinal)
                ? column.TypeName[..^1]
                : column.TypeName;

            if (_enumBinding.QueryLocalTypes.TryGetValue(declaredTypeName, out var queryLocalEnum))
            {
                var carrierType = EnumScalarTypeFacts.GetCarrierType(queryLocalEnum.UnderlyingKind);
                tableColumns.Add(CreateLogicalEnumSchemaColumn(
                    column,
                    i,
                    BuildMetadataAndInferTypesVisitorUtilities.MakeTypeNullable(carrierType),
                    queryLocalEnum));
                continue;
            }

            var remappedType = EvaluationHelper.RemapPrimitiveTypes(declaredTypeName);
            var type = EvaluationHelper.RemapPrimitiveTypeAsNullable(remappedType);

            if (type == null && TryResolveExactNativeEnum(declaredTypeName, out var nativeEnumType))
                type = BuildMetadataAndInferTypesVisitorUtilities.MakeTypeNullable(nativeEnumType);

            if (type == null)
            {
                var typeSpan = column.Span.IsEmpty ? node.SpanOrEmpty() : column.Span;
                if (TryReportTypeNotFound(remappedType, typeSpan))
                    continue;
                throw new TypeNotFoundException(remappedType, string.Empty, typeSpan);
            }

            tableColumns.Add(CreateSchemaColumn(column, i, type));
        }

        var table = new DynamicTable(tableColumns.ToArray(), caseSensitive: true);
        if (!_sourceBinding.ExplicitlyDefinedTables.ContainsKey(node.Name))
            _sourceBinding.ExplicitlyDefinedTables.Add(node.Name, table);
        _sourceBinding.ExplicitlyDefinedTableDiagnosticLocations[node.Name] =
            SourceContractDiagnosticLocationMap.FromTable(node);

        PushSemanticNode(((CreateTableNode)new CreateTableNode(node.Name, node.Columns))
            .WithSpan(node.Span)
            .WithFullSpan(node.FullSpan));
    }

    private bool TryResolveExactNativeEnum(string typeName, out Type enumType)
    {
        enumType = null!;
        if (!typeName.Contains('.', StringComparison.Ordinal))
            return false;

        var assemblies = new List<System.Reflection.Assembly> { _provider.GetType().Assembly };
        foreach (var assembly in _methodResolution.Assemblies)
            if (!assemblies.Contains(assembly))
                assemblies.Add(assembly);

        foreach (var assembly in assemblies)
        {
            var candidate = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
            if (candidate is not { IsEnum: true })
                continue;

            enumType = candidate;
            AddAssembly(assembly);
            return true;
        }

        return false;
    }

    private bool TryReportInvalidSchemaDefinition(string message, TextSpan span)
    {
        var exception = new VisitorException(
            nameof(BuildMetadataAndInferTypesVisitor),
            "ValidateTableDefinition",
            message,
            DiagnosticCode.MQ2012_InvalidSchemaDefinition,
            span);

        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportException(exception, span);
            return true;
        }

        throw exception;
    }
}
