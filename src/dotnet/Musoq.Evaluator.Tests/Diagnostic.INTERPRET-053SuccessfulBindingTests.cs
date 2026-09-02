using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticInterpret053SuccessfulBindingTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    public void Interpret_BinaryScalar_ShouldBindProjectedFieldsWithGeneratedTypes()
    {
        const string query = @"
            binary Packet {
                Magic: int le,
                Version: byte,
                Temperature: short be
            };
            select
                p.Magic,
                p.Version,
                p.Temperature
            from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var entities = new[]
        {
            new BinaryEntity
            {
                Name = "packet.bin",
                Content = [0x78, 0x56, 0x34, 0x12, 0x07, 0xED, 0xCC]
            }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { ["#test"] = entities });

        var table = CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver,
                TestCompilationOptions)
            .Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Magic", typeof(int)),
            ("p.Version", typeof(byte)),
            ("p.Temperature", typeof(short)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [0x12345678, (byte)7, (short)-4660]);
    }

    [TestMethod]
    public void Parse_TextScalar_ShouldBindProjectedFieldsAndMultipleRows()
    {
        const string query = @"
            text LogEntry {
                Date: until ' ',
                Level: until ' ',
                Message: rest
            };
            select
                log.Date,
                log.Level,
                log.Message
            from #test.lines() f
            cross apply Parse<LogEntry>(f.Text) log";

        var entities = new[]
        {
            new TextEntity { Name = "app.log", Text = "2026-08-31 INFO started" },
            new TextEntity { Name = "app.log", Text = "2026-08-31 WARN delayed" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { ["#test"] = entities });

        var table = CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver,
                TestCompilationOptions)
            .Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("log.Date", typeof(string)),
            ("log.Level", typeof(string)),
            ("log.Message", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["2026-08-31", "INFO", "started"],
            ["2026-08-31", "WARN", "delayed"]);
    }

    [TestMethod]
    public void Interpret_MultipleSchemaTypes_ShouldDispatchEachGeneratedInterpreter()
    {
        const string query = @"
            binary Header {
                Magic: int le
            };
            binary VersionInfo {
                Magic: int le,
                Version: byte
            };
            select
                h.Magic,
                v.Magic,
                v.Version
            from #test.files() f
            cross apply Interpret<Header>(f.Content) h
            cross apply Interpret<VersionInfo>(f.Content) v";

        var entities = new[]
        {
            new BinaryEntity
            {
                Name = "header.bin",
                Content = [0x78, 0x56, 0x34, 0x12, 0x09]
            }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { ["#test"] = entities });

        var table = CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver,
                TestCompilationOptions)
            .Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("h.Magic", typeof(int)),
            ("v.Magic", typeof(int)),
            ("v.Version", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [0x12345678, 0x12345678, (byte)9]);
    }

    [TestMethod]
    public void Interpret_UnknownSchema_ShouldReportStructuredSchemaDiagnostic()
    {
        const string query = @"
            select p.Value
            from #test.files() f
            cross apply Interpret<MissingPacket>(f.Content) p";

        var entities = new[]
        {
            new BinaryEntity { Name = "packet.bin", Content = [0, 0, 0, 0] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { ["#test"] = entities });

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver,
                TestCompilationOptions));

        AssertErrorEnvelope(
            exception,
            DiagnosticCode.MQ3010_UnknownSchema,
            DiagnosticPhase.Bind,
            "MissingPacket");
        AssertHasGuidance(exception);
    }
}
