using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// REC-136's production boundary is independent query sessions. The class is
/// kept out of MSTest's own scheduler because the historical star fixture and
/// the test culture setup use process-wide harness state; each test below still
/// creates and exercises independent production sessions concurrently.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class DiagnosticRec136ConcurrentSessionIsolationTests
{
    private const int CasesPerFamily = 12;

    private static readonly CompilationOptions TestCompilationOptions =
        new(usePrimitiveTypeValidation: false);

    [TestMethod]
    public async Task ConcurrentProviderSessions_ShouldKeepRowsAndSchemaBindingsSeparate()
    {
        var observations = await Task.WhenAll(Enumerable.Range(1, CasesPerFamily).Select(index =>
            Task.Run(() =>
            {
                var expected = $"REC136-provider-{index}";
                var query =
                    $"select e.Name from #A.Entities() e /* REC-136-PROVIDER-{index:00}-candidate */";
                using var compiled = InstanceCreator.CompileForExecution(
                    query,
                    $"REC136_PROVIDER_{index}_{Guid.NewGuid():N}",
                    CreateBasicProvider(expected),
                    new TestsLoggerResolver(),
                    TestCompilationOptions);
                using var table = compiled.Run();

                Assert.HasCount(1, table, $"provider case {index}");
                Assert.AreEqual(expected, table[0][0], $"provider case {index}");
                return (Index: index, Value: (string)table[0][0]);
            })));

        Assert.HasCount(CasesPerFamily, observations);
        CollectionAssert.AreEquivalent(
            Enumerable.Range(1, CasesPerFamily)
                .Select(index => $"REC136-provider-{index}")
                .ToArray(),
            observations.Select(static observation => observation.Value).ToArray());
    }

    [TestMethod]
    public async Task ConcurrentParameterAndSettingsSessions_ShouldKeepSnapshotsSeparate()
    {
        var parameterObservations = await Task.WhenAll(Enumerable.Range(1, CasesPerFamily).Select(index =>
            Task.Run(() =>
            {
                var expected = index * 17;
                var parameterQuery =
                    $"param(value: int) select $value as Value from #A.Entities() /* REC-136-CONTEXT-{index:00}-candidate */";
                using var compiled = InstanceCreator.CompileForExecution(
                    parameterQuery,
                    $"REC136_PARAMETER_{index}_{Guid.NewGuid():N}",
                    CreateBasicProvider($"REC136-parameter-{index}"),
                    new TestsLoggerResolver(),
                    TestCompilationOptions);
                compiled.Parameters["value"] = expected;

                using var table = compiled.Run();
                Assert.HasCount(1, table, $"parameter case {index}");
                Assert.AreEqual(expected, table[0][0], $"parameter case {index}");
                return expected;
            })));

        var settingsObservations = await Task.WhenAll(Enumerable.Range(1, CasesPerFamily).Select(index =>
            Task.Run(() =>
            {
                var expected = $"REC136-setting-{index}";
                var settingsQuery =
                    $"select Token from #settings.items() /* REC-136-CONTEXT-{index:00}-candidate */";
                using var compiled = InstanceCreator.CompileForExecution(
                    settingsQuery,
                    $"REC136_SETTINGS_{index}_{Guid.NewGuid():N}",
                    new SourceRuntimeSettingsLifecycleTests.SettingsSchemaProvider(declareRequirement: true),
                    new TestsLoggerResolver(),
                    new CompilationOptions(sourceRuntimeSettingsResolver: new SessionSettingsResolver(expected)));

                using var table = compiled.Run();
                Assert.HasCount(1, table, $"settings case {index}");
                Assert.AreEqual(expected, table[0][0], $"settings case {index}");
                return (string)table[0][0];
            })));

        CollectionAssert.AreEquivalent(
            Enumerable.Range(1, CasesPerFamily).Select(index => index * 17).ToArray(),
            parameterObservations);
        CollectionAssert.AreEquivalent(
            Enumerable.Range(1, CasesPerFamily)
                .Select(index => $"REC136-setting-{index}")
                .ToArray(),
            settingsObservations);
    }

    [TestMethod]
    public async Task ConcurrentDiagnosticSessions_ShouldKeepLocationsCorrelationsAndEnumDescriptorsSeparate()
    {
        var locationObservations = await Task.WhenAll(Enumerable.Range(1, CasesPerFamily).Select(index =>
            Task.Run(() =>
            {
                var marker = $"REC136Missing{index}";
                var query = $"select {marker} from #A.Entities() /* REC-136-DIAG-{index:00}-seed */";
                var result = InstanceCreator.CompileWithDiagnostics(
                    query,
                    $"REC136_DIAGNOSTIC_{index}_{Guid.NewGuid():N}",
                    CreateBasicProvider($"REC136-diagnostic-{index}"),
                    new TestsLoggerResolver(),
                    TestCompilationOptions);

                Assert.IsFalse(result.Succeeded, $"diagnostic case {index}");
                var diagnostic = result.Errors.Single(item =>
                    item.Code == DiagnosticCode.MQ3001_UnknownColumn);
                Assert.Contains(marker, diagnostic.Message, $"diagnostic case {index}");
                Assert.IsTrue(diagnostic.Span.Start >= 0, $"diagnostic case {index}");
                Assert.IsTrue(diagnostic.Span.End <= query.Length, $"diagnostic case {index}");
                Assert.IsTrue(
                    query.Substring(diagnostic.Span.Start, diagnostic.Span.Length).Contains(marker, StringComparison.Ordinal),
                    $"diagnostic case {index} location was not tied to its own query marker");

                return (Index: index, Marker: marker, Offset: diagnostic.Span.Start);
            })));

        Assert.HasCount(CasesPerFamily, locationObservations);
        Assert.AreEqual(
            CasesPerFamily,
            locationObservations.Select(static observation => observation.Marker)
                .Distinct(StringComparer.Ordinal)
                .Count());

        var correlationObservations = await Task.WhenAll(Enumerable.Range(1, CasesPerFamily).Select(index =>
            Task.Run(() =>
            {
                var source = new SourceText($"select 1 /* REC136-CORRELATION-{index} */");
                var context = new Musoq.Evaluator.DiagnosticContext(source);
                context.ReportException(
                    InternalDiagnosticException.ForCompiler(
                        new InvalidOperationException($"REC136-private-{index}")));
                var diagnostic = context.Diagnostics.Single();

                Assert.AreEqual(DiagnosticCode.MQ9001_InternalCompilerError, diagnostic.Code);
                Assert.IsNotNull(diagnostic.CorrelationId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.CorrelationId));
                Assert.IsTrue(diagnostic.CorrelationId.StartsWith("internal-", StringComparison.Ordinal));
                Assert.DoesNotContain($"REC136-private-{index}", diagnostic.Message);
                return diagnostic.CorrelationId!;
            })));

        Assert.HasCount(
            CasesPerFamily,
            correlationObservations.Distinct(StringComparer.Ordinal));

        var enumObservations = await Task.WhenAll(Enumerable.Range(1, CasesPerFamily).Select(index =>
            Task.Run(() =>
            {
                var enumName = $"REC136Enum{index}";
                var query =
                    $"enum {enumName} : short {{ Value = {index}s }};" +
                    $"table Jobs {{ Status: {enumName} }};" +
                    "couple #rec103.native with table Jobs as Jobs;select Status from Jobs() " +
                    $"/* REC-136-ENUM-{index:00}-candidate */";
                var provider = new REC103MetadataSchemaProvider();
                var result = InstanceCreator.CompileWithDiagnostics(
                    query,
                    $"REC136_ENUM_{index}_{Guid.NewGuid():N}",
                    provider,
                    new TestsLoggerResolver(),
                    TestCompilationOptions);

                try
                {
                    Assert.IsTrue(
                        result.Succeeded,
                        string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.Message)));
                    var column = provider.Schema.CapturedColumns
                        .SelectMany(static columns => columns)
                        .Last(item => item.ColumnName == "Status");
                    Assert.IsNotNull(column.EnumType, $"enum case {index}");
                    Assert.AreEqual(enumName, column.EnumType.DisplayName, $"enum case {index}");
                    Assert.AreEqual(EnumTypeOrigin.QueryLocal, column.EnumType.Origin, $"enum case {index}");
                    return column.EnumType.Fingerprint;
                }
                finally
                {
                    result.CompiledQuery?.Dispose();
                }
            })));

        Assert.HasCount(
            CasesPerFamily,
            enumObservations.Distinct(StringComparer.Ordinal));
    }

    [TestMethod]
    public async Task ConcurrentHistoricalStarModifierSessions_ShouldKeepRowsAndColumnsSeparate()
    {
        var observations = await Task.WhenAll(Enumerable.Range(1, CasesPerFamily).Select(index =>
            Task.Run(() =>
            {
                var expected = $"REC136-star-{index}-owner";
                var query =
                    $"select * like '%o%' exclude (country) replace (Population * 3 as population) " +
                    $"rename (population as Population3x) from #A.Entities() /* REC-136-STAR-{index:00}-candidate */";
                var provider = new BasicSchemaProvider<BasicEntity>(
                    new Dictionary<string, IEnumerable<BasicEntity>>
                    {
                        ["#A"] = [new BasicEntity(expected) { Money = index }]
                    });
                using var compiled = InstanceCreator.CompileForExecution(
                    query,
                    $"REC136_STAR_{index}_{Guid.NewGuid():N}",
                    provider,
                    new TestsLoggerResolver(),
                    TestCompilationOptions);
                using var table = compiled.Run();

                Assert.HasCount(1, table, $"star case {index}");
                Assert.IsFalse(
                    table.Columns.Any(column => column.ColumnName.Equals("Country", StringComparison.OrdinalIgnoreCase)),
                    $"star case {index}");
                Assert.IsTrue(
                    table.Rows.SelectMany(static row => row.Values).Any(value => Equals(value, (decimal)index)),
                    $"star case {index} returned a row from another session");
                Assert.IsTrue(
                    table.Columns.Any(column => column.ColumnName == "Population3x"),
                    $"star case {index}");
                return expected;
            })));

        CollectionAssert.AreEquivalent(
            Enumerable.Range(1, CasesPerFamily)
                .Select(index => $"REC136-star-{index}-owner")
                .ToArray(),
            observations);
    }

    private static BasicSchemaProvider<BasicEntity> CreateBasicProvider(string name)
    {
        return new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [new BasicEntity(name)]
            });
    }

    private sealed class SessionSettingsResolver(string token) : ISourceRuntimeSettingsResolver
    {
        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["TOKEN"] = token
            };
        }
    }
}
