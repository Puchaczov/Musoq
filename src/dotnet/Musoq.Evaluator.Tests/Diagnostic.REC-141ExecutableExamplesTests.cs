using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.ReadModifiers;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.Optimization;
using RuntimeParseException = Musoq.Schema.Interpreters.ParseException;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC141ExecutableExamplesTests : BasicEntityTestBase
{
    [TestMethod]
    public void CoreAppendixExamples_ShouldExecuteDocumentedCaseCastAndGroupingResults()
    {
        var query = @"
            select
                case
                    when Population >= 1000 then 'large'
                    when Population >= 500 then 'medium'
                    when Population >= 100 then 'small'
                    else 'tiny'
                end as Category,
                Population::Int32 as PopulationInt,
                (Population + 1)::Decimal as PopulationPlusOne
            from #A.entities()
            order by Population desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity("large") { Population = 1000m },
                new BasicEntity("medium") { Population = 500m },
                new BasicEntity("small") { Population = 100m },
                new BasicEntity("tiny") { Population = 50m }
            ]
        };

        using var table = CreateAndRunVirtualMachine(query, sources).Run(TokenSource.Token);

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("large", table[0][0]);
        Assert.AreEqual(1000, table[0][1]);
        Assert.AreEqual(1001m, table[0][2]);
        Assert.AreEqual("medium", table[1][0]);
        Assert.AreEqual("small", table[2][0]);
        Assert.AreEqual("tiny", table[3][0]);
    }

    [TestMethod]
    public void CoreAppendixGroupingExamples_ShouldExecuteOrdinalAndAliasForms()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity("Warsaw") { City = "Warsaw" },
                new BasicEntity("Warsaw") { City = "Warsaw" },
                new BasicEntity("Krakow") { City = "Krakow" }
            ]
        };

        const string ordinalQuery =
            "select City, Count(*) from #A.entities() group by 1 order by City";
        using var ordinalTable = CreateAndRunVirtualMachine(ordinalQuery, sources).Run(TokenSource.Token);
        Assert.AreEqual(2, ordinalTable.Count);
        Assert.AreEqual("Krakow", ordinalTable[0][0]);
        Assert.AreEqual(1L, ordinalTable[0][1]);
        Assert.AreEqual("Warsaw", ordinalTable[1][0]);
        Assert.AreEqual(2L, ordinalTable[1][1]);

        const string aliasQuery =
            "select City as c, Count(*) from #A.entities() group by c order by c";
        using var aliasTable = CreateAndRunVirtualMachine(aliasQuery, sources).Run(TokenSource.Token);
        Assert.AreEqual(2, aliasTable.Count);
        Assert.AreEqual("Krakow", aliasTable[0][0]);
        Assert.AreEqual(1L, aliasTable[0][1]);
        Assert.AreEqual("Warsaw", aliasTable[1][0]);
        Assert.AreEqual(2L, aliasTable[1][1]);
    }

    [TestMethod]
    public void CoreAppendixStringExamples_ShouldPreserveRawAndDoubledPathValues()
    {
        var provider = CreateCoreProvider([new BasicEntity("fixture")]);
        var cases = new[]
        {
            (Query: @"select r'C:\new\test' from #A.entities()", Expected: @"C:\new\test"),
            (Query: @"select 'C:\\new\\test' from #A.entities()", Expected: @"C:\new\test")
        };

        foreach (var (query, expected) in cases)
        {
            var build = InstanceCreator.CompileWithDiagnostics(
                query,
                $"REC141_String_{Guid.NewGuid():N}",
                provider,
                LoggerResolver,
                TestCompilationOptions);
            Assert.IsTrue(build.Succeeded, Format(build.Diagnostics));
            Assert.IsNotNull(build.CompiledQuery);
            using var table = build.CompiledQuery!.Run(TokenSource.Token);
            Assert.AreEqual(expected, table[0][0], query);
        }
    }

    [TestMethod]
    public void CoreAppendixCaseNegative_ShouldAssertDiagnosticAndRunPrescribedElseCorrection()
    {
        var provider = CreateCoreProvider([new BasicEntity("fixture") { Population = 1m }]);
        const string invalidQuery =
            "select case when Population > 0 then 'positive' end from #A.entities()";
        var invalid = new QueryAnalyzer(provider).Analyze(invalidQuery);

        Assert.IsTrue(invalid.HasErrors, Format(invalid.Diagnostics));
        Assert.HasCount(1, invalid.Errors);
        var diagnostic = invalid.Errors.Single();
        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, diagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual("end", invalidQuery.Substring(diagnostic.Span.Start, diagnostic.Span.Length));

        const string correctedQuery =
            "select case when Population > 0 then 'positive' else 'non-positive' end from #A.entities()";
        using var table = CreateAndRunVirtualMachine(
            correctedQuery,
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [new BasicEntity("fixture") { Population = 1m }]
            }).Run(TokenSource.Token);
        Assert.AreEqual("positive", table[0][0]);
    }

    [TestMethod]
    public void TableCoupleExample_ShouldExecuteWithAnExplicitRepositoryFixtureMapping()
    {
        const string query = @"
            table DummyTable {
                Name: string
            };
            couple #A.Entities with table DummyTable as SourceOfDummyRows;
            select Name from SourceOfDummyRows();";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("Alice"), new BasicEntity("Bob")]
        };

        using var table = CreateAndRunVirtualMachine(query, sources).Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
        Assert.AreEqual("Bob", table[1][0]);
    }

    [TestMethod]
    public void RecoveryMetadataExample_ShouldMatchIndependentCatalogAndMissingProviderBoundary()
    {
        var provider = CreateCoreProvider([new BasicEntity("fixture")]);
        const string missingProviderQuery = "select Name from #missing.entities()";
        var analysis = new QueryAnalyzer(provider).Analyze(missingProviderQuery);

        Assert.IsTrue(analysis.HasErrors, Format(analysis.Diagnostics));
        Assert.HasCount(1, analysis.Errors);
        var diagnostic = analysis.Errors.Single();
        Assert.AreEqual(DiagnosticCode.MQ3010_UnknownSchema, diagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        StringAssert.Contains(diagnostic.Message, "missing");

        var catalogPath = Path.Combine(FindRepositoryRoot(), "specs", "diagnostic-catalog.json");
        using var catalog = JsonDocument.Parse(File.ReadAllText(catalogPath));
        var descriptor = catalog.RootElement
            .GetProperty("diagnostics")
            .EnumerateArray()
            .Single(item => item.GetProperty("code").GetString() == "MQ3010_UnknownSchema");
        Assert.AreEqual("Bind", descriptor.GetProperty("phase").GetString());
        Assert.AreEqual("Error", descriptor.GetProperty("severity").GetString());
        Assert.IsTrue(
            descriptor.GetProperty("suggestedFixes").EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .Any(item => item.Contains("provider", StringComparison.OrdinalIgnoreCase)),
            "The recovery metadata must retain the provider-registration correction.");

        using var corrected = CreateAndRunVirtualMachine(
            "select Name from #A.entities()",
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [new BasicEntity("fixture")]
            }).Run(TokenSource.Token);
        Assert.AreEqual("fixture", corrected[0][0]);
    }

    private static BasicSchemaProvider<BasicEntity> CreateCoreProvider(IEnumerable<BasicEntity> entities)
    {
        return new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = entities });
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "musoq-recovery-campaign.json")))
                return directory.FullName;

            directory = directory.Parent;
        }

        Assert.Fail("Could not locate the repository root from the test output directory.");
        return string.Empty;
    }

    private static string Format(IEnumerable<Diagnostic> diagnostics)
    {
        return string.Join(" | ", diagnostics);
    }
}

[TestClass]
public sealed class DiagnosticREC141TableExamplesTests : BasicEntityTestBase
{
    [TestMethod]
    public void TableTypedColumnsExample_ShouldExecuteWithAnExplicitFixtureMapping()
    {
        const string query = """
            table DataTable {
                Country: string,
                Population: decimal
            };
            couple #readmods.records with table DataTable as Countries;
            select Country, Population from Countries() where Population > 100;
            """;
        var provider = new ReadModifiersSchemaProvider(
        [
            Row(("Country", "Poland"), ("Population", "38000000")),
            Row(("Country", "Vatican"), ("Population", "800")),
            Row(("Country", "Nauru"), ("Population", "10"))
        ]);

        using var table = CreateAndRunVirtualMachine(query, schemaProvider: provider).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Poland", 38000000m],
            ["Vatican", 800m]);
    }

    [TestMethod]
    public void TableJoinExample_ShouldExecuteWithExplicitFixtureMappings()
    {
        const string query = """
            table FirstTable {
                Country: string,
                Population: decimal
            };
            table SecondTable {
                Name: string
            };
            couple #readmods.records with table FirstTable as Source1;
            couple #readmods.records with table SecondTable as Source2;

            select s1.Country, s2.Name
            from Source1() s1
            inner join Source2() s2 on s1.Country = s2.Name;
            """;
        var provider = new ReadModifiersSchemaProvider(
        [
            Row(("Country", "Poland"), ("Name", "Poland"), ("Population", "38000000")),
            Row(("Country", "Nauru"), ("Name", "Nauru"), ("Population", "10"))
        ]);

        using var table = CreateAndRunVirtualMachine(query, schemaProvider: provider).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Poland", "Poland"],
            ["Nauru", "Nauru"]);
    }

    [TestMethod]
    public void TableParametersExample_ShouldExecuteWithAnExplicitFixtureMapping()
    {
        const string query = """
            table Parameters {
                Parameter0: bool,
                Parameter1: string
            };
            couple #readmods.records with table Parameters as Config;
            select Parameter0, Parameter1 from Config(true, 'test');
            """;
        var provider = new ReadModifiersSchemaProvider(
        [Row(("Parameter0", true), ("Parameter1", "test"))]);

        using var table = CreateAndRunVirtualMachine(query, schemaProvider: provider).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [true, "test"]);
    }

    [TestMethod]
    public void TableAllTypesExample_ShouldExecuteWithAnExplicitFixtureMapping()
    {
        const string query = """
            table AllTypes {
                ByteCol: byte,
                SByteCol: sbyte,
                ShortCol: short,
                IntCol: int,
                LongCol: long,
                UShortCol: ushort,
                UIntCol: uint,
                ULongCol: ulong,
                FloatCol: float,
                DoubleCol: double,
                DecimalCol: decimal,
                MoneyCol: money,
                BoolCol: bool,
                CharCol: char,
                StringCol: string,
                DateTimeCol: datetime,
                DateTimeOffsetCol: datetimeoffset,
                TimeSpanCol: timespan,
                GuidCol: guid,
                ObjectCol: object
            };
            couple #readmods.records with table AllTypes as TypedData;
            select ByteCol, SByteCol, ShortCol, IntCol, LongCol, UShortCol, UIntCol, ULongCol, FloatCol, DoubleCol, DecimalCol, MoneyCol, BoolCol, CharCol, StringCol, DateTimeCol, DateTimeOffsetCol, TimeSpanCol, GuidCol, ObjectCol from TypedData();
            """;
        const string explicitProjection = "select ByteCol, SByteCol, ShortCol, IntCol, LongCol, UShortCol, UIntCol, ULongCol, FloatCol, DoubleCol, DecimalCol, MoneyCol, BoolCol, CharCol, StringCol, DateTimeCol, DateTimeOffsetCol, TimeSpanCol, GuidCol, ObjectCol from TypedData();";
        var dateTime = new DateTime(2026, 7, 17, 12, 0, 0);
        var dateTimeOffset = new DateTimeOffset(dateTime, TimeSpan.Zero);
        var guid = Guid.Parse("b7f0d48f-e2d0-4a8e-8c12-0e44cf3a3d0a");
        var timeSpan = TimeSpan.FromMinutes(5);
        var provider = new ReadModifiersSchemaProvider(
        [Row(
            ("ByteCol", (byte)1),
            ("SByteCol", (sbyte)-2),
            ("ShortCol", (short)-3),
            ("IntCol", 4),
            ("LongCol", 5L),
            ("UShortCol", (ushort)6),
            ("UIntCol", 7U),
            ("ULongCol", 8UL),
            ("FloatCol", 1.5f),
            ("DoubleCol", 2.5d),
            ("DecimalCol", 3.5m),
            ("MoneyCol", 4.5m),
            ("BoolCol", true),
            ("CharCol", 'Z'),
            ("StringCol", "text"),
            ("DateTimeCol", dateTime),
            ("DateTimeOffsetCol", dateTimeOffset),
            ("TimeSpanCol", timeSpan),
            ("GuidCol", guid),
            ("ObjectCol", "object")
        )]);

        var publishedQuery = query.Replace(explicitProjection, "select * from TypedData();", StringComparison.Ordinal);
        using var publishedTable = CreateAndRunVirtualMachine(
            publishedQuery,
            schemaProvider: provider).Run(TokenSource.Token);
        Assert.AreEqual(17, publishedTable.Columns.Count());
        Assert.IsFalse(publishedTable.Columns.Any(column =>
            column.ColumnName is "TimeSpanCol" or "GuidCol" or "ObjectCol"));

        using var table = CreateAndRunVirtualMachine(query, schemaProvider: provider).Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(20, table.Columns.Count(), string.Join(", ", table.Columns.Select(column => column.ColumnName)));
        var row = table[0];
        Assert.AreEqual((byte)1, row[0]);
        Assert.AreEqual((sbyte)-2, row[1]);
        Assert.AreEqual((short)-3, row[2]);
        Assert.AreEqual(4, row[3]);
        Assert.AreEqual(5L, row[4]);
        Assert.AreEqual((ushort)6, row[5]);
        Assert.AreEqual(7U, row[6]);
        Assert.AreEqual(8UL, row[7]);
        Assert.AreEqual(1.5f, row[8]);
        Assert.AreEqual(2.5d, row[9]);
        Assert.AreEqual(3.5m, row[10]);
        Assert.AreEqual(4.5m, row[11]);
        Assert.AreEqual(true, row[12]);
        Assert.AreEqual('Z', row[13]);
        Assert.AreEqual("text", row[14]);
        Assert.AreEqual(dateTime, row[15]);
        Assert.AreEqual(dateTimeOffset, row[16]);
        Assert.AreEqual(timeSpan, row[17]);
        Assert.AreEqual(guid, row[18]);
        Assert.AreEqual("object", row[19]);
    }

    [TestMethod]
    public void TableNullableTrailingCommaExample_ShouldExecuteWithAnExplicitFixtureMapping()
    {
        const string query = """
            table NullableExample {
                Id: int?,
                Name: string,
                IsActive: bool?,
            };
            couple #readmods.records with table NullableExample as Data;
            select Id, Name, IsActive from Data();
            """;
        var provider = new ReadModifiersSchemaProvider(
        [Row(("Id", 1), ("Name", "Test"), ("IsActive", true))]);

        using var table = CreateAndRunVirtualMachine(query, schemaProvider: provider).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [1, "Test", true]);
    }

    [TestMethod]
    public void TableSettingsExample_ShouldExecuteWithExplicitProfileFixtureMappings()
    {
        const string query = """
            table ApiItem {
                Token: string
            };

            couple #settings.items with table ApiItem and settings prod as ProdItems;
            couple #settings.items with settings staging as StagingItems;

            select p.Token, s.Token
            from ProdItems() p
            inner join StagingItems() s on 1 = 1;
            """;
        var provider = new SourceRuntimeSettingsLifecycleTests.SettingsSchemaProvider(declareRequirement: true);
        var resolver = new REC141ProfileResolver();
        var options = new CompilationOptions(sourceRuntimeSettingsResolver: resolver);
        using var table = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            options).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["prod-token", "staging-token"]);
        CollectionAssert.AreEquivalent(new[] { "prod", "staging" }, resolver.Profiles.ToArray());
    }

    [TestMethod]
    public void TableReadModifiersExample_ShouldExecuteWithExplicitFixtureProviderMapping()
    {
        const string query = """
            table LegacyInvoiceRow {
                InvoiceNo: string encoding 'windows-1250' trim,
                CustomerName: string encoding 'windows-1250' trim,
                Total: decimal culture 'pl-PL' format '#,##0.00',
                Attachment: string source codec 'base64'
            };

            couple #readmods.records with table LegacyInvoiceRow as Invoices;

            select InvoiceNo, CustomerName, Total, Attachment
            from Invoices('legacy-invoices.csv');
            """;
        var provider = new ReadModifiersSchemaProvider(
            [Row(
                ("InvoiceNo", "  FV-001  "),
                ("CustomerName", "  Jan Kowalski  "),
                ("Total", "1 234,56"),
                ("Attachment", "cGF5bG9hZA=="))],
            ReadModifiersValidationMode.LenientUnsupportedModifiers);

        using var table = CreateAndRunVirtualMachine(query, schemaProvider: provider).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["FV-001", "Jan Kowalski", 1234.56m, "payload"]);
    }

    private static IReadOnlyDictionary<string, object?> Row(params (string Name, object? Value)[] values)
    {
        return values.ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal);
    }

    private sealed class REC141ProfileResolver : ISourceRuntimeSettingsResolver
    {
        public List<string?> Profiles { get; } = [];

        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
        {
            Profiles.Add(request.ProfileName);
            return new Dictionary<string, string>
            {
                ["TOKEN"] = $"{request.ProfileName}-token"
            };
        }
    }
}

[TestClass]
public sealed class DiagnosticREC141BinaryExamplesTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    public void BinaryPngExample_ShouldExecuteWithExplicitFixtureProviderMapping()
    {
        var query = @"
            binary PngSignature {
                Signature: byte[8] magic [0x89, 0x50, 0x4E, 0x47,
                                          0x0D, 0x0A, 0x1A, 0x0A]
            }

            binary PngChunk {
                Length:     int be,
                ChunkType:  string[4] ascii,
                Data:       byte[Length],
                Crc:        int be
            }

            binary IhdrData {
                Width:              int be,
                Height:             int be,
                BitDepth:           byte,
                ColorType:          byte,
                CompressionMethod: byte,
                FilterMethod:       byte,
                InterlaceMethod:    byte
            }

            select f.Name, ihdr.Width, ihdr.Height, ihdr.BitDepth, ihdr.ColorType
            from #test.files() f
            cross apply Interpret<PngSignature>(f.Content) sig
            cross apply InterpretAt<PngChunk>(f.Content, 8) chunk
            cross apply Interpret<IhdrData>(chunk.Data) ihdr
            where chunk.ChunkType = 'IHDR'";
        var content = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D,
            0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x03, 0x00,
            0x00, 0x00, 0x02, 0x00,
            0x08, 0x06, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        };
        var provider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>>
            {
                ["#test"] = [new BinaryEntity { Name = "image.png", Content = content }]
            });

        var compiled = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            TestCompilationOptions);
        using var table = compiled.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("image.png", table[0][0]);
        Assert.AreEqual(768, table[0][1]);
        Assert.AreEqual(512, table[0][2]);
        Assert.AreEqual((byte)8, table[0][3]);
        Assert.AreEqual((byte)6, table[0][4]);
    }

    [TestMethod]
    public void BinaryPngPublishedExample_ShouldExposeTheCheckListCorrection()
    {
        const string invalidQuery = @"
            binary PngSignature {
                Signature: byte[8] check Signature = [0x89, 0x50, 0x4E, 0x47,
                                                       0x0D, 0x0A, 0x1A, 0x0A]
            }
            select 1 from #test.files()";
        var provider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>>
            {
                ["#test"] = []
            });

        var result = new QueryAnalyzer(provider).Analyze(invalidQuery);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ2001_UnexpectedToken,
            "REC-141 PNG byte-list CHECK example");
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual("[", invalidQuery.Substring(diagnostic.Span.Start, diagnostic.Span.Length));
    }

    [TestMethod]
    public void BinaryApacheCorrectedExample_ShouldExecuteWithExplicitFixtureProviderMapping()
    {
        const string query = ApacheCorrectedQuery;
        var provider = CreateApacheProvider();

        var compiled = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            TestCompilationOptions);
        using var table = compiled.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("/missing", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
        Assert.AreEqual(new DateTime(2026, 7, 17, 14, 34, 56), table[0][2]);
    }

    [TestMethod]
    public void BinaryApachePublishedExample_ShouldExposeConsumedDelimiterBoundary()
    {
        var query = ApacheQuery.Replace("Max(log.Timestamp)", "Count(*)");
        var compiled = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            CreateApacheProvider(),
            LoggerResolver,
            TestCompilationOptions);

        var exception = Assert.ThrowsExactly<RuntimeParseException>(() =>
        {
            using var table = compiled.Run(CancellationToken.None);
            _ = table.Count;
        });
        Assert.AreEqual("ISE0004", exception.FormattedErrorCode);
        StringAssert.Contains(exception.Message, "ApacheLog._");
    }

    [TestMethod]
    public void BinaryApachePublishedExample_ShouldExposeTheStringAggregateCorrection()
    {
        var result = new QueryAnalyzer(CreateApacheProvider()).Analyze(ApacheQuery);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ3088_NoMatchingCallableOverload,
            "REC-141 Apache string timestamp aggregate");
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        StringAssert.Contains(diagnostic.Message, "Max");
    }

    [TestMethod]
    public void BinaryMixedProtocolExample_ShouldExecuteWithExplicitFixtureProviderMapping()
    {
        const string query = MixedCorrectedQuery;

        var commandPayload = "PING 42"u8.ToArray();
        var telemetryPayload = new byte[19];
        BitConverter.GetBytes(123L).CopyTo(telemetryPayload, 0);
        BitConverter.GetBytes((short)7).CopyTo(telemetryPayload, 8);
        BitConverter.GetBytes(12.5d).CopyTo(telemetryPayload, 10);
        telemetryPayload[18] = 1;

        var provider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>>
            {
                ["#test"] =
                [
                    new BinaryEntity { Name = "command.bin", Content = CreateMessageFrame(commandPayload, 0x01) },
                    new BinaryEntity { Name = "telemetry.bin", Content = CreateMessageFrame(telemetryPayload, 0x02) }
                ]
            });

        var published = CompileGeneratedQuery(
            MixedPublishedQuery,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            TestCompilationOptions);
        var publishedException = Assert.ThrowsExactly<RuntimeParseException>(() =>
        {
            using var publishedTable = published.Run(CancellationToken.None);
            _ = publishedTable.Count;
        });
        Assert.AreEqual("ISE0002", publishedException.FormattedErrorCode);
        StringAssert.Contains(publishedException.Message, "MessageFrame.Sync");

        var compiled = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            TestCompilationOptions);
        using var table = compiled.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)0x01, table[0][0]);
        Assert.AreEqual("PING", table[0][1]);
        Assert.AreEqual((byte)0x02, table[1][0]);
        Assert.AreEqual("7", table[1][1]);
    }

    [TestMethod]
    public void BinaryCobolPublishedExample_ShouldExposeDateCallableCorrection()
    {
        var provider = CreateCobolProvider();
        var published = new QueryAnalyzer(provider).Analyze(CobolPublishedQuery);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            published,
            DiagnosticCode.MQ3086_UnknownCallable,
            "REC-141 COBOL ParseDate example");
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        StringAssert.Contains(diagnostic.Message, "ParseDate");

        var compiled = CompileGeneratedQuery(
            CobolCorrectedQuery,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            TestCompilationOptions);
        using var table = compiled.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("CUST000001", table[0][0]);
        Assert.AreEqual("Alice", table[0][1]);
        Assert.AreEqual("1 Main St, Krakow, PL 30-001", table[0][2]);
        Assert.AreEqual(1234.56m, table[0][3]);
        Assert.AreEqual(new DateTime(2026, 7, 17), table[0][4]);
    }

    [TestMethod]
    public void BinaryStoragePublishedExample_ShouldExecuteWithExplicitFixtureProviderMapping()
    {
        var data = new byte[20 + 12];
        BitConverter.GetBytes(0x53544F52).CopyTo(data, 0);
        BitConverter.GetBytes((short)1).CopyTo(data, 4);
        BitConverter.GetBytes((short)0).CopyTo(data, 6);
        BitConverter.GetBytes(1).CopyTo(data, 8);
        BitConverter.GetBytes(20L).CopyTo(data, 12);
        data[20] = 1;
        BitConverter.GetBytes(7).CopyTo(data, 21);
        BitConverter.GetBytes((short)5).CopyTo(data, 25);
        "hello"u8.CopyTo(data.AsSpan(27));

        var provider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>>
            {
                ["#test"] = [new BinaryEntity { Name = "storage.dat", Content = data }]
            });

        var publishedException = Assert.ThrowsExactly<MusoqQueryException>(() =>
            CompileGeneratedQuery(
                StoragePublishedQuery,
                Guid.NewGuid().ToString(),
                provider,
                LoggerResolver,
                TestCompilationOptions));
        StringAssert.Contains(publishedException.Message, "MQ2030");

        var offsetException = Assert.ThrowsExactly<MusoqQueryException>(() =>
            CompileGeneratedQuery(
                StorageCorrectedQuery.Replace(
                    "ToInt32(h.DataOffset) ?? 0",
                    "h.DataOffset",
                    StringComparison.Ordinal),
                Guid.NewGuid().ToString(),
                provider,
                LoggerResolver,
                TestCompilationOptions));
        StringAssert.Contains(offsetException.Message, "MQ9001");

        var compiled = CompileGeneratedQuery(
            StorageCorrectedQuery,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            TestCompilationOptions);
        using var table = compiled.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)1, table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual((byte)1, table[0][2]);
        Assert.AreEqual("hello", table[0][3]);
    }

    private const string MixedPublishedQuery = """
        binary MessageFrame {
            Sync:        short le check Sync = 0xAA55,
            MsgType:     byte,
            PayloadLen:  short le,
            Payload:     byte[PayloadLen],
            Checksum:    short le
        }

        text CommandPayload {
            Command:   until ' ',
            _:         whitespace*,
            Args:       rest
        }

        binary TelemetryPayload {
            Timestamp:   long le,
            SensorId:    short le,
            Value:       double le,
            Flags:       byte
        }

        select
            frame.MsgType,
            CASE frame.MsgType
                WHEN 0x01 THEN command.Command
                WHEN 0x02 THEN ToString(telemetry.SensorId)
                ELSE 'Unknown'
            END AS Identifier
        FROM #test.files() f
        CROSS APPLY InterpretAt<MessageFrame>(f.Content, 0) frame
        OUTER APPLY TryParse<CommandPayload>(ToText(frame.Payload, 'utf8')) command
        OUTER APPLY TryInterpret<TelemetryPayload>(frame.Payload) telemetry
        WHERE frame.MsgType IN (0x01, 0x02)
        """;

    private const string MixedCorrectedQuery = """
        binary MessageFrame {
            Sync:        ushort le check Sync = 0xAA55,
            MsgType:     byte,
            PayloadLen:  short le,
            Payload:     byte[PayloadLen],
            Checksum:    short le
        }

        text CommandPayload {
            Command:   until ' ',
            _:         whitespace*,
            Args:       rest
        }

        binary TelemetryPayload {
            Timestamp:   long le,
            SensorId:    short le,
            Value:       double le,
            Flags:       byte
        }

        select
            frame.MsgType,
            CASE frame.MsgType
                WHEN 0x01 THEN command.Command
                WHEN 0x02 THEN ToString(telemetry.SensorId)
                ELSE 'Unknown'
            END AS Identifier
        FROM #test.files() f
        CROSS APPLY InterpretAt<MessageFrame>(f.Content, 0) frame
        OUTER APPLY TryParse<CommandPayload>(ToText(frame.Payload, 'utf8')) command
        OUTER APPLY TryInterpret<TelemetryPayload>(frame.Payload) telemetry
        WHERE frame.MsgType IN (0x01, 0x02)
        """;

    private const string StoragePublishedQuery = """
        binary StorageHeader {
            Magic:          int le check Magic = 0x53544F52,
            Version:        short le,
            Flags:          short le,

            IsCompressed: (Flags & 0x01) <> 0,
            HasIndex:     (Flags & 0x02) <> 0,

            RecordCount:    int le,
            IndexOffset:    long le when HasIndex,
            DataOffset:     long le
        }

        binary StorageRecord {
            RecordType:     byte,
            RecordLength:   int le,

            StringLen:      short le when RecordType = 1,
            StringData:     string[StringLen] utf8 when RecordType = 1,

            BlobData:       byte[RecordLength - 1] when RecordType = 2,

            NestedCount:    int le when RecordType = 3,
            Nested:         SubRecord[NestedCount] when RecordType = 3
        }

        binary SubRecord {
            Key:    string[32] utf8 nullterm,
            Value:  string[64] utf8 nullterm
        }

        SELECT
            h.Version,
            h.RecordCount,
            r.RecordType,
            CASE r.RecordType
                WHEN 1 THEN r.StringData
                WHEN 2 THEN ToHex(r.BlobData)
                WHEN 3 THEN 'Nested: ' + ToString(r.NestedCount) + ' items'
                ELSE 'Unknown record type'
            END AS Content
        FROM #test.files() f
        CROSS APPLY Interpret<StorageHeader>(f.Content) h
        CROSS APPLY InterpretAt<StorageRecord>(f.Content, h.DataOffset) r
        """;

    private const string StorageCorrectedQuery = """
        binary StorageHeader {
            Magic:          int le check Magic = 0x53544F52,
            Version:        short le,
            Flags:          short le,

            IsCompressed: (Flags & 0x01) <> 0,
            HasIndex:     (Flags & 0x02) <> 0,

            RecordCount:    int le,
            IndexOffset:    long le when HasIndex,
            DataOffset:     long le
        }

        binary SubRecord {
            Key:    string[32] utf8 nullterm,
            Value:  string[64] utf8 nullterm
        }

        binary StorageRecord {
            RecordType:     byte,
            RecordLength:   int le,

            StringLen:      short le when RecordType = 1,
            StringData:     string[StringLen] utf8 when RecordType = 1,

            BlobData:       byte[RecordLength - 1] when RecordType = 2,

            NestedCount:    int le when RecordType = 3,
            Nested:         SubRecord[NestedCount] when RecordType = 3
        }

        SELECT
            h.Version,
            h.RecordCount,
            r.RecordType,
            CASE r.RecordType
                WHEN 1 THEN r.StringData
                WHEN 2 THEN ToHex(r.BlobData)
                WHEN 3 THEN 'Nested: ' + ToString(r.NestedCount) + ' items'
                ELSE 'Unknown record type'
            END AS Content
        FROM #test.files() f
        CROSS APPLY Interpret<StorageHeader>(f.Content) h
        CROSS APPLY InterpretAt<StorageRecord>(f.Content, ToInt32(h.DataOffset) ?? 0) r
        """;

    private const string CobolPublishedQuery = """
        text CobolCustomerRecord {
            CustomerId:      chars[10],
            CustomerName:    chars[30] trim,
            AddressLine1:    chars[40] trim,
            AddressLine2:    chars[40] trim,
            City:            chars[20] trim,
            State:           chars[2],
            ZipCode:         chars[10] trim,
            Balance:         chars[12],
            StatusCode:      chars[1],
            LastUpdate:      chars[8]
        }

        SELECT
            r.CustomerId,
            r.CustomerName,
            Concat(r.AddressLine1, ', ', r.City, ', ', r.State, ' ', r.ZipCode) AS Address,
            ToDecimal(r.Balance) / 100.0 AS Balance,
            ParseDate(r.LastUpdate, 'yyyyMMdd') AS LastUpdated
        FROM #test.files() f
        CROSS APPLY Lines(f.Text) line
        CROSS APPLY Parse<CobolCustomerRecord>(line.Value) r
        WHERE r.StatusCode = 'A'
        """;

    private const string CobolCorrectedQuery = """
        text CobolCustomerRecord {
            CustomerId:      chars[10],
            CustomerName:    chars[30] trim,
            AddressLine1:    chars[40] trim,
            AddressLine2:    chars[40] trim,
            City:            chars[20] trim,
            State:           chars[2],
            ZipCode:         chars[10] trim,
            Balance:         chars[12],
            StatusCode:      chars[1],
            LastUpdate:      chars[8]
        }

        SELECT
            r.CustomerId,
            r.CustomerName,
            Concat(r.AddressLine1, ', ', r.City, ', ', r.State, ' ', r.ZipCode) AS Address,
            ToDecimal(r.Balance) / 100.0 AS Balance,
            ToDateTimeWithFormat(r.LastUpdate, 'yyyyMMdd') AS LastUpdated
        FROM #test.files() f
        CROSS APPLY Lines(f.Text) line
        CROSS APPLY Parse<CobolCustomerRecord>(line.Value) r
        WHERE r.StatusCode = 'A'
        """;

    private static TextSchemaProvider CreateCobolProvider()
    {
        var line = string.Concat(
            "CUST000001".PadRight(10),
            "Alice".PadRight(30),
            "1 Main St".PadRight(40),
            string.Empty.PadRight(40),
            "Krakow".PadRight(20),
            "PL",
            "30-001".PadRight(10),
            "000000123456".PadRight(12),
            "A",
            "20260717");
        return new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>>
            {
                ["#test"] = [new TextEntity { Name = "CUSTMAST.DAT", Text = line }]
            });
    }

    private static byte[] CreateMessageFrame(byte[] payload, byte messageType)
    {
        var frame = new byte[2 + 1 + 2 + payload.Length + 2];
        BitConverter.GetBytes(unchecked((short)0xAA55)).CopyTo(frame, 0);
        frame[2] = messageType;
        BitConverter.GetBytes((short)payload.Length).CopyTo(frame, 3);
        payload.CopyTo(frame.AsSpan(5));
        return frame;
    }

    private const string ApacheQuery = """
        text ApacheLog {
            RemoteHost:    until ' ',
            _:             literal ' ',
            Identity:      until ' ',
            _:             literal ' ',
            User:          until ' ',
            _:             literal ' ',
            Timestamp:     between '[' ']',
            _:             literal ' "',
            Method:        until ' ',
            _:             literal ' ',
            Path:          until ' ',
            _:             literal ' ',
            Protocol:      until '"',
            _:             literal '" ',
            Status:        pattern '\d{3}',
            _:             literal ' ',
            Size:          pattern '\d+|-',
            _:             optional literal ' "',
            Referrer:      optional between '"' '"',
            _:             optional literal ' "',
            UserAgent:     optional between '"' '"'
        }

        select log.Path, Count(*) as ErrorCount, Max(log.Timestamp) as LastSeen
        from #test.lines() f
        cross apply Lines(f.Text) line
        cross apply Parse<ApacheLog>(line.Value) log
        where log.Status = '404'
        group by log.Path
        order by ErrorCount desc
        take 20
        """;

    private const string ApacheCorrectedQuery = """
        text ApacheLog {
            RemoteHost:    until ' ',
            Identity:      until ' ',
            User:          until ' ',
            Timestamp:     between '[' ']',
            _:             literal ' "',
            Method:        until ' ',
            Path:          until ' ',
            Protocol:      until '"',
            _:             literal ' ',
            Status:        pattern '\d{3}',
            _:             literal ' ',
            Size:          pattern '\d+|-',
            _:             optional literal ' "',
            Referrer:      optional between '"' '"',
            _:             optional literal ' "',
            UserAgent:     optional between '"' '"'
        }

        select log.Path, Count(*) as ErrorCount,
            MaxDateTime(ToDateTimeWithFormat(log.Timestamp, 'dd/MMM/yyyy:HH:mm:ss zzz')) as LastSeen
        from #test.lines() f
        cross apply Lines(f.Text) line
        cross apply Parse<ApacheLog>(line.Value) log
        where log.Status = '404'
        group by log.Path
        order by ErrorCount desc
        take 20
        """;

    private static TextSchemaProvider CreateApacheProvider()
    {
        const string line =
            "192.0.2.1 - - [17/Jul/2026:12:34:56 +0000] \"GET /missing HTTP/1.1\" 404 12 \"-\" \"agent\"";
        return new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>>
            {
                ["#test"] = [new TextEntity { Name = "access.log", Text = line }]
            });
    }
}
