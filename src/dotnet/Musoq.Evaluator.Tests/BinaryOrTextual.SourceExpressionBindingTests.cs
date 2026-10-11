using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualSchemaFeaturesTests
{
    [TestMethod]
    [DataRow("FromHexToBytes(b.Name)")]
    [DataRow("FromHexToBytes(ToHex(b.Content))")]
    public void FirstApply_RowDependentConversion_UsesEachLeftRow(string input)
    {
        var query = $$"""
            binary Header { Value: int le };
            select h.Value as Value
            from #test.bytes() b
            cross apply Interpret<Header>({{input}}) h
            order by h.Value
            """;
        var provider = CreateBinaryProvider(
            new BinaryEntity { Name = "2A000000", Content = [0x2A, 0, 0, 0] },
            new BinaryEntity { Name = "2B000000", Content = [0x2B, 0, 0, 0] });

        var compiled = CompileGeneratedQuery(
            query, Guid.NewGuid().ToString(), provider, LoggerResolver, TestCompilationOptions);
        var table = compiled.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(42, table[0].ReadValue(0));
        Assert.AreEqual(43, table[1].ReadValue(0));
    }

    [TestMethod]
    public void FirstApply_FunctionSourceWithConversion_ExpandsBothLines()
    {
        const string query = """
            select line.Value as Value
            from #test.lines() t
            cross apply Lines(FromHexToString('34320A3433')) line
            order by line.Value
            """;
        var provider = CreateTextProvider(new TextEntity { Name = "sample", Text = "unused" });

        var compiled = CompileGeneratedQuery(
            query, Guid.NewGuid().ToString(), provider, LoggerResolver, TestCompilationOptions);
        var table = compiled.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("42", table[0].ReadValue(0));
        Assert.AreEqual("43", table[1].ReadValue(0));
    }

    [TestMethod]
    [DataRow("", "values { (Hex: '2A000000') } v", "v.Hex")]
    [DataRow("with data as (select b.Name as Hex from #test.bytes() b)", "data d", "d.Hex")]
    public void FirstApply_ValuesOrCteLeftSource_ResolvesConversion(string declaration, string source, string hex)
    {
        var query = $$"""
            binary Header { Value: int le };
            {{declaration}}
            select h.Value as Value
            from {{source}}
            cross apply Interpret<Header>(FromHexToBytes({{hex}})) h
            """;
        var provider = CreateBinaryProvider(new BinaryEntity { Name = "2A000000", Content = [0x2A, 0, 0, 0] });

        var compiled = CompileGeneratedQuery(
            query, Guid.NewGuid().ToString(), provider, LoggerResolver, TestCompilationOptions);
        var table = compiled.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0].ReadValue(0));
    }

    [TestMethod]
    [DataRow("cross apply Interpret<Header>(b.Content) previous")]
    [DataRow("inner join #test.bytes() peer on b.Name = peer.Name")]
    public void ApplyAfterJoinOrApply_UsesVisibleLeftSources(string previousSource)
    {
        var query = $$"""
            binary Header { Value: int le };
            select h.Value as Value
            from #test.bytes() b
            {{previousSource}}
            cross apply Interpret<Header>(FromHexToBytes(b.Name)) h
            """;
        var provider = CreateBinaryProvider(new BinaryEntity { Name = "2A000000", Content = [0x2A, 0, 0, 0] });

        var compiled = CompileGeneratedQuery(
            query, Guid.NewGuid().ToString(), provider, LoggerResolver, TestCompilationOptions);
        var table = compiled.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0].ReadValue(0));
    }

    [TestMethod]
    [DataRow("Interpret", "FromHexToBytes('2A000000')", "cross apply")]
    [DataRow("Interpret", "b.Content", "cross apply")]
    [DataRow("Interpret", "b.FromHexToBytes('2A000000')", "cross apply")]
    [DataRow("TryInterpret", "FromHexToBytes('2A000000')", "outer apply")]
    [DataRow("InterpretAt", "FromHexToBytes('002A000000'), 1", "cross apply")]
    public void FirstApply_BinaryFunctionInput_ReturnsDecodedValue(string function, string arguments, string apply)
    {
        Assert.AreEqual(42, RunBinarySourceExpression(function, arguments, apply, "h.Value"));
    }

    [TestMethod]
    public void FirstApply_PartialInterpret_ReturnsDecodedField()
    {
        var fields = PartialPayloadAssertions.CompletedValues(RunBinarySourceExpression(
            "PartialInterpret", "FromHexToBytes('2A000000')", "cross apply", "h.ParsedFields")!);

        Assert.AreEqual(42, fields["Value"]);
    }

    [TestMethod]
    [DataRow("Parse", "cross apply", "FromHexToString('34323A')")]
    [DataRow("Parse", "cross apply", "'42:'")]
    [DataRow("TryParse", "outer apply", "FromHexToString('34323A')")]
    public void FirstApply_TextFunctionInput_ReturnsDecodedValue(string function, string apply, string input)
    {
        Assert.AreEqual("42", RunTextSourceExpression(function, apply, "r.Value", input));
    }

    [TestMethod]
    public void FirstApply_PartialParse_ReturnsDecodedField()
    {
        var fields = PartialPayloadAssertions.CompletedValues(RunTextSourceExpression(
            "PartialParse", "cross apply", "r.ParsedFields", "FromHexToString('34323A')")!);

        Assert.AreEqual("42", fields["Value"]);
    }

    private object? RunBinarySourceExpression(string function, string arguments, string apply, string field)
    {
        var query = $$"""
            binary Header { Value: int le };
            select {{field}} as Value
            from #test.bytes() b
            {{apply}} {{function}}<Header>({{arguments}}) h
            """;
        var provider = CreateBinaryProvider(new BinaryEntity { Name = "sample", Content = [0x2A, 0, 0, 0] });

        var compiled = CompileGeneratedQuery(
            query, Guid.NewGuid().ToString(), provider, LoggerResolver, TestCompilationOptions);
        var table = compiled.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        return table[0].ReadValue(0);
    }

    private object? RunTextSourceExpression(string function, string apply, string field, string input)
    {
        var query = $$"""
            text Record { Value: until ':' };
            select {{field}} as Value
            from #test.lines() t
            {{apply}} {{function}}<Record>({{input}}) r
            """;
        var provider = CreateTextProvider(new TextEntity { Name = "sample", Text = "42:" });

        var compiled = CompileGeneratedQuery(
            query, Guid.NewGuid().ToString(), provider, LoggerResolver, TestCompilationOptions);
        var table = compiled.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        return table[0].ReadValue(0);
    }

    private static BinarySchemaProvider CreateBinaryProvider(params BinaryEntity[] rows) =>
        new(new Dictionary<string, IEnumerable<BinaryEntity>> { ["#test"] = rows });

    private static TextSchemaProvider CreateTextProvider(params TextEntity[] rows) =>
        new(new Dictionary<string, IEnumerable<TextEntity>> { ["#test"] = rows });
}
