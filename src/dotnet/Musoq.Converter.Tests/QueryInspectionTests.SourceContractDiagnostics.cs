using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenSourceWarnsUnsupportedEncodingModifier_ShouldReportSourceContractWarning()
    {
        var provider = new ContractDiagnosticSchemaProvider(ContractDiagnosticMode.WarnUnsupportedEncoding);
        var query = CreateContractQuery("Name: string encoding 'windows-1250'");
        var inspection = Inspect(query, provider);

        var warning = inspection.Warnings.Single(item => item.Code == DiagnosticCode.MQ5013_SourceContractWarning);
        var expectedSpan = CreateExpectedSpan(query, "encoding 'windows-1250'");

        Assert.Contains("Encoding modifier 'windows-1250' is ignored by the source.", warning.Message);
        Assert.Contains("sourceCode=UnsupportedEncoding", warning.Message);
        Assert.Contains("column=Name", warning.Message);
        Assert.Contains("modifier=encoding", warning.Message);
        Assert.AreEqual(expectedSpan, warning.Span);
        Assert.Contains("source contract diagnostic [DescribeSource]: Warning UnsupportedEncoding", inspection.PlanningText);
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenSourceSupportsOnlyUtf8Encoding_ShouldReportSourceContractError()
    {
        var provider = new ContractDiagnosticSchemaProvider(ContractDiagnosticMode.RequireUtf8Encoding);
        var result = InstanceCreator.CompileWithDiagnostics(
            CreateContractQuery("Name: string encoding 'windows-1250'"),
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver);

        var error = result.Errors.Single(item => item.Code == DiagnosticCode.MQ3071_SourceContractError);

        Assert.IsFalse(result.Succeeded);
        Assert.Contains("Only utf-8 encoding is supported, but column 'Name' requested 'windows-1250'.", error.Message);
        Assert.Contains("origin=TryPlanSource", error.Message);
        Assert.Contains("sourceCode=UnsupportedEncoding", error.Message);
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenDeclaredTableTypeConflictsWithSourceKind_ShouldReportSourceContractError()
    {
        var provider = new ContractDiagnosticSchemaProvider(ContractDiagnosticMode.RequireAmountString);
        var query = CreateContractQuery("Amount: decimal", "Amount");
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver);

        var error = result.Errors.Single(item => item.Code == DiagnosticCode.MQ3071_SourceContractError);
        var expectedSpan = CreateExpectedSpan(query, "Amount: decimal");

        Assert.IsFalse(result.Succeeded);
        Assert.Contains("Source column 'Amount' is text, but the table contract declares Decimal.", error.Message);
        Assert.Contains("origin=DescribeSource", error.Message);
        Assert.Contains("column=Amount", error.Message);
        Assert.AreEqual(expectedSpan, error.Span);
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenModifiersAreAbsent_ShouldNotReportSourceContractDiagnostic()
    {
        var provider = new ContractDiagnosticSchemaProvider(ContractDiagnosticMode.WarnUnsupportedEncoding);
        var result = InstanceCreator.CompileWithDiagnostics(
            CreateContractQuery("Name: string"),
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver);

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(result.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5013_SourceContractWarning));
        Assert.IsFalse(result.Errors.Any(static item => item.Code == DiagnosticCode.MQ3071_SourceContractError));
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenSourceContractDiagnosticHasNoTableOrigin_ShouldReportUsefulMessageWithEmptySpan()
    {
        var provider = new ContractDiagnosticSchemaProvider(ContractDiagnosticMode.GenericWarning);
        var result = InstanceCreator.CompileWithDiagnostics(
            "select 1 from #contract.items()",
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver);

        var warning = result.Warnings.Single(item => item.Code == DiagnosticCode.MQ5013_SourceContractWarning);

        Assert.IsTrue(result.Succeeded);
        Assert.Contains("The source reported a contract diagnostic without table-origin metadata.", warning.Message);
        Assert.Contains("sourceCode=GenericContractWarning", warning.Message);
        Assert.AreEqual(TextSpan.Empty, warning.Span);
    }

    private static string CreateContractQuery(string columns, string projection = "Name")
    {
        return
            $"table LegacyRecord {{ Id: int, {columns} }};" +
            "couple #contract.items with table LegacyRecord as Records;" +
            $"select {projection} from Records()";
    }

    private static TextSpan CreateExpectedSpan(string query, string text)
    {
        var start = query.IndexOf(text, StringComparison.Ordinal);
        Assert.AreNotEqual(-1, start);
        return new TextSpan(start, text.Length);
    }

    private enum ContractDiagnosticMode
    {
        WarnUnsupportedEncoding,
        RequireUtf8Encoding,
        RequireAmountString,
        GenericWarning
    }

    private sealed class ContractDiagnosticSchemaProvider(ContractDiagnosticMode mode) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!string.Equals(schema, "contract", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(schema, "#contract", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(schema);
            }

            return new ContractDiagnosticSchema(mode);
        }
    }

    private sealed class ContractDiagnosticSchema(ContractDiagnosticMode mode)
        : SchemaBase("contract", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (!string.Equals(name, "items", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException(name);

            return new ContractDiagnosticTable(metadataContext.AllColumns.ToArray());
        }

        public override SourceDescriptor DescribeSource(
            string name,
            SourceDescribeContext context,
            params object?[] parameters)
        {
            var descriptor = base.DescribeSource(name, context, parameters);
            var diagnostics = CreateDescriptorDiagnostics(context.MetadataContext.AllColumns);
            return diagnostics.Length == 0
                ? descriptor
                : descriptor with { ContractDiagnostics = descriptor.ContractDiagnostics.Concat(diagnostics).ToArray() };
        }

        public override SourcePlanResult TryPlanSource(
            string name,
            SourcePlanRequest request,
            params object?[] parameters)
        {
            var result = SourcePlanResult.AcceptAll(request);
            var diagnostics = CreatePlanDiagnostics(request.RequiredColumns);
            return diagnostics.Length == 0
                ? result
                : result with { ContractDiagnostics = diagnostics };
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            return EnsureSourceType<T, IReadOnlyDictionary<string, object?>>(
                name,
                new EmptyContractRowSource());
        }

        private SourceContractDiagnostic[] CreateDescriptorDiagnostics(IReadOnlyCollection<ISchemaColumn> columns)
        {
            if (mode == ContractDiagnosticMode.GenericWarning)
            {
                return
                [
                    SourceContractDiagnostic.Warning(
                        "The source reported a contract diagnostic without table-origin metadata.",
                        "GenericContractWarning")
                ];
            }

            if (mode == ContractDiagnosticMode.WarnUnsupportedEncoding &&
                TryFindEncodingColumn(columns, out var warningColumn, out var warningEncoding))
            {
                return
                [
                    SourceContractDiagnostic.Warning(
                        $"Encoding modifier '{warningEncoding}' is ignored by the source.",
                        "UnsupportedEncoding") with
                    {
                        ColumnName = warningColumn.ColumnName,
                        ModifierKey = ColumnReadModifiers.Encoding
                    }
                ];
            }

            if (mode == ContractDiagnosticMode.RequireAmountString &&
                columns.SingleOrDefault(static column => column.ColumnName == "Amount") is { } amountColumn &&
                amountColumn.ColumnType != typeof(string))
            {
                var declaredType = Nullable.GetUnderlyingType(amountColumn.ColumnType) ?? amountColumn.ColumnType;
                return
                [
                    SourceContractDiagnostic.Error(
                        $"Source column 'Amount' is text, but the table contract declares {declaredType.Name}.",
                        "ColumnKindMismatch") with
                    {
                        ColumnName = amountColumn.ColumnName
                    }
                ];
            }

            return [];
        }

        private SourceContractDiagnostic[] CreatePlanDiagnostics(IReadOnlyList<SourceColumnRef> requiredColumns)
        {
            if (mode != ContractDiagnosticMode.RequireUtf8Encoding ||
                !TryFindEncodingColumn(requiredColumns, out var column, out var encoding) ||
                string.Equals(encoding, "utf-8", StringComparison.OrdinalIgnoreCase))
            {
                return [];
            }

            return
            [
                SourceContractDiagnostic.Error(
                    $"Only utf-8 encoding is supported, but column '{column.Name}' requested '{encoding}'.",
                    "UnsupportedEncoding") with
                {
                    ColumnName = column.Name,
                    ModifierKey = ColumnReadModifiers.Encoding
                }
            ];
        }

        private static bool TryFindEncodingColumn(
            IEnumerable<ISchemaColumn> columns,
            out ISchemaColumn column,
            out string encoding)
        {
            foreach (var candidate in columns)
            {
                if (candidate.ReadModifiers.TryGetValue(ColumnReadModifiers.Encoding, out encoding!))
                {
                    column = candidate;
                    return true;
                }
            }

            column = null!;
            encoding = string.Empty;
            return false;
        }

        private static bool TryFindEncodingColumn(
            IEnumerable<SourceColumnRef> columns,
            out SourceColumnRef column,
            out string encoding)
        {
            foreach (var candidate in columns)
            {
                if (candidate.ReadModifiers.TryGetValue(ColumnReadModifiers.Encoding, out encoding!))
                {
                    column = candidate;
                    return true;
                }
            }

            column = null!;
            encoding = string.Empty;
            return false;
        }

        private static MethodsAggregator CreateLibrary()
        {
            return new MethodsAggregator(new MethodsManager());
        }
    }

    private sealed class ContractDiagnosticTable(ISchemaColumn[] columns) : ISchemaTable
    {
        public ISchemaColumn[] Columns => columns;

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns
                .Where(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        public SchemaTableMetadata Metadata { get; } = new(typeof(IReadOnlyDictionary<string, object?>));
    }

    private sealed class EmptyContractRowSource : RowSourceBase<IReadOnlyDictionary<string, object?>>
    {
        protected override void CollectChunks(IChunkWriter<IReadOnlyDictionary<string, object?>> writer)
        {
            writer.Write([]);
        }
    }
}
