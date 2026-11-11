using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tests.Schema.QueryRows;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

public sealed partial class DiagnosticREC103NativeEnumSourcePlanningTests
{
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0080")]
    public void NativeEnumTable_ShouldRejectNativeClrDeclaration()
    {
        const string query =
            "table Jobs { Id: int, Status: Musoq.Evaluator.Tests.REC103NativeStatus, Access: uint }; couple #queryrowsample.rows with table Jobs as Jobs; select Status from Jobs()";
        AssertNativeTableRejection(
            query,
            new GeneratedQueryRowSampleSchemaProvider(GeneratedQueryRowSampleShape.Enum, true),
            $"Status: {NativeStatusTypeName}",
            typeof(REC103NativeStatus)
        );
    }

    [TestMethod]
    [TestProperty("CandidateId", "PROMOTED-NativeEnumTableReferenceProjectsPrimitiveCarrierAndDescriptor")]
    public void NativeEnumTableReference_ShouldRejectBeforeOpeningSources()
    {
        const string query =
            "table Jobs { Status: Musoq.Evaluator.Tests.REC103NativeStatus }; couple #specstressnative.rows with table Jobs as Jobs; select Status from Jobs()";
        var sourceOpened = false;
        AssertNativeTableRejection(
            query,
            new NativeTableCarrierSchemaProvider(sourceOpened: () => sourceOpened = true),
            $"Status: {NativeStatusTypeName}",
            typeof(REC103NativeStatus)
        );
        Assert.IsFalse(sourceOpened);
    }

    [TestMethod]
    [DataRow("", true)]
    [DataRow("?", true)]
    [DataRow("", false)]
    [DataRow("?", false)]
    public void NativeEnumTable_UnselectedColumnShouldRejectRegardlessOfSourceCapabilities(
        string nullableSuffix,
        bool queryScopedRowsEnabled
    )
    {
        var declaration = $"Status: {NativeStatusTypeName}{nullableSuffix}";
        var query =
            $"table Jobs {{ Id: int, {declaration} }}; "
            + "couple #queryrowsample.rows with table Jobs as Jobs; select Id from Jobs()";
        AssertNativeTableRejection(
            query,
            new GeneratedQueryRowSampleSchemaProvider(GeneratedQueryRowSampleShape.Enum, queryScopedRowsEnabled),
            declaration,
            typeof(REC103NativeStatus)
        );
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("?")]
    public void NativeEnumTable_FrameworkEnumShouldReject(string nullableSuffix)
    {
        var declaration = $"Status: System.DayOfWeek{nullableSuffix}";
        var query =
            $"table Jobs {{ {declaration} }}; "
            + "couple #specstressnative.rows with table Jobs as Jobs; select Status from Jobs()";
        var sourceOpened = false;
        AssertNativeTableRejection(
            query,
            new NativeTableCarrierSchemaProvider(sourceOpened: () => sourceOpened = true),
            declaration,
            typeof(DayOfWeek)
        );
        Assert.IsFalse(sourceOpened);
    }

    private void AssertNativeTableRejection(
        string query,
        ISchemaProvider provider,
        string columnDeclaration,
        Type enumType
    )
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            TestCompilationOptions
        );
        try
        {
            Assert.IsFalse(result.Succeeded, FormatDiagnostics(result));
            Assert.HasCount(1, result.Errors, FormatDiagnostics(result));
            var diagnostic = result.Errors.Single();
            Assert.AreEqual(DiagnosticCode.MQ2012_InvalidSchemaDefinition, diagnostic.Code);
            Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
            Assert.AreEqual(
                new TextSpan(query.IndexOf(columnDeclaration, StringComparison.Ordinal), columnDeclaration.Length),
                diagnostic.Span
            );
            var backingType = EvaluationHelper.GetCastableType(Enum.GetUnderlyingType(enumType));
            var columnName = columnDeclaration[..columnDeclaration.IndexOf(':')];
            Assert.Contains(
                $"TABLE 'Jobs' column '{columnName}' cannot declare native CLR enum type '{enumType.FullName}'. "
                    + $"Declare its underlying primitive type '{backingType}', or use a query-local ENUM.",
                diagnostic.Message
            );
        }
        finally
        {
            result.CompiledQuery?.Dispose();
        }
    }
}
