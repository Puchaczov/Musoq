using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class IntermediateProducerIdentityTests
{
    [TestMethod]
    [DataRow("ab", "a", "b", false)]
    [DataRow("AB", "a", "b", false)]
    [DataRow("Ab", "A", "B", false)]
    [DataRow("decoded", "a", "b", false)]
    [DataRow("ab", "x", "b", false)]
    [DataRow("abc", "a", "b", true)]
    [DataRow("ABC", "A", "b", true)]
    public void CteName_ShouldNotIdentifyAnIntermediateProducer(string name, string first, string second, bool chained)
    {
        var projection = chained ? "c.Value" : second + ".Value";
        var extra = chained ? " cross apply Split(" + second + ".Value, ',') c" : string.Empty;
        var sql = $"with {name} as (select {projection} as V from values {{ (Dummy: 'x') }} {first} cross apply Split('x', ',') {second}{extra}) select z.V from {name} z";
        using var query = Compile(sql);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["x"]);
    }

    [TestMethod]
    [DataRow("with ab as (select b.Value as V from values { (Dummy: 'x') } a cross apply Split('x', ',') b), later as (select q.V from ab q) select z.V from later z")]
    [DataRow("with ab as (select b.Value as V from values { (Dummy: 'x') } a cross apply Split('x', ',') b), abc as (select c.Value as V from values { (Dummy: 'x') } a cross apply Split('x', ',') b cross apply Split(b.Value, ',') c) select r.V from ab l inner join abc r on l.V = r.V")]
    [DataRow("with ab as (select b.Value as V from values { (Dummy: 'x') } a cross apply Split('x', ',') b), __musoqIntermediate0 as (select q.V from ab q) select z.V from __musoqIntermediate0 z")]
    [DataRow("with AB as (select b.Value as V from values { (Dummy: 'x') } a cross apply Split('x', ',') b), ab__producer0 as (select q.V from AB q) select z.V from ab__producer0 z")]
    public void IndependentProducerScopes_ShouldKeepLegalCteNames(string sql)
    {
        using var query = Compile(sql);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["x"]);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void InterpretedProducer_ShouldKeepIdentityAcrossCteProjection(bool byteArrayCte)
    {
        var sql = InterpretedSql(byteArrayCte);
        using var query = Compile(sql, byteArrayCte);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)1]);
    }

    [TestMethod]
    public void ByteArrayCte_ShouldKeepPrimitiveOutputValidation()
    {
        var error = Assert.ThrowsExactly<MusoqQueryException>(() => Compile(InterpretedSql(true)));
        Assert.Contains("MQ3027", error.Message);
    }

    [TestMethod]
    public void CollidingNames_ShouldKeepTypedExecutionWithoutBoxing()
    {
        using var query = Compile(InterpretedSql(false));
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)1]);
        Assert.IsFalse(GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query)
            .Any(GeneratedExecutionIlInspection.ContainsBoxOpcode));
    }

    private static string InterpretedSql(bool byteArrayCte) => "binary R { A: byte } with " +
        (byteArrayCte ? "c as (select FromHexToBytes('01') as Bytes from values { (Dummy: 'x') } d), " : string.Empty) +
        "ab as (select b.A as A from " + (byteArrayCte ? "c a" : "values { (Dummy: 'x') } a") +
        " cross apply Interpret<R>(" + (byteArrayCte ? "a.Bytes" : "FromHexToBytes('01')") + ") b) select z.A from ab z";

    private static CompiledQuery Compile(string sql, bool complex = false) => InstanceCreator.CompileForExecution(sql,
        Guid.NewGuid().ToString(), new TestSchemaProvider(), new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: !complex));
}
