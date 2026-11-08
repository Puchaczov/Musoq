using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class TextSwitchGeneralResultsTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    [DataRow("ab", "ab")]
    [DataRow("XYZ", "XYZ")]
    public void ScalarArms_ShouldReturnStringsDirectly(string input, string expected)
    {
        using var result = Run("text Rec { Content: switch { pattern '[a-z]' => chars[2], _ => rest } }", "p.Content", input);
        Assert.AreEqual(typeof(string), result.Columns.Single().ColumnType);
        TableMaterializationTestHelper.AssertRowsInOrder(result, [expected]);
    }

    [TestMethod]
    public void CaptureArms_ShouldExposeGroupsWithoutAWrapper()
    {
        using var result = Run("text Rec { Content: switch { pattern '[a-z]' => pattern '(?<Name>[a-z]+)' capture (Name), _ => pattern '(?<Name>[A-Z]+)' capture (Name) } }", "p.Content.Name, p.Content", "abc");
        Assert.AreEqual(typeof(string), result.Columns.First().ColumnType);
        Assert.AreEqual("abc", result[0].ReadValues()[0]);
        var record = result[0].ReadValues()[1]!;
        CollectionAssert.AreEqual(new[] { "Name" }, record.GetType().GetProperties().Select(property => property.Name).ToArray());
    }

    [TestMethod]
    public void MixedArrayAndScalarArms_ShouldPreserveTheSelectedArray()
    {
        using var result = Run("text Rec { Content: switch { pattern 'a' => repeat chars[1] until end, _ => rest } }", "p.Content", "abc");
        Assert.AreEqual(typeof(object), result.Columns.Single().ColumnType);
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, (string[])result[0].ReadValues()[0]!);
    }

    [TestMethod]
    public void NestedSwitch_ShouldReturnItsSelectedValueDirectly()
    {
        using var result = Run("text Rec { Content: switch { _ => switch { pattern 'a' => chars[1], _ => chars[2] } }, Tail: rest }", "p.Content, p.Tail", "abc");
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["a", "bc"]);
    }

    [TestMethod]
    [DataRow("switch { _ => optional literal 'x' }")]
    [DataRow("optional switch { pattern 'x' => literal 'x' }")]
    public void OptionalSwitchOrArm_ShouldRestoreItsCheckpoint(string type)
    {
        using var result = Run($"text Rec {{ Content: {type}, Tail: rest }}", "p.Content, p.Tail", "z");
        TableMaterializationTestHelper.AssertRowsInOrder(result, [null, "z"]);
    }

    [TestMethod]
    public void SchemaAndScalarArms_ShouldReturnNullForAnAbsentKnownMember()
    {
        using var result = Run("text Arm { Name: rest } text Rec { Content: switch { pattern 'a' => Arm, _ => rest } }", "p.Content.Name, p.Content", "z");
        TableMaterializationTestHelper.AssertRowsInOrder(result, [null, "z"]);
    }

    [TestMethod]
    public void NestedMixedMemberShapes_ShouldDispatchThroughEverySelectedRecord()
    {
        using var result = Run("text Item { Name: rest } text Other { Name: rest } text A { Payload: Item } text B { Payload: Other } text Rec { Content: switch { pattern 'a' => A, _ => B } }", "p.Content.Payload.Name", "abc");
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["abc"]);
    }

    [TestMethod]
    public void UnionMembers_ShouldSurviveCteProjection()
    {
        using var result = RunSql("text A { Name: rest } text B { Other: rest } text Rec { Content: switch { pattern 'a' => A, _ => B } } with c as (select p.Content as Value from #test.lines() e cross apply Parse<Rec>(e.Line) p) select c.Value.Name from c", "abc");
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["abc"]);
    }

    [TestMethod]
    public void CaptureMembersInsideRepeatedSwitches_ShouldRemainQueryable()
    {
        using var result = Run("text Rec { Content: repeat switch { pattern '[a-z]' => pattern '(?<Letter>[a-z])' capture (Letter), _ => pattern '(?<Letter>[A-Z])' capture (Letter) } until end }", "p.Content[0].Letter, p.Content[1].Letter", "aB");
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["a", "B"]);
    }

    [TestMethod]
    public void SchemaArraysSelectedBySwitch_ShouldKeepTypedIndexedMembers()
    {
        using var result = Run("text Item { Name: chars[1] } text Rec { Content: switch { _ => repeat Item until end } }", "p.Content[0].Name, p.Content[1].Name", "ab");
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["a", "b"]);
    }

    [TestMethod]
    [DataRow("Z", "Z")]
    [DataRow("a", null)]
    public void SwitchSelectingArraysOfSwitches_ShouldCombineAllElementMemberShapes(string input, string? expected)
    {
        const string schema = "text Rec { Content: switch { pattern '[a-z]' => repeat switch { pattern 'a' => pattern '(?<A>.)' capture (A), _ => pattern '(?<B>.)' capture (B) } until end, _ => repeat switch { pattern 'Z' => pattern '(?<C>.)' capture (C), _ => pattern '(?<D>.)' capture (D) } until end } }";
        using var result = Run(schema, "p.Content[0].C", input);
        TableMaterializationTestHelper.AssertRowsInOrder(result, [expected]);
    }

    [TestMethod]
    public void SchemaAndCaptureArrays_ShouldSerializeOnlySelectedPayloadMembers()
    {
        using var result = Run("text Arm { Name: rest } text Rec { Content: switch { _ => Arm } }", "ToJson(p.Content)", "abc");
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["{\"Name\":\"abc\"}"]);
        using var captures = Run("text Rec { Content: switch { _ => repeat pattern '(?<Letter>[a-z])' capture (Letter) until end } }", "ToJson(p.Content)", "ab");
        TableMaterializationTestHelper.AssertRowsInOrder(captures, ["[{\"Letter\":\"a\"},{\"Letter\":\"b\"}]"]);
    }

    [TestMethod]
    [DataRow("literal 'x'", "Content")]
    [DataRow("pattern '(?<Name>x)' capture (Name)", "Content")]
    [DataRow("Arm", "Content.Name")]
    public void SelectedArmFailure_ShouldIdentifyTheAuthoredField(string body, string errorField)
    {
        using var result = RunSql($"text Arm {{ Name: literal 'x' }} text Rec {{ Content: switch {{ _ => {body} }} }} select p.ErrorField, p.BytesConsumed, ToJson(p.ParsedFields) from #test.lines() e cross apply PartialParse<Rec>(e.Line) p", "z");
        TableMaterializationTestHelper.AssertRowsInOrder(result, [errorField, 0, "{}"]);
    }

    [TestMethod]
    public void MixedWholeValues_ShouldSerializeTheSelectedConcreteSchema()
    {
        using var result = Run("text A { Name: rest } text B { Other: rest } text Rec { Content: switch { pattern 'a' => A, _ => B } }", "ToJson(p.Content)", "z");
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["{\"Other\":\"z\"}"]);
    }

    [TestMethod]
    public void InheritedCaptureInSwitchSchemaArm_ShouldKeepItsConcreteShape()
    {
        using var result = Run("text Base { Data: pattern '(?<Name>[a-z]+)' capture (Name) } text A extends Base {} text B { Other: rest } text Rec { Content: switch { pattern 'a' => A, _ => B } }", "p.Content.Data.Name", "abc");
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["abc"]);
    }

    [TestMethod]
    public void RepetitionOfSwitches_ShouldRetainCaptureMemberShapes()
    {
        using var result = Run("text Rec { Content: repeat switch { pattern '[a-z]' => pattern '(?<Letter>[a-z])' capture (Letter), _ => pattern '(?<Letter>[A-Z])' capture (Letter) } until end }", "ToJson(p.Content)", "aB");
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["[{\"Letter\":\"a\"},{\"Letter\":\"B\"}]"]);
    }

    [TestMethod]
    [DataRow("pattern '(?<Name>a)' capture (Missing)", DiagnosticCode.MQ4002_InvalidTextSchemaField)]
    [DataRow("repeat chars[1] nested until end", DiagnosticCode.MQ4002_InvalidTextSchemaField)]
    [DataRow("Missing", DiagnosticCode.MQ4003_UndefinedSchemaReference)]
    public void InvalidUnselectedArm_ShouldFailCompilation(string body, DiagnosticCode expected)
    {
        var error = Assert.Throws<MusoqQueryException>(() => Run($"text Rec {{ Content: switch {{ pattern 'x' => rest, _ => {body} }} }}", "p.Content", "xyz"));
        Assert.AreEqual(expected, error.PrimaryEnvelope.Code);
    }

    private Table Run(string schemas, string projection, string input) => RunSql($"{schemas} select {projection} from #test.lines() e cross apply Parse<Rec>(e.Line) p", input);

    private Table RunSql(string sql, string input)
    {
        var provider = new TextSchemaProvider(new Dictionary<string, IEnumerable<TextEntity>> { ["#test"] = [new TextEntity { Text = input }] });
        using var compiled = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), provider, LoggerResolver, TestCompilationOptions);
        return TableMaterializationTestHelper.Materialize(compiled.Run());
    }
}
