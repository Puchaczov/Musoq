using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class NullableBase64BindingTests
{
    [TestMethod]
    public void NullableSliceWrapper_ShouldNotBoxValueArguments()
    {
        var method = typeof(LibraryBase).GetMethod(nameof(LibraryBase.ToBase64), [typeof(byte[]), typeof(int?), typeof(int?)])!;
        Assert.IsFalse(GeneratedExecutionIlInspection.ContainsBoxOpcode(method));
    }

    [TestMethod]
    [DataRow("0102", "AQI=")]
    [DataRow(null, null)]
    public void NullableSliceArithmetic_ShouldBindTheNullableOverloadAndPreserveNulls(string? hex, string? expected)
    {
        var provider = LibraryExecutionFixture.CreateProvider<BinaryEntity, LibraryBase>(
            [new BinaryEntity { Content = hex == null ? null! : Convert.FromHexString(hex) }]);
        using var query = InstanceCreator.CompileForExecution(
            "select ToBase64(f.Content, 0, Length(f.Content) - 0) as Encoded from #schema.first() f",
            Guid.NewGuid().ToString(), provider, new TestsLoggerResolver());
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { expected });
    }
}
