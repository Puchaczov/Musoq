using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

internal static class TableSourceContractBinding
{
    public static ISchemaTable Bind(ISchemaTable source, ISchemaTable? declaration)
    {
        if (declaration is not DeclaredTableSchema declared)
            return source;

        var columns = declared.Columns.Select(column => BindColumn(source, declared, column)).ToArray();
        return new CoupledTableSchema(source, columns);
    }

    private static ISchemaColumn BindColumn(ISchemaTable source, DeclaredTableSchema table, ISchemaColumn declared)
    {
        var syntax = table.Declaration.Columns.Single(column => column.ColumnName == declared.ColumnName);
        var matches = source.GetColumnsByName(declared.ColumnName);
        if (matches.Length == 0)
            throw Failure(table, syntax, DiagnosticCode.MQ3001_UnknownColumn,
                $"TABLE '{table.Declaration.Name}' declares column '{declared.ColumnName}', which the datasource does not expose. " +
                "Declare an existing datasource column.");
        if (matches.Length != 1)
            throw Failure(table, syntax, DiagnosticCode.MQ3002_AmbiguousColumn,
                $"TABLE '{table.Declaration.Name}' column '{declared.ColumnName}' matches multiple datasource columns. " +
                "Use an unambiguous datasource column name.");

        var provider = matches[0];
        var expectedType = declared.ColumnType;
        if (source.Metadata.TypeOrigin == TableSchemaTypeOrigin.ProviderDefined &&
            !syntax.TypeName.EndsWith("?", StringComparison.Ordinal))
            expectedType = Nullable.GetUnderlyingType(expectedType) ?? expectedType;

        if (provider.ColumnType != expectedType ||
            (source.Metadata.TypeOrigin == TableSchemaTypeOrigin.ProviderDefined &&
             provider.EnumType?.Fingerprint != declared.EnumType?.Fingerprint))
            throw Failure(table, syntax, DiagnosticCode.MQ3005_TypeMismatch,
                $"TABLE '{table.Declaration.Name}' column '{declared.ColumnName}' declares {DescribeType(expectedType, declared.EnumType)}, " +
                $"but the datasource exposes {DescribeType(provider.ColumnType, provider.EnumType)}. " +
                MatchingDeclarationSuggestion(provider));

        return provider;
    }

    private static string DescribeType(Type carrierType, EnumTypeDescriptor? enumType)
    {
        var typeName = EvaluationHelper.GetCastableType(carrierType);
        if (enumType is null)
            return typeName;

        return $"{typeName} (ENUM '{enumType.DisplayName}')";
    }

    private static string MatchingDeclarationSuggestion(ISchemaColumn column)
    {
        if (column.EnumType?.Origin == EnumTypeOrigin.NativeClr)
            return "Project the provider-native enum directly without a TABLE declaration; native CLR enum declarations in TABLE are prohibited.";

        if (column.EnumType is null)
            return $"Declare '{column.ColumnName}: {EvaluationHelper.GetCastableType(column.ColumnType)}'.";

        var typeName = column.EnumType.DisplayName;
        if (Nullable.GetUnderlyingType(column.ColumnType) is not null)
            typeName += "?";
        return $"Declare '{column.ColumnName}: {typeName}'. " +
               "Use a query-local ENUM with the identical backing type, flags contract, and members.";
    }

    private static VisitorException Failure(DeclaredTableSchema table, CreateTableColumnDefinition column,
        DiagnosticCode code, string message)
    {
        var span = column.Span;
        if (span.IsEmpty)
            span = table.Declaration.SpanOrEmpty();
        return new VisitorException(nameof(BuildMetadataAndInferTypesVisitor), "BindTableSourceContract", message, code, span);
    }
}
