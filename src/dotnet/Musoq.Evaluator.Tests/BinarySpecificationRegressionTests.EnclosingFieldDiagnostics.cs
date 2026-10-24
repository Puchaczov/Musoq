using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void ExplicitParentLen_ShouldParseTheDocumentedThreeByteFixture()
    {
        var table = RunBinary("binary Inner { Payload: byte[parent.Len] }; binary Rec { Len: byte, Body: Inner };",
            "r.Len, r.Body.Payload[0], r.Body.Payload[1]", [2, 10, 20]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)2, (byte)10, (byte)20]);
    }

    [TestMethod]
    public void NamedSchemaBareEnclosingField_ShouldExplainLocalScopeAndExplicitParent()
    {
        var exception = Assert.Throws<MusoqQueryException>(() => RunBinary(
            "binary Inner { Payload: byte[Len] }; binary Rec { Len: byte, Body: Inner };", "r.Len", [2, 10, 20]));
        Assert.Contains("MQ2030", exception.Message);
        Assert.Contains("local scope", exception.Message);
        Assert.Contains("parent.Len", exception.Message);
        Assert.DoesNotContain("before it is declared", exception.Message);
    }

    [TestMethod]
    [DataRow("binary Rec { Payload: byte[Len], Len: byte };")]
    [DataRow("binary Rec { Body: { Payload: byte[Len], Len: byte } };")]
    [DataRow("binary Rec { Body: { Payload: byte[Len] }, Len: byte };")]
    [DataRow("binary Rec { Body: { Nested: { Payload: byte[Len] }, Len: byte } };")]
    public void GenuineForwardFieldReference_ShouldRetainItsSpecificDiagnostic(string schema)
    {
        var exception = Assert.Throws<MusoqQueryException>(() => RunBinary(schema, "r.Body", [1]));
        Assert.Contains("before it is declared", exception.Message);
        Assert.DoesNotContain("parent.Len", exception.Message);
    }

    [TestMethod]
    public void AnonymousUnknownField_ShouldNotSuggestNamedSchemaEnclosingLookup()
    {
        var exception = Assert.Throws<MusoqQueryException>(() => RunBinary(
            "binary Rec { Body: { Payload: byte[Missing] } };", "r.Body", [1]));
        Assert.Contains("unknown field 'Missing'", exception.Message);
        Assert.DoesNotContain("parent.Missing", exception.Message);
    }

    [TestMethod]
    [DataRow("binary Rec { Other: { Len: byte }, Body: { Payload: byte[Len] } };")]
    [DataRow("binary Rec { Body: { Payload: byte[Len] }, Other: { Len: byte } };")]
    public void AnonymousUnknownField_ShouldIgnoreFieldsInUnrelatedSiblings(string schema)
    {
        var exception = Assert.Throws<MusoqQueryException>(() => RunBinary(schema, "r.Body", [1]));
        Assert.Contains("unknown field 'Len'", exception.Message);
        Assert.DoesNotContain("before it is declared", exception.Message);
    }

    [TestMethod]
    public void UnknownFieldAfterAnonymousSchema_ShouldRestoreNamedScopeGuidance()
    {
        var exception = Assert.Throws<MusoqQueryException>(() => RunBinary(
            "binary Rec { Body: { Value: byte }, Payload: byte[Missing] };", "r.Body", [1]));
        Assert.Contains("unknown field 'Missing'", exception.Message);
        Assert.Contains("parent.Missing", exception.Message);
    }

    [TestMethod]
    public void LocalLen_ShouldShadowAnEnclosingLen()
    {
        var table = RunBinary("binary Inner { Len: byte, Payload: byte[Len] }; binary Rec { Len: byte, Body: Inner, Tail: byte };",
            "r.Len, r.Body.Len, r.Body.Payload[0], r.Tail", [2, 1, 10, 99]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)2, (byte)1, (byte)10, (byte)99]);
    }

    [TestMethod]
    public void ExplicitParentLen_ShouldRemainDistinctFromALocalLen()
    {
        var table = RunBinary("binary Inner { Len: byte, Payload: byte[parent.Len] }; binary Rec { Len: byte, Body: Inner };",
            "r.Body.Len, r.Body.Payload[0], r.Body.Payload[1]", [2, 1, 10, 20]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)1, (byte)10, (byte)20]);
    }

    [TestMethod]
    public void ExplicitAncestorLen_ShouldFollowRecordNesting()
    {
        var table = RunBinary("binary Leaf { Payload: byte[parent.parent.Len] }; binary Inner { Body: Leaf }; binary Rec { Len: byte, Body: Inner };",
            "r.Body.Body.Payload[0], r.Body.Body.Payload[1]", [2, 10, 20]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)10, (byte)20]);
    }

    [TestMethod]
    public void NamedParentSizedSchema_ShouldBindEachEmbeddingIndependently()
    {
        var table = RunBinary("binary Inner { Payload: byte[parent.Len] }; binary Outer { Len: byte, Body: Inner }; binary Rec { Left: Outer, Right: Outer };",
            "r.Left.Body.Payload[0], r.Right.Body.Payload[0], r.Right.Body.Payload[1]", [1, 10, 2, 20, 30]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)10, (byte)20, (byte)30]);
    }

    [TestMethod]
    [DataRow("binary Rec { Payload: byte[parent.Len] };")]
    [DataRow("binary Inner { Payload: byte[parent.parent.Len] }; binary Rec { Len: byte, Body: Inner };")]
    [DataRow("binary Inner { Payload: byte[parent.Len] }; binary Rec { Body: Inner, Len: byte };")]
    public void UnavailableEnclosingFields_ShouldRemainSchemaDiagnostics(string schema)
    {
        var exception = Assert.Throws<MusoqQueryException>(() => RunBinary(schema, "r.Payload", [2, 10, 20]));
        Assert.Contains("MQ2030", exception.Message);
        Assert.DoesNotContain("MQ9001", exception.Message);
    }

    [TestMethod]
    public void ExplicitParentLen_ShouldEmitTypedAccessWithoutBoxingOrRuntimeDiscovery()
    {
        const string sql = "binary Inner { Payload: byte[parent.Len] }; binary Rec { Len: byte, Body: Inner }; " +
            "select r.Len, r.Body.Payload[0], r.Body.Payload[1] from #test.files() f cross apply Interpret<Rec>(f.Content) r";
        var provider = new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
            { ["#test"] = [new() { Content = [2, 10, 20] }] });
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(), provider, LoggerResolver);
        Assert.Contains("r.Body.Payload", inspection.GeneratedCSharpCode);
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), provider, LoggerResolver);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)2, (byte)10, (byte)20]);
        Assert.IsFalse(GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query).Any(GeneratedExecutionIlInspection.ContainsBoxOpcode));
    }
}
