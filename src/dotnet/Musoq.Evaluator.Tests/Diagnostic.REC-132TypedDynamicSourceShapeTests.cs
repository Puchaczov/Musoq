using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Dynamic;
using Musoq.Evaluator.Tests.Schema.ReadModifiers;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Permanent REC-132 coverage for the historical typed-versus-dynamic source
/// shape caveat.  The ASOF and comparison families intentionally execute the
/// same query over a public CLR source and over TABLE/COUPLE dictionary rows.
/// </summary>
[TestClass]
public sealed class DiagnosticRec132TypedDynamicSourceShapeTests
{
    private static readonly CompilationOptions CompilationOptions =
        new(usePrimitiveTypeValidation: false);

    [TestMethod]
    [DataRow("AS-01")]
    [DataRow("AS-02")]
    [DataRow("AS-03")]
    [DataRow("AS-04")]
    [DataRow("AS-05")]
    [DataRow("AS-06")]
    [DataRow("AS-07")]
    [DataRow("AS-08")]
    [DataRow("AS-09")]
    [DataRow("AS-10")]
    [DataRow("AS-11")]
    [DataRow("AS-12")]
    public void TypedAndDynamicAsOfMatrix_ShouldPreserveSourceShape(string caseId)
    {
        var table = RunAsOf(caseId, IsDynamicCase(caseId));

        AssertAsOfRows(table, Variant(caseId));
    }

    [TestMethod]
    [DataRow("MD-01")]
    [DataRow("MD-02")]
    [DataRow("MD-03")]
    [DataRow("MD-04")]
    [DataRow("MD-05")]
    [DataRow("MD-06")]
    [DataRow("MD-07")]
    [DataRow("MD-08")]
    [DataRow("MD-09")]
    [DataRow("MD-10")]
    [DataRow("MD-11")]
    [DataRow("MD-12")]
    public void ModifierAndLogicalMetadataMatrix_ShouldSurvivePlanningAndExecution(string caseId)
    {
        var provider = new ReadModifiersSchemaProvider(
            [
                Row(("Name", "  Name  "), ("Amount", "12,50"), ("Payload", "cGF5bG9hZA=="),
                    ("Plain", "plain"))
            ]);
        var query = BuildMetadataQuery(caseId);
        using var compiled = Compile(query, provider);
        var table = TableMaterializationTestHelper.Materialize(compiled.Run());

        AssertMetadataRows(table, caseId);
        AssertMetadataContract(provider, caseId);
    }

    [TestMethod]
    [DataRow("PC-01")]
    [DataRow("PC-02")]
    [DataRow("PC-03")]
    [DataRow("PC-04")]
    [DataRow("PC-05")]
    [DataRow("PC-06")]
    [DataRow("PC-07")]
    [DataRow("PC-08")]
    [DataRow("PC-09")]
    [DataRow("PC-10")]
    [DataRow("PC-11")]
    [DataRow("PC-12")]
    public void ProviderCompatibilityMatrix_ShouldUseSourceContractDiagnostics(string caseId)
    {
        var query = BuildCompatibilityQuery(caseId);
        var provider = new ReadModifiersSchemaProvider(
            [Row(("Value", "12.50"))],
            ReadModifiersValidationMode.ValidateSourceKinds,
            new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                ["Value"] = typeof(string)
            });
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            CompilationOptions);

        Assert.IsFalse(result.Succeeded, caseId);
        var error = result.Errors.Single();
        Assert.AreEqual(DiagnosticCode.MQ3071_SourceContractError, error.Code, caseId);
        Assert.AreEqual(DiagnosticPhase.Bind, error.Phase, caseId);
        Assert.AreEqual(DiagnosticSourceKind.Query, error.SourceKind, caseId);

        var envelope = result.ToEnvelopes().Single();
        Assert.AreEqual(DiagnosticCode.MQ3071_SourceContractError, envelope.Code, caseId);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase, caseId);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind, caseId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation), caseId);
        Assert.IsNotEmpty(envelope.SuggestedFixes, caseId);
        Assert.IsNotEmpty(envelope.Actions, caseId);
        Assert.DoesNotContain("SQL typo", envelope.Message, caseId);
    }

    [TestMethod]
    [DataRow("OR-01")]
    [DataRow("OR-02")]
    [DataRow("OR-03")]
    [DataRow("OR-04")]
    [DataRow("OR-05")]
    [DataRow("OR-06")]
    [DataRow("OR-07")]
    [DataRow("OR-08")]
    [DataRow("OR-09")]
    [DataRow("OR-10")]
    [DataRow("OR-11")]
    [DataRow("OR-12")]
    public void OriginalAndSubstituteMatrix_ShouldNotConflateSourceShapes(string caseId)
    {
        var variant = Variant(caseId);
        var typed = RunAsOf($"AS-{((variant - 1) % 12) + 1:00}", false);
        var dynamic = RunAsOf($"AS-{((variant - 1) % 12) + 1:00}", true);

        Assert.AreEqual(typed.Count, dynamic.Count, caseId);
        for (var rowIndex = 0; rowIndex < typed.Count; rowIndex++)
        {
            var expected = typed[rowIndex].Values;
            var actual = dynamic[rowIndex].Values;
            Assert.AreEqual(expected.Length, actual.Length, caseId);
            for (var columnIndex = 0; columnIndex < expected.Length; columnIndex++)
                Assert.AreEqual(expected[columnIndex], actual[columnIndex], $"{caseId} row {rowIndex}, column {columnIndex}");
        }
    }

    private static Table RunAsOf(string caseId, bool dynamicPath)
    {
        var variant = Variant(caseId);
        var query = BuildAsOfQuery(variant, dynamicPath);
        using var compiled = Compile(query, dynamicPath ? CreateDynamicProvider(variant) : CreateTypedProvider(variant));
        return TableMaterializationTestHelper.Materialize(compiled.Run());
    }

    private static CompiledQuery Compile(string query, ISchemaProvider provider)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            CompilationOptions);
    }

    private static string BuildAsOfQuery(int variant, bool dynamicPath)
    {
        var prefix = dynamicPath
            ? "table Events { Country: string, Population: decimal, Name: string };" +
              "table Snapshots { Country: string, Population: decimal, Name: string };" +
              "couple #events.entities with table Events as Events;" +
              "couple #snapshots.entities with table Snapshots as Snapshots;"
            : string.Empty;
        var left = dynamicPath ? "Events()" : "#events.entities()";
        var right = dynamicPath ? "Snapshots()" : "#snapshots.entities()";
        var join = variant switch
        {
            1 or 2 => $"asof join {right} s on e.Population >= s.Population",
            3 or 4 => $"asof left join {right} s on e.Population >= s.Population",
            5 or 6 => $"asof join {right} s on e.Population <= s.Population",
            7 or 8 => $"asof left join {right} s on e.Country = s.Country and e.Population >= s.Population",
            9 or 10 => $"asof left join {right} s on e.Population >= s.Population tie break by s.Name asc",
            11 or 12 => $"asof left join {right} s on e.Population >= s.Population",
            _ => throw new ArgumentOutOfRangeException(nameof(variant))
        };

        if (variant is 11 or 12)
        {
            return prefix +
                   $"with EventsData as (select e.Name, e.Country, e.Population from {left} e) " +
                   $"select e.Name, s.Name from EventsData e {join} order by e.Name;";
        }

        return prefix + $"select e.Name, s.Name from {left} e {join} order by e.Name;";
    }

    private static ISchemaProvider CreateTypedProvider(int variant)
    {
        var rows = CreateEntities(variant);
        return new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#events"] = rows.Events,
            ["#snapshots"] = rows.Snapshots
        });
    }

    private static ISchemaProvider CreateDynamicProvider(int variant)
    {
        var rows = CreateEntities(variant);
        var schema = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["Country"] = typeof(string),
            ["Population"] = typeof(decimal),
            ["Name"] = typeof(string)
        };
        return new DynamicSchemaProvider(new Dictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)>
        {
            ["#events"] = (schema, rows.Events.Select(ToRow)),
            ["#snapshots"] = (schema, rows.Snapshots.Select(ToRow))
        });
    }

    private static (BasicEntity[] Events, BasicEntity[] Snapshots) CreateEntities(int variant)
    {
        var events = new[]
        {
            new BasicEntity { Name = "event-100", Country = "A", Population = 100m },
            new BasicEntity { Name = "event-250", Country = "A", Population = 250m },
            new BasicEntity { Name = "event-b", Country = "B", Population = 50m }
        };
        var snapshots = variant is 9 or 10
            ? new[]
            {
                new BasicEntity { Name = "snapshot-old", Country = "A", Population = 80m },
                new BasicEntity { Name = "zeta", Country = "A", Population = 200m },
                new BasicEntity { Name = "alpha", Country = "A", Population = 200m }
            }
            : variant is 7 or 8
                ? new[]
                {
                    new BasicEntity { Name = "snapshot-old", Country = "A", Population = 80m },
                    new BasicEntity { Name = "snapshot-new", Country = "A", Population = 200m },
                    new BasicEntity { Name = "snapshot-b", Country = "B", Population = 100m }
                }
                : new[]
            {
                new BasicEntity { Name = "snapshot-old", Country = "A", Population = 80m },
                new BasicEntity { Name = "snapshot-new", Country = "A", Population = 200m }
            };
        return (events, snapshots);
    }

    private static Dictionary<string, object?> ToRow(BasicEntity entity) => new(StringComparer.Ordinal)
    {
        ["Country"] = entity.Country,
        ["Population"] = entity.Population,
        ["Name"] = entity.Name
    };

    private static void AssertAsOfRows(Table table, int variant)
    {
        if (variant is 1 or 2)
        {
            TableMaterializationTestHelper.AssertRowsInOrder(
                table,
                ["event-100", "snapshot-old"],
                ["event-250", "snapshot-new"]);
            return;
        }

        if (variant is 9 or 10)
        {
            TableMaterializationTestHelper.AssertRowsInOrder(
                table,
                ["event-100", "snapshot-old"],
                ["event-250", "alpha"],
                ["event-b", null]);
            return;
        }

        if (variant is 5 or 6)
        {
            TableMaterializationTestHelper.AssertRowsInOrder(
                table,
                ["event-100", "snapshot-new"],
                ["event-b", "snapshot-old"]);
            return;
        }

        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["event-100", "snapshot-old"],
            ["event-250", "snapshot-new"],
            ["event-b", null]);
    }

    private static string BuildMetadataQuery(string caseId)
    {
        const string prefix =
            "table RecordsShape { Name: string encoding 'utf-8' trim, Amount: decimal culture 'pl-PL' format '#,##0.00', Payload: string source codec 'base64', Plain: string };" +
            "couple #readmods.records with table RecordsShape as Records;";
        return caseId switch
        {
            "MD-01" => prefix + "select r.Name from Records() r;",
            "MD-02" => prefix + "select r.Amount from Records() r;",
            "MD-03" => prefix + "select r.Payload from Records() r;",
            "MD-04" => prefix + "select r.Name, r.Amount, r.Payload, r.Plain from Records() r;",
            "MD-05" => prefix + "select r.Name, r.Plain from Records() r order by r.Name;",
            "MD-06" => prefix + "with rows as (select r.Name, r.Amount from Records() r) select Name, Amount from rows;",
            "MD-07" => prefix + "select r.Name from Records() r group by r.Name;",
            "MD-08" => prefix + "select r.Amount from Records() r order by r.Amount desc take 1;",
            "MD-09" => prefix + "select r.Name, s.Plain from Records() r inner join Records() s on r.Name = s.Name;",
            "MD-10" => prefix + "select r.Payload from Records() r where r.Plain is not null;",
            "MD-11" => prefix + "select r.Name, r.Amount from Records() r skip 0 take 1;",
            "MD-12" => prefix + "with rows as (select r.Name, r.Payload from Records() r) select Name, Payload from rows order by Name;",
            _ => throw new ArgumentOutOfRangeException(nameof(caseId))
        };
    }

    private static void AssertMetadataRows(Table table, string caseId)
    {
        switch (caseId)
        {
            case "MD-01":
                TableMaterializationTestHelper.AssertRowsUnordered(table, ["Name"]);
                break;
            case "MD-02":
            case "MD-08":
                TableMaterializationTestHelper.AssertRowsUnordered(table, [12.50m]);
                break;
            case "MD-03":
            case "MD-10":
                TableMaterializationTestHelper.AssertRowsUnordered(table, ["payload"]);
                break;
            case "MD-04":
                TableMaterializationTestHelper.AssertRowsUnordered(table, ["Name", 12.50m, "payload", "plain"]);
                break;
            case "MD-05":
                TableMaterializationTestHelper.AssertRowsUnordered(table, ["Name", "plain"]);
                break;
            case "MD-07":
                TableMaterializationTestHelper.AssertRowsUnordered(table, ["Name"]);
                break;
            case "MD-06":
                TableMaterializationTestHelper.AssertRowsUnordered(table, ["Name", 12.50m]);
                break;
            case "MD-11":
                TableMaterializationTestHelper.AssertRowsUnordered(table, ["Name", 12.50m]);
                break;
            case "MD-09":
                TableMaterializationTestHelper.AssertRowsUnordered(table, ["Name", "plain"]);
                break;
            case "MD-12":
                TableMaterializationTestHelper.AssertRowsUnordered(table, ["Name", "payload"]);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(caseId));
        }
    }

    private static void AssertMetadataContract(ReadModifiersSchemaProvider provider, string caseId)
    {
        var columns = provider.Recorder.GetTableColumns
            .Concat(provider.Recorder.DescriptorColumns)
            .Concat(provider.Recorder.ExecutionColumns)
            .SelectMany(static item => item)
            .ToArray();
        Assert.IsNotEmpty(columns, caseId);

        var name = columns.FirstOrDefault(static column => column.ColumnName == "Name");
        if (name != null)
        {
            AssertModifier(name.ReadModifiers, ColumnReadModifiers.Encoding, "utf-8", caseId);
            AssertModifier(name.ReadModifiers, ColumnReadModifiers.Trim, "true", caseId);
            Assert.AreEqual(typeof(string), name.ColumnType, caseId);
            Assert.AreEqual(typeof(string), name.SourceReadType, caseId);
        }

        var amount = columns.FirstOrDefault(static column => column.ColumnName == "Amount");
        if (amount != null)
        {
            AssertModifier(amount.ReadModifiers, ColumnReadModifiers.Culture, "pl-PL", caseId);
            AssertModifier(amount.ReadModifiers, ColumnReadModifiers.Format, "#,##0.00", caseId);
            Assert.AreEqual(typeof(decimal?), amount.ColumnType, caseId);
            Assert.AreEqual(typeof(decimal?), amount.SourceReadType, caseId);
        }

        var payload = columns.FirstOrDefault(static column => column.ColumnName == "Payload");
        if (payload != null)
            AssertModifier(payload.ReadModifiers, "source.codec", "base64", caseId);
    }

    private static void AssertModifier(
        IReadOnlyDictionary<string, string> modifiers,
        string key,
        string expected,
        string caseId)
    {
        Assert.IsTrue(modifiers.TryGetValue(key, out var actual), $"{caseId}: missing modifier {key}");
        Assert.AreEqual(expected, actual, caseId);
    }

    private static string BuildCompatibilityQuery(string caseId)
    {
        var shape = caseId switch
        {
            "PC-01" or "PC-02" or "PC-03" or "PC-04" => "decimal",
            "PC-05" or "PC-06" or "PC-07" or "PC-08" => "int",
            _ => "DateTime"
        };
        return caseId switch
        {
            "PC-01" or "PC-05" or "PC-09" =>
                $"table RecordsShape {{ Value: {shape} }};couple #readmods.records with table RecordsShape as Records;select Value from Records();",
            "PC-02" or "PC-06" or "PC-10" =>
                $"table RecordsShape {{ Value: {shape} }};couple #readmods.records with table RecordsShape as Records;select r.Value from Records() r where r.Value is not null;",
            "PC-03" or "PC-07" or "PC-11" =>
                $"table RecordsShape {{ Value: {shape} }};couple #readmods.records with table RecordsShape as Records;with rows as (select r.Value from Records() r) select Value from rows;",
            _ =>
                $"table RecordsShape {{ Value: {shape} }};couple #readmods.records with table RecordsShape as Records;select Value from Records() order by Value;"
        };
    }

    private static IReadOnlyDictionary<string, object?> Row(params (string Name, object? Value)[] values)
    {
        return values.ToDictionary(static value => value.Name, static value => value.Value, StringComparer.Ordinal);
    }

    private static bool IsDynamicCase(string caseId) => int.Parse(caseId[3..]) % 2 == 0;

    private static int Variant(string caseId)
    {
        var value = int.Parse(caseId[3..]);
        return caseId.StartsWith("OR-", StringComparison.Ordinal) ? ((value - 1) % 12) + 1 : value;
    }
}
