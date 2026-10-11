using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionNativeGenericQueryTests
{
    [TestMethod]
    [DataRow("param(items: (Label: string)[]) select UnmanagedElements($items) as Value from #schema.first()", "contains managed fields")]
    [DataRow("param(items: (Value: int)?[]) select StructElements($items) as Value from #schema.first()", "non-nullable value type")]
    [DataRow("select ReferenceValue((Value: Position)) as Value from #schema.first()", "generated record carrier is a value type")]
    public void GeneratedRecordConstraints_ShouldRejectTheActualCarrierShape(string sql, string reason)
    {
        var error = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<Input, GenericLibrary>(sql, [new(1)]));
        Assert.AreEqual(DiagnosticCode.MQ3088_NoMatchingCallableOverload, error.PrimaryEnvelope.Code);
        Assert.Contains(reason, error.Message);
    }

    [TestMethod]
    public void DescribeGeneratedGenericResult_ShouldHideCompilationConstraintWitnesses()
    {
        const string sql = "param(items: (Value: int)[]) desc query (select StructEcho($items) as Echo from #schema.first())";
        using var query = CreateAndRunVirtualMachine<Input, GenericLibrary>(sql, [new(1)]);
        using var table = query.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        Assert.HasCount(1, rows);
        Assert.AreEqual("Echo", rows[0].GetValue<string>(0));
        var text = rows[0].GetValue<string>(2);
        Assert.DoesNotContain("GeneratedRecordConstraintWitness", text);
        Assert.AreEqual("(Value: int32)[]", text);
        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Index", typeof(int)), ("Type", typeof(string)));
    }

    [TestMethod]
    public void InlineRecordArgument_ShouldBindTheConcreteStructGenericParameter()
    {
        const string sql = "select NeedsStruct((Value: Position)) as Value from #schema.first()";
        using var query = CreateAndRunVirtualMachine<Input, GenericLibrary>(sql, [new(1), new(2)]);
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int)));
        CollectionAssert.AreEqual(new[] { 1, 1 }, table.Select(row => row.GetValue<int>(0)).ToArray());
    }
}
