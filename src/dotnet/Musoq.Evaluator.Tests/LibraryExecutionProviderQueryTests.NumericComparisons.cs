using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionProviderQueryTests
{
    [TestMethod]
    public void DecimalDoubleComparisons_ShouldUseTypedConversionAndExcludeNonFiniteOrOutOfRangeValues()
        => AssertFloatingDecimalComparisons<double>();

    [TestMethod]
    public void DecimalSingleComparisons_ShouldUseTypedConversionAndExcludeNonFiniteOrOutOfRangeValues()
        => AssertFloatingDecimalComparisons<float>();

    private void AssertFloatingDecimalComparisons<T>() where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        FloatingDecimalComparisonRow<T>[] rows =
        [
            new(1, null, 1.25m), new(2, T.NaN, 1.25m),
            new(3, T.PositiveInfinity, 1.25m), new(4, T.NegativeInfinity, 1.25m),
            new(5, T.MaxValue, 1.25m), new(6, T.CreateChecked(2), 1.25m),
            new(7, T.CreateChecked(1.25m), 1.25m), new(8, T.CreateChecked(-2), 1.25m)
        ];
        AssertFloatingDecimalQuery(rows, "Value > Amount", 6, T.CreateChecked(2));
        AssertFloatingDecimalQuery(rows, "Amount < Value", 6, T.CreateChecked(2));
        AssertFloatingDecimalQuery(rows, "Value = Amount", 7, T.CreateChecked(1.25m));
        AssertFloatingDecimalQuery(rows, "Amount = Value", 7, T.CreateChecked(1.25m));
    }

    private void AssertFloatingDecimalQuery<T>(FloatingDecimalComparisonRow<T>[] rows,
        string predicate, int expectedPosition, T expectedValue) where T : struct
    {
        var sql = $"select Position, Value from #schema.first() where {predicate} order by Position";
        using var compiled = CreateAndRunVirtualMachine(sql, rows);
        using var table = compiled.Run(TestContext.CancellationToken);
        var actual = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Value", typeof(T?)));
        Assert.HasCount(1, actual);
        Assert.AreEqual(expectedPosition, actual[0].GetValue<int>(0));
        Assert.AreEqual((T?)expectedValue, actual[0].GetValue<T?>(1));
    }

    [TestMethod]
    public void ErasedMixedNumericColumn_ShouldRejectItsShapeBeforeRowEvaluation()
    {
        object[] values = [(byte)50, 150, 250L, 350.5d, 450.75m];
        var rows = values.Select(value => (dynamic)new System.Collections.Generic.Dictionary<string, object>
            { ["Value"] = value, ["Name"] = "numeric" }).ToArray();
        const string sql = "table Items { Value: object, Name: string }; " +
            "couple #test.whatever with table Items as Items; select Name from Items() where Value > 100";
        var error = Assert.ThrowsExactly<global::Musoq.Converter.Exceptions.MusoqQueryException>(() =>
            global::Musoq.Converter.InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString("N"),
                new Schema.Unknown.UnknownSchemaProvider(rows), LoggerResolver, TestCompilationOptions));
        Assert.AreEqual(global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ3071_SourceContractError, error.PrimaryEnvelope.Code);
        Assert.Contains("Value", error.Message);
        Assert.Contains("object runtime type", error.Message);
    }

    public sealed record FloatingDecimalComparisonRow<T>(int Position, T? Value, decimal? Amount) where T : struct;
}
