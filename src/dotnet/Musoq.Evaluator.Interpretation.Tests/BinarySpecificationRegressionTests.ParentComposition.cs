using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    [DataRow("Body: { Values: byte[parent.Count] }")]
    [DataRow("Body: substream[Count] as Child exact")]
    public void ParentContextThroughInlineAndSubstream_ShouldReadSameBytes(string body)
    {
        var table = RunBinaryQuery($"binary Child {{ Values: byte[parent.Count] }}; binary Rec {{ Count: byte, {body}, Tail: byte }}; select i.Value, r.Tail from #test.files() f cross apply Interpret<Rec>(f.Content) r cross apply r.Body.Values i", [2, 10, 20, 99]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)10, (byte)99], [(byte)20, (byte)99]);
    }

    [TestMethod]
    public void ParentDependentArrayAndRepeatElements_ShouldUseContainingRecord()
    {
        var table = RunBinaryQuery("binary Child { Values: byte[parent.Count] }; binary Rec { Count: byte, Items: Child[2], Rest: Child repeat until eof }; select i.Value from #test.files() f cross apply Interpret<Rec>(f.Content) r cross apply r.Items c cross apply c.Values i", [1, 10, 20, 30]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)10], [(byte)20]);
    }

    [TestMethod]
    public void ParentDependentGenericAndInheritedSchemas_ShouldUseContext()
    {
        var table = RunBinaryQuery("binary Item { V: byte }; binary Base { Flag: byte }; binary Body<T> extends Base { Values: T[parent.Count] }; binary Rec { Count: byte, Body: Body<Item> }; select r.Body.Flag, i.V from #test.files() f cross apply Interpret<Rec>(f.Content) r cross apply r.Body.Values i", [1, 7, 10]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)7, (byte)10]);
    }

    [TestMethod]
    public void ContextualGenericElement_ShouldReceiveGenericContainerCount()
    {
        var table = RunBinaryQuery("binary Item { Values: byte[parent.Count] }; binary Body<T> { Count: byte, Items: T[1] }; binary Rec { Body: Body<Item> }; select v.Value from #test.files() f cross apply Interpret<Rec>(f.Content) r cross apply r.Body.Items i cross apply i.Values v", [2, 10, 20]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)10], [(byte)20]);
    }

    [TestMethod]
    public void ContextualElementThroughNestedGenericContainer_ShouldReceiveImmediateRecord()
    {
        var table = RunBinaryQuery("binary Item { Values: byte[parent.Count] }; binary Body<T> { Count: byte, Items: T[1] }; binary Wrapper<T> { Body: Body<T> }; binary Rec { Wrapper: Wrapper<Item> }; select v.Value from #test.files() f cross apply Interpret<Rec>(f.Content) r cross apply r.Wrapper.Body.Items i cross apply i.Values v", [2, 10, 20]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)10], [(byte)20]);
    }

    [TestMethod]
    public void RepeatedGenericSchemaAtDifferentDepths_ShouldBindEachContext()
    {
        var table = RunBinaryQuery("binary Item { Values: byte[parent.Count] }; binary Body<T> { Count: byte, Items: T[1] }; binary Rec { Body: Body<Body<Item>> }; select v.Value from #test.files() f cross apply Interpret<Rec>(f.Content) r cross apply r.Body.Items a cross apply a.Items i cross apply i.Values v", [2, 1, 10]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)10]);
    }

    [TestMethod]
    public void ReusedChildSchema_ShouldAcceptDifferentCompatibleParents()
    {
        var table = RunBinaryQuery("binary Child { Values: byte[parent.Count] }; binary LeftRecord { Count: byte, Body: Child }; binary RightRecord { Count: int le, Body: Child }; binary Rec { A: LeftRecord, B: RightRecord }; select a.Value, b.Value from #test.files() f cross apply Interpret<Rec>(f.Content) r cross apply r.A.Body.Values a cross apply r.B.Body.Values b", [1, 10, 1, 0, 0, 0, 20]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)10, (byte)20]);
    }

    [TestMethod]
    public void LocalParentField_ShouldRetainOrdinaryPathMeaning()
    {
        var table = RunBinaryQuery("binary Header { Count: byte }; binary Child { parent: Header, Values: byte[parent.Count] }; binary Rec { Body: Child }; select i.Value from #test.files() f cross apply Interpret<Rec>(f.Content) r cross apply r.Body.Values i", [1, 10]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)10]);
    }

    [TestMethod]
    public async Task ConcurrentParentDependentQueries_ShouldKeepContextsIndependent()
    {
        const string schema = "binary Child { V: byte when parent.Flag = 1 }; binary Rec { Flag: byte, Child: Child, Tail: byte };";
        var present = Task.Run(() => RunBinary(schema, "r.Child.V, r.Tail", [1, 7, 99]));
        var absent = Task.Run(() => RunBinary(schema, "r.Child.V, r.Tail", [0, 88]));
        await Task.WhenAll(present, absent);

        TableMaterializationTestHelper.AssertRowsInOrder(present.Result, [(byte)7, (byte)99]);
        TableMaterializationTestHelper.AssertRowsInOrder(absent.Result, [null, (byte)88]);
    }

    [TestMethod]
    public void StringParentSize_ShouldReportTypeDiagnostic()
    {
        var exception = Assert.Throws<MusoqQueryException>(() => RunBinary("binary Child { Values: byte[parent.Count] }; binary Rec { Count: string[1] ascii, Body: Child };", "r.Body.Values[0]", [49, 10]));

        StringAssert.Contains(exception.Message, "MQ4006");
    }
}
