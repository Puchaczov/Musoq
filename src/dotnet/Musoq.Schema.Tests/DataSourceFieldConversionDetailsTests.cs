using System;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.Diagnostics;
using Musoq.Schema.Exceptions;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class DataSourceFieldConversionDetailsTests
{
    [TestMethod]
    public void ReadFailure_WithTypedDetails_PreservesCodeAndExposesOnlySafeFacts()
    {
        var failure = CreateConversionFailure(2L, 3, typeof(long?));
        var wrapped = WrapRead(failure);
        var diagnostic = wrapped.ToDiagnostic();
        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic);
        var text = MusoqErrorEnvelopeFormatter.FormatText(envelope);
        var json = MusoqErrorEnvelopeFormatter.FormatJson(envelope);
        using var document = JsonDocument.Parse(json);
        var arguments = document.RootElement.GetProperty("arguments");

        Assert.AreSame(failure, wrapped.InnerException);
        Assert.AreEqual(DiagnosticCode.MQ7011_DataSourceReadFailed, diagnostic.Code);
        Assert.AreEqual(WrapRead(new FormatException()).Message, diagnostic.Message);
        Assert.AreEqual(DiagnosticPhase.DataSource, diagnostic.Phase);
        Assert.IsNull(diagnostic.InputLocation);
        Assert.AreEqual("2", arguments.GetProperty("recordNumber").GetString());
        Assert.AreEqual("3", arguments.GetProperty("columnOrdinal").GetString());
        Assert.AreEqual("Int64", arguments.GetProperty("expectedType").GetString());
        StringAssert.Contains(text, "expectedType: Int64");
        Assert.DoesNotContain("private-cell-value", text);
        Assert.DoesNotContain("private-cell-value", json);
    }

    [TestMethod]
    public void ReadFailure_WithoutValidDetails_KeepsItsOrdinaryDiagnostic()
    {
        var ordinary = WrapRead(new FormatException()).ToDiagnostic();
        var invalid = WrapRead(CreateConversionFailure(0L, 1, typeof(long))).ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ7011_DataSourceReadFailed, ordinary.Code);
        Assert.AreEqual(ordinary.Message, invalid.Message);
        Assert.IsFalse(ordinary.Arguments.ContainsKey("recordNumber"));
        CollectionAssert.AreEquivalent(ordinary.Arguments.ToArray(), invalid.Arguments.ToArray());
    }

    [TestMethod]
    public void ReadFailure_WithAmbiguousAggregate_DoesNotReportAnIndividualField()
    {
        var diagnostic = WrapRead(
                new AggregateException(CreateConversionFailure(2L, 3, typeof(long)), new FormatException())
            )
            .ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ7011_DataSourceReadFailed, diagnostic.Code);
        Assert.IsFalse(diagnostic.Arguments.ContainsKey("recordNumber"));
    }

    [TestMethod]
    public void ReadFailure_WithMalformedInput_KeepsItsExistingPrecedence()
    {
        var failure = new DataSourceInputException(
            DataSourceInputFailureReason.MalformedInput,
            new DataSourceInputLocation(line: 4),
            CreateConversionFailure(2L, 3, typeof(long))
        );
        var diagnostic = WrapRead(failure).ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ7014_MalformedRuntimeInput, diagnostic.Code);
        Assert.AreEqual(4L, diagnostic.InputLocation?.Line);
        Assert.IsFalse(diagnostic.Arguments.ContainsKey("recordNumber"));
    }

    private static FormatException CreateConversionFailure(long record, int column, Type expectedType)
    {
        var failure = new FormatException("private-cell-value");
        failure.Data[DataSourceFieldConversionDetails.RecordNumberKey] = record;
        failure.Data[DataSourceFieldConversionDetails.ColumnOrdinalKey] = column;
        failure.Data[DataSourceFieldConversionDetails.ExpectedTypeKey] = expectedType;
        failure.Data["recordText"] = "private-cell-value";
        return failure;
    }

    private static DataSourceLifecycleException WrapRead(Exception failure) =>
        DataSourceLifecycleException.ForRead("#input", "rows", "r", "r:1", failure);
}
