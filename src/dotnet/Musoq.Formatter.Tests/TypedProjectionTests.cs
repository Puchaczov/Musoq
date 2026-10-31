using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Formatter.Tests.Verification;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Formatter.Tests;

[TestClass]
public sealed class TypedProjectionTests
{
    [TestMethod]
    [DataRow("B", "UB")]
    [DataRow("S", "US")]
    [DataRow("I", "UI")]
    [DataRow("L", "UL")]
    [DataRow("I", "L")]
    public void EqualNumericValuesWithDifferentKinds_ShouldRemainDistinct(string first, string second)
    {
        Assert.AreNotEqual(AstProjector.Snapshot(new IntegerNode("1", first)),
            AstProjector.Snapshot(new IntegerNode("1", second)));
    }

    [TestMethod]
    public void ClrReturnTypes_ShouldRemainDistinct()
    {
        var first = new AccessColumnNode("Name", "a", typeof(int), default);
        var second = new AccessColumnNode("Name", "a", typeof(long), default);

        Assert.AreNotEqual(AstProjector.Snapshot(first), AstProjector.Snapshot(second));
    }

    [TestMethod]
    public void IndependentParserNullMarkers_ShouldCompareEqually()
    {
        Assert.AreEqual(AstProjector.Snapshot(new NullNode()), AstProjector.Snapshot(new NullNode()));
    }

    [TestMethod]
    public void TypedNull_ShouldDifferFromParserNullMarker()
    {
        Assert.AreNotEqual(AstProjector.Snapshot(new NullNode(typeof(object))), AstProjector.Snapshot(new NullNode()));
    }

    [TestMethod]
    public void SourceLocations_ShouldNotAffectStructure()
    {
        Assert.AreEqual(AstProjector.Snapshot(new IntegerNode("1", "I", new TextSpan(1, 1))),
            AstProjector.Snapshot(new IntegerNode("1", "I", new TextSpan(100, 1))));
    }

    [TestMethod]
    public void UnsupportedScalar_ShouldFailExplicitly()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => AstProjector.Snapshot(new IntegerNode(new object())));
    }

    [TestMethod]
    public void UnreviewedDerivedNode_ShouldFailExplicitly()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => AstProjector.Snapshot(new UnreviewedInteger()));
    }

    [TestMethod]
    [DataRow("select case when 1 = 1 then 2 else 3 end from #s.rows()", "select case when 1 = 1 then 3 else 2 end from #s.rows()")]
    [DataRow("select 1, 2 from #s.rows()", "select 2, 1 from #s.rows()")]
    [DataRow("profile select Name from #s.rows()", "profile select Other from #s.rows()")]
    [DataRow("select Name from #s.rows() a", "select Name from #s.rows() b")]
    public void NestedStructureChanges_ShouldRemainDistinct(string first, string second)
    {
        Assert.AreNotEqual(AstSignature.Parse(first), AstSignature.Parse(second));
    }

    [TestMethod]
    public void Snapshot_ShouldRemainUnchangedWhenTheInputAstIsMutated()
    {
        var node = new AccessColumnNode("Name", "a", typeof(int), default);
        var snapshot = AstProjector.Snapshot(node);
        node.ChangeReturnType(typeof(long));

        Assert.AreEqual(AstProjector.Snapshot(new AccessColumnNode("Name", "a", typeof(int), default)), snapshot);
    }

    [TestMethod]
    public void DictionaryKeys_ShouldRemainSemantic()
    {
        Assert.AreNotEqual(AstProjector.Snapshot(TableWithModifierSpan("encoding", new TextSpan(1, 1))),
            AstProjector.Snapshot(TableWithModifierSpan("culture", new TextSpan(1, 1))));
    }

    [TestMethod]
    public void DictionarySourceRanges_ShouldRemainIgnorable()
    {
        Assert.AreEqual(AstProjector.Snapshot(TableWithModifierSpan("encoding", new TextSpan(1, 1))),
            AstProjector.Snapshot(TableWithModifierSpan("encoding", new TextSpan(100, 1))));
    }

    [TestMethod]
    [DataRow("binary Outer { Inner: { X: byte } }", "binary Outer { Inner: { X: short le } }")]
    [DataRow("table T { Name: string encoding 'utf-8' };", "table T { Name: string encoding 'ascii' };")]
    [DataRow("select (Name: 'a', Value: 1) from #s.rows()", "select (Name: 'a', Value: 2) from #s.rows()")]
    public void SchemaAndRecordChildren_ShouldRemainDistinct(string first, string second)
    {
        Assert.AreNotEqual(AstSignature.Parse(first), AstSignature.Parse(second));
    }

    private static CreateTableNode TableWithModifierSpan(string key, TextSpan span) => new("T",
        new[] { new CreateTableColumnDefinition("Name", "string", Array.Empty<CreateTableColumnModifier>())
        {
            ReadModifierSpans = new Dictionary<string, TextSpan> { [key] = span }
        } });

    private sealed class UnreviewedInteger() : IntegerNode("1", "I");
}
