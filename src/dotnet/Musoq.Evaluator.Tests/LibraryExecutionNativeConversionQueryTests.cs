using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LibraryExecutionNativeConversionQueryTests : GenericEntityTestBase
{
    internal const string Query = "select Position, Amount::int as Rounded, Amount::double as Floating, " +
        "Amount::string as Text from #schema.first() order by Position";

    [TestMethod]
    public void NumericCasts_ShouldPreserveNullableRoundingAndInvariantValues()
    {
        using var query = CreateAndRunVirtualMachine(Query,
            new Input[] { new(1, 1.5m), new(2, 2.5m), new(3, -1.5m), new(4, null) });
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table,
            ("Position", typeof(int)), ("Rounded", typeof(int?)), ("Floating", typeof(double?)), ("Text", typeof(string)));
        var rows = table.Select(row => (row.GetValue<int>(0), row.GetValue<int?>(1),
            row.GetValue<double?>(2), row.GetValue<string?>(3))).ToArray();
        CollectionAssert.AreEqual(new (int, int?, double?, string?)[]
            { (1, 2, 1.5, "1.5"), (2, 2, 2.5, "2.5"), (3, -2, -1.5, "-1.5"), (4, null, null, null) }, rows);
    }

    [TestMethod]
    public void UnsupportedConcreteCast_ShouldReportBindingDiagnosticInsteadOfAnObjectFallback()
    {
        const string sql = "select Payload::int as Value from #schema.first()";
        var error = Assert.Throws<Musoq.Converter.Exceptions.MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(sql, new[] { new UnsupportedInput(new Payload(1)) }));
        Assert.AreEqual(Musoq.Parser.Diagnostics.DiagnosticCode.MQ3027_InvalidExpressionType, error.PrimaryEnvelope.Code);
        Assert.Contains("no concrete conversion operation", error.Message);
    }

    public sealed record Payload(int Value);
    public sealed record UnsupportedInput(Payload Payload);
    public sealed record Input(int Position, decimal? Amount);
}
