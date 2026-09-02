using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.ReadModifiers;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class GeneratedCodeSpecificationExecutionTests
{
    [TestMethod]
    public void CoreNamedDatasourceArgumentsSample_WhenExecuted_ShouldBindReorderedNamedDefaultAndParameterArguments()
    {
        var sample = GeneratedCodeSamplesCatalog.GetByFileName("Q277_SpecCoreNamedDatasourceArguments.cs");
        using var compiled = Compile(sample, sample.CreateSchemaProvider());

        var table = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("d.Value", typeof(int)),
            ("d.First", typeof(string)),
            ("d.Second", typeof(int)),
            ("p.Value", typeof(int)),
            ("p.First", typeof(string)),
            ("p.Second", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [1, "parameter", 4, 1, "positional", 7]);
    }

    [TestMethod]
    public void TableCoupleArgumentsSample_WhenExecuted_ShouldBindPositionalNamedDefaultParameterAndCteArguments()
    {
        var sample = GeneratedCodeSamplesCatalog.GetByFileName("Q321_SpecTableCoupleArguments.cs");
        using var compiled = Compile(sample, sample.CreateSchemaProvider());

        var table = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("d.First", typeof(string)),
            ("d.Second", typeof(int)),
            ("p.First", typeof(string)),
            ("p.Second", typeof(int)),
            ("c.Text", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["positional", 7, "parameter", 4, "cte"]);
    }

    [TestMethod]
    public void TableCoupleSettingsProfilesSample_WhenExecuted_ShouldResolveEveryProfileDeterministically()
    {
        var sample = GeneratedCodeSamplesCatalog.GetByFileName("Q322_SpecTableSettingsProfiles.cs");
        using var compiled = Compile(sample, sample.CreateSchemaProvider());

        var table = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Token", typeof(string)),
            ("b.Token", typeof(string)),
            ("c.Token", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["blue-token", "red-token", "green-token"]);
    }

    [TestMethod]
    public void TableReadModifiersSample_WhenExecuted_ShouldApplyEncodingTrimCultureFormatAndCodec()
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>
        {
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["InvoiceNo"] = Encoding.UTF8.GetBytes(" INV-1 "),
                ["CustomerName"] = " Jane ",
                ["Total"] = "1 234,50",
                ["Attachment"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("document"))
            }
        };
        var provider = new ReadModifiersSchemaProvider(
            rows,
            ReadModifiersValidationMode.LenientUnsupportedModifiers);
        var sample = GeneratedCodeSamplesCatalog.GetByFileName("Q320_SpecTableReadModifiers.cs");
        using var compiled = Compile(sample, provider);

        var table = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("InvoiceNo", typeof(string)),
            ("CustomerName", typeof(string)),
            ("Total", typeof(decimal?)),
            ("Attachment", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["INV-1", "Jane", 1234.50m, "document"]);
    }

    [TestMethod]
    public void TableCoupleCompositionSample_WhenExecuted_ShouldComposeCteApplyJoinAggregationAndUnion()
    {
        var sample = GeneratedCodeSamplesCatalog.GetByFileName("Q323_SpecTableCoupleComposition.cs");
        using var compiled = Compile(sample, sample.CreateSchemaProvider());

        var table = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["alpha", 10m],
            ["beta", 20m]);
    }

    private static CompiledQuery Compile(GeneratedCodeSample sample, ISchemaProvider provider)
    {
        return InstanceCreator.CompileForExecution(
            sample.Query,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            sample.CompilationOptions);
    }
}
