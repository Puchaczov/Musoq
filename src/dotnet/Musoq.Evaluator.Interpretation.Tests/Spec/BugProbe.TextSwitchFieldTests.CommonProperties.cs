using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

public partial class BugProbeTextSwitchFieldTests
{
    [TestMethod]
    [DataRow("text A { Payload: rest } text B { Payload: rest }", "p.Variant.Payload", "value", typeof(string), "value")]
    [DataRow("text A { Payload: rest } text B { Other: rest }", "p.Variant.Payload", "value", typeof(object), null)]
    [DataRow("text Base { Payload: rest } text A extends Base {} text B extends Base {}", "p.Variant.Payload", "value", typeof(string), "value")]
    [DataRow("text Base { Payload: chars[1] } text A extends Base { Payload: rest } text B { Payload: rest }", "p.Variant.Payload", "value", typeof(string), "value")]
    [DataRow("text Item { Value: rest } text A { Payload: Item } text B { Payload: Item }", "p.Variant.Payload.Value", "value", typeof(string), "value")]
    public void TextSwitch_CommonPropertyInferenceIncludesDefaultAndInheritedArms(
        string schemas, string projection, string input, Type expectedType, string? expectedValue)
    {
        var query = schemas + " text Packet { Variant: switch { pattern 'A' => A, _ => B } } " +
                    $"select {projection} as Result from #test.entities() e cross apply Parse<Packet>(e.Text) p";
        var sources = new Dictionary<string, IEnumerable<TextEntity>>
        {
            ["#test"] = [new TextEntity { Name = "input", Text = input }]
        };
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new TextSchemaProvider(sources), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result, ("Result", expectedType));
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(expectedValue, result[0].ReadValues()[0]);
    }

    [TestMethod]
    [DataRow("text Item { Value: rest } text A { Payload: rest } text B { Payload: Item }")]
    [DataRow("text Item { Value: rest } text OtherItem { Value: rest } text A { Payload: Item } text B { Payload: OtherItem }")]
    public void TextSwitch_IncompatiblePropertyShapes_ShouldReturnTheSelectedPayload(string schemas)
    {
        var query = $"{schemas} text Packet {{ Variant: switch {{ pattern 'A' => A, _ => B }} }} " +
                    "select p.Variant.Payload as Result from #test.entities() e cross apply Parse<Packet>(e.Text) p";
        var sources = new Dictionary<string, IEnumerable<TextEntity>>
        {
            ["#test"] = [new TextEntity { Name = "input", Text = "value" }]
        };
        using var compiled = InstanceCreator.CompileForExecution(query,
            Guid.NewGuid().ToString(), new TextSchemaProvider(sources), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        Assert.AreEqual(typeof(object), result.Columns.Single().ColumnType);
        var selected = result[0].ReadValues()[0]!;
        Assert.AreEqual("value", selected.GetType().GetProperty("Value")!.GetValue(selected));
    }

    [TestMethod]
    public void TextSwitch_WhenPropertyIsNotDeclaredByAnyArm_ShouldRejectTheQuery()
    {
        const string query = "text A { Payload: rest } text B { Other: rest } text Packet { Variant: switch { pattern 'A' => A, _ => B } } select p.Variant.Unknown from #test.entities() e cross apply Parse<Packet>(e.Text) p";
        var sources = new Dictionary<string, IEnumerable<TextEntity>>
        {
            ["#test"] = [new TextEntity { Name = "input", Text = "value" }]
        };
        var error = Assert.Throws<MusoqQueryException>(() => InstanceCreator.CompileForExecution(query,
            Guid.NewGuid().ToString(), new TextSchemaProvider(sources), LoggerResolver, TestCompilationOptions));
        Assert.AreEqual(DiagnosticCode.MQ3071_SourceContractError, error.PrimaryEnvelope.Code);
        Assert.Contains("Unknown", error.Message);
    }

    [TestMethod]
    public void TextSwitch_WholeValueProjectionPreservesConcreteResultType()
    {
        const string query = "text Arm { Payload: rest } text Packet { Variant: switch { _ => Arm } } " +
                             "select p.Variant as Variant from #test.entities() e cross apply Parse<Packet>(e.Text) p";
        var sources = new Dictionary<string, IEnumerable<TextEntity>>
        {
            ["#test"] = [new TextEntity { Name = "input", Text = "value" }]
        };
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new TextSchemaProvider(sources), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();

        Assert.AreEqual("Arm", result.Columns.Single().ColumnType.Name);
        Assert.AreEqual(1, result.Count);
        var value = result[0].ReadValues()[0]!;
        Assert.AreEqual(result.Columns.Single().ColumnType, value.GetType());
        Assert.AreEqual("value", value.GetType().GetProperty("Payload")!.GetValue(value));
    }

    [TestMethod]
    public void TextSwitch_IdenticallyNamedFieldsInIndependentSchemasKeepTheirOwnMetadata()
    {
        const string query = """
            text StringArm { Payload: rest }
            text MissingArm { Other: rest }
            text StringPacket { Variant: switch { pattern 'A' => StringArm, _ => StringArm } }
            text MixedPacket { Variant: switch { pattern 'A' => StringArm, _ => MissingArm } }
            select s.Variant.Payload as Typed, m.Variant.Payload as Dynamic
            from #test.entities() e
            cross apply Parse<StringPacket>(e.Text) s
            cross apply Parse<MixedPacket>(e.Text) m
            """;
        var sources = new Dictionary<string, IEnumerable<TextEntity>>
        {
            ["#test"] = [new TextEntity { Name = "input", Text = "value" }]
        };
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new TextSchemaProvider(sources), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result, ("Typed", typeof(string)), ("Dynamic", typeof(object)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { "value", null });
    }
}
