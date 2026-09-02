using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Unknown;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticBinary041NestedArrayComputedTests : BinaryOrTextualEvaluatorTestBase
{
    private static readonly CompilationOptions CompilationOptions =
        new(usePrimitiveTypeValidation: false);

    [TestMethod]
    public void BinaryInterpretation_NestedPrimitiveSchemaAndStringArrays_WithConditionalStrings_ShouldPreserveBoundaries()
    {
        const string query = @"
            binary Inner {
                Value: short le
            };
            binary Packet {
                Count: byte,
                Items: Inner[Count],
                Values: short le[Count],
                Names: string[2] ascii[Count],
                HasExtra: byte,
                OptionalItems: Inner[Count] when HasExtra <> 0,
                OptionalNames: string[2] ascii[Count] when HasExtra <> 0,
                OptionalValue: short le when HasExtra <> 0,
                ComputedTotal: Count + 1,
                ComputedAgain: ComputedTotal + Count,
                ComputedFlag: HasExtra <> 0,
                Tail: byte
            };
            select
                p.Count,
                p.Items[0].Value,
                p.Values[1],
                p.Names[1],
                p.OptionalItems,
                p.OptionalNames,
                p.OptionalValue,
                p.ComputedTotal,
                p.ComputedAgain,
                p.ComputedFlag,
                p.Tail
            from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var entities = new[]
        {
            new BinaryEntity { Name = "without-extra.bin", Content = CreatePacket(false, 0xEE) },
            new BinaryEntity { Name = "with-extra.bin", Content = CreatePacket(true, 0xEF) }
        };

        var table = CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
                {
                    ["#test"] = entities
                }),
                LoggerResolver,
                CompilationOptions)
            .Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);

        Assert.AreEqual((byte)2, table[0][0]);
        Assert.AreEqual((short)1, table[0][1]);
        Assert.AreEqual((short)4, table[0][2]);
        Assert.AreEqual("CD", table[0][3]);
        Assert.IsNull(table[0][4]);
        Assert.IsNull(table[0][5]);
        Assert.IsNull(table[0][6]);
        Assert.AreEqual(3, table[0][7]);
        Assert.AreEqual(5, table[0][8]);
        Assert.AreEqual(false, table[0][9]);
        Assert.AreEqual((byte)0xEE, table[0][10]);

        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual((short)1, table[1][1]);
        Assert.AreEqual((short)4, table[1][2]);
        Assert.AreEqual("CD", table[1][3]);
        var optionalItems = (object[])table[1][4];
        Assert.HasCount(2, optionalItems);
        Assert.AreEqual((short)5, optionalItems[0].GetType().GetProperty("Value")!.GetValue(optionalItems[0]));
        Assert.AreEqual("XY", ((string[])table[1][5])[0]);
        Assert.AreEqual((short)0x1234, table[1][6]);
        Assert.AreEqual(3, table[1][7]);
        Assert.AreEqual(5, table[1][8]);
        Assert.AreEqual(true, table[1][9]);
        Assert.AreEqual((byte)0xEF, table[1][10]);
    }

    [TestMethod]
    public void BinaryInterpretation_NullConditionalSize_ShouldProduceEmptyArrayAndKeepTailBoundary()
    {
        const string query = @"
            binary NullSized {
                HasData: byte,
                Length: int le when HasData <> 0,
                Data: byte[Length],
                AdjustedLength: Length + 1,
                Tail: byte
            };
            select p.Length, p.Data, p.AdjustedLength, p.Tail
            from #test.files() f
            cross apply Interpret<NullSized>(f.Content) p";

        var table = CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
                {
                    ["#test"] = [new BinaryEntity { Name = "null-sized.bin", Content = [0, 0x7A] }]
                }),
                LoggerResolver,
                CompilationOptions)
            .Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
        Assert.IsEmpty((byte[])table[0][1]);
        Assert.IsNull(table[0][2]);
        Assert.AreEqual((byte)0x7A, table[0][3]);
    }

    [TestMethod]
    public void BinaryInterpretation_ZeroLengthPrimitiveSchemaAndStringArrays_ShouldKeepTailBoundary()
    {
        const string query = @"
            binary Item {
                Value: byte
            };
            binary EmptyCollections {
                Count: byte,
                Items: Item[Count],
                Values: short le[Count],
                Names: string[2] ascii[Count],
                Tail: byte
            };
            select p.Items, p.Values, p.Names, p.Tail
            from #test.files() f
            cross apply Interpret<EmptyCollections>(f.Content) p";

        var table = CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
                {
                    ["#test"] = [new BinaryEntity { Name = "empty-collections.bin", Content = [0, 0x7B] }]
                }),
                LoggerResolver,
                CompilationOptions)
            .Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.IsEmpty((object[])table[0][0]);
        Assert.IsEmpty((short[])table[0][1]);
        Assert.IsEmpty((string[])table[0][2]);
        Assert.AreEqual((byte)0x7B, table[0][3]);
    }

    [TestMethod]
    public void BinaryInterpretation_ForwardFieldReferences_ShouldReportStructuredDiagnostic()
    {
        const string query =
            "binary Packet { Data: byte[Later], Later: byte };" +
            "select 1 from #test.files();";

        var result = new QueryAnalyzer(
                new UnknownSchemaProvider(Array.Empty<dynamic>()),
                compilationOptions: CompilationOptions)
            .Analyze(query);

        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            "binary size expression referencing a later field");
        var expectedSpan = new TextSpan(query.IndexOf("Later", StringComparison.Ordinal), "Later".Length);

        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsNotEmpty(envelope.Actions);
    }

    [TestMethod]
    public void BinaryInterpretation_ForwardSchemaReference_ShouldFailBeforeCodeGeneration()
    {
        const string query =
            "binary Outer { Child: Inner };" +
            "binary Inner { Value: byte };" +
            "select 1 from #test.files();";

        var result = new QueryAnalyzer(
                new UnknownSchemaProvider(Array.Empty<dynamic>()),
                compilationOptions: CompilationOptions)
            .Analyze(query);

        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            "binary schema reference declared after its use");
        var expectedSpan = new TextSpan(query.IndexOf("Inner", StringComparison.Ordinal), "Inner".Length);
        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);

        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsNotEmpty(envelope.Actions);
    }

    private static byte[] CreatePacket(bool includeExtra, byte tail)
    {
        var data = new List<byte> { 2 };
        data.AddRange([1, 0, 2, 0]);
        data.AddRange([3, 0, 4, 0]);
        data.AddRange("ABCD"u8.ToArray());
        data.Add(includeExtra ? (byte)1 : (byte)0);

        if (includeExtra)
        {
            data.AddRange([5, 0, 6, 0]);
            data.AddRange("XYZZ"u8.ToArray());
            data.AddRange([0x34, 0x12]);
        }

        data.Add(tail);
        return data.ToArray();
    }
}
