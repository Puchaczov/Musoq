using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using ParserType = global::Musoq.Parser.Parser;

namespace Musoq.Converter.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ParsedQueryTemplateCacheTests
{
    public static IEnumerable<object[]> InterpretationSchemaScripts
    {
        get
        {
            yield return
            [
                "binary",
                "binary Packet { Length: byte, Payload: byte[Length] }; select 1 from #A.Entities()",
                1
            ];
            yield return
            [
                "text",
                "text Line { Key: until '=', Value: rest trim }; select 1 from #A.Entities()",
                1
            ];
            yield return
            [
                "text-captures",
                "text Coordinates { Value: pattern '(?<Lat>-?[0-9]+),(?<Lon>-?[0-9]+)' capture (Lat, Lon) }; select 1 from #A.Entities()",
                1
            ];
            yield return
            [
                "text-switch",
                "text Line { Content: switch { pattern 'header' => Header, _ => Value } }; select 1 from #A.Entities()",
                1
            ];
            yield return
            [
                "nested",
                "binary Point { X: float le, Y: float le }; binary Mesh { Count: byte, Points: Point[Count] }; select 1 from #A.Entities()",
                2
            ];
            yield return
            [
                "composed",
                "text Pair { Key: until ':', Value: rest trim }; binary Packet { Length: byte, Value: string[Length] utf8 as Pair }; select 1 from #A.Entities()",
                2
            ];
            yield return
            [
                "rich-binary",
                "binary Leaf { Value: byte }; binary Packet { Type: byte oneOf [1, 2], Length: byte, Signature: byte[2] magic [1, 2], Name: string[Length] utf8, Points: Leaf[2], Flags: bits[3], _: align[8], Inline: { Tag: byte, Value: short le }, Payload: switch Type { 1 => Structured: Leaf, _ => Raw: byte[Length] }, Body: substream[Length] as Leaf lax, Items: Leaf repeat until eof }; select 1 from #A.Entities()",
                2
            ];
        }
    }

    [TestInitialize]
    public void Initialize()
    {
        ParsedQueryTemplateCache.Clear();
    }

    [TestCleanup]
    public void Cleanup()
    {
        ParsedQueryTemplateCache.Clear();
    }

    [TestMethod]
    public async Task SameScript_WhenRequestedConcurrently_ShouldParseOnceAndClonePerCaller()
    {
        const string script = "select 1 from #A.Entities()";
        var parseCount = 0;

        var roots = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
            ParsedQueryTemplateCache.GetOrAdd(
                script,
                () =>
                {
                    Interlocked.Increment(ref parseCount);
                    Thread.Sleep(10);
                    return Parse(script);
                }))));

        Assert.AreEqual(1, parseCount);
        Assert.HasCount(32, roots.Distinct(ReferenceEqualityComparer.Instance));
        Assert.AreEqual(1, ParsedQueryTemplateCache.Snapshot.Count);
        Assert.AreEqual(31, ParsedQueryTemplateCache.Snapshot.Hits);
        Assert.AreEqual(1, ParsedQueryTemplateCache.Snapshot.Misses);
    }

    [TestMethod]
    [DynamicData(nameof(InterpretationSchemaScripts))]
    public async Task InterpretationSchemaScript_WhenReused_ShouldCoverColdHotRepeatedAndConcurrentCloning(
        string scenario,
        string script,
        int expectedSchemaCount)
    {
        _ = scenario;
        var parseCount = 0;

        RootNode GetRoot() => ParsedQueryTemplateCache.GetOrAdd(
            script,
            () =>
            {
                Interlocked.Increment(ref parseCount);
                return Parse(script);
            });

        var cold = GetRoot();
        var hot = GetRoot();
        var repeated = GetRoot();

        Assert.AreEqual(1, parseCount);
        Assert.AreEqual(1, ParsedQueryTemplateCache.Snapshot.Misses);
        Assert.AreEqual(2, ParsedQueryTemplateCache.Snapshot.Hits);
        AssertInterpretationSchemasAreIsolated(cold, hot, expectedSchemaCount);
        AssertInterpretationSchemasAreIsolated(hot, repeated, expectedSchemaCount);

        ParsedQueryTemplateCache.Clear();
        parseCount = 0;
        var concurrent = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(GetRoot)));

        Assert.AreEqual(1, parseCount);
        Assert.AreEqual(1, ParsedQueryTemplateCache.Snapshot.Misses);
        Assert.AreEqual(15, ParsedQueryTemplateCache.Snapshot.Hits);
        foreach (var root in concurrent.Skip(1))
            AssertInterpretationSchemasAreIsolated(concurrent[0], root, expectedSchemaCount);
    }

    [TestMethod]
    public void InterpretationSchemaScript_WhenOneCloneIsMutated_ShouldNotChangeCachedTemplateOrOtherClones()
    {
        const string script =
            "binary Packet { Length: byte, Payload: byte[Length] }; select 1 from #A.Entities()";

        var first = ParsedQueryTemplateCache.GetOrAdd(script, () => Parse(script));
        var second = ParsedQueryTemplateCache.GetOrAdd(script, () => throw new InvalidOperationException());
        var firstSchema = (BinarySchemaNode)GetSchemaDefinitions(first).Single();
        var secondSchema = (BinarySchemaNode)GetSchemaDefinitions(second).Single();

        firstSchema.Fields[0] = new ComputedFieldNode("Changed", new IntegerNode(1));

        var third = ParsedQueryTemplateCache.GetOrAdd(script, () => throw new InvalidOperationException());
        var thirdSchema = (BinarySchemaNode)GetSchemaDefinitions(third).Single();

        Assert.AreEqual("Length", secondSchema.Fields[0].Name);
        Assert.AreEqual("Length", thirdSchema.Fields[0].Name);
        Assert.AreNotSame(firstSchema.Fields, secondSchema.Fields);
        Assert.AreNotSame(firstSchema.Fields, thirdSchema.Fields);
    }

    [TestMethod]
    public async Task SameScriptWithDiagnostics_WhenRequestedConcurrently_ShouldReplayOneWarningPerCaller()
    {
        const string script = @"select 'C:\new\test' from #system.dual()";
        var parseCount = 0;

        var templates = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
            ParsedQueryTemplateCache.GetOrAddWithDiagnostics(
                script,
                ParsedQueryTemplateCache.DefaultParserContract,
                () =>
                {
                    Interlocked.Increment(ref parseCount);
                    Thread.Sleep(10);
                    var lexer = new Lexer(script, true);
                    var root = new ParserType(lexer).ComposeAll();
                    return new ParsedQueryTemplate(root, lexer.Diagnostics.ToImmutableArray());
                }))));

        Assert.AreEqual(1, parseCount);
        Assert.HasCount(32, templates.Select(static template => template.Root).Distinct(ReferenceEqualityComparer.Instance));
        Assert.IsTrue(templates.All(static template => template.Diagnostics.Length == 1));
        Assert.IsTrue(templates.All(static template =>
            template.Diagnostics[0].Code == global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape));
        Assert.AreEqual(1, ParsedQueryTemplateCache.Snapshot.Count);
        Assert.AreEqual(31, ParsedQueryTemplateCache.Snapshot.Hits);
        Assert.AreEqual(1, ParsedQueryTemplateCache.Snapshot.Misses);
    }

    [TestMethod]
    public async Task SameScriptWithRequiredAliasError_WhenRequestedConcurrently_ShouldReplayOneErrorPerCaller()
    {
        const string script =
            "select 1 from #system.dual() source cross apply source.Column take 10; " +
            "select 1 from #system.dual() d";
        var parseCount = 0;

        var templates = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
            ParsedQueryTemplateCache.GetOrAddWithDiagnostics(
                script,
                ParsedQueryTemplateCache.DefaultParserContract,
                () =>
                {
                    Interlocked.Increment(ref parseCount);
                    Thread.Sleep(10);
                    var lexer = new Lexer(script, true);
                    var diagnostics = new DiagnosticBag { SourceText = new SourceText(script) };
                    var parseResult = new ParserType(lexer, diagnostics).ParseWithDiagnostics();
                    return new ParsedQueryTemplate(
                        parseResult.Root!,
                        diagnostics.ToSortedList().ToImmutableArray());
                }))));

        Assert.AreEqual(1, parseCount);
        Assert.HasCount(32, templates.Select(static template => template.Root).Distinct(ReferenceEqualityComparer.Instance));
        Assert.IsTrue(templates.All(static template => template.Diagnostics.Length == 1));
        Assert.IsTrue(templates.All(static template =>
            template.Diagnostics[0].Code == DiagnosticCode.MQ2035_MissingRequiredAlias));
        Assert.AreEqual(1, ParsedQueryTemplateCache.Snapshot.Count);
        Assert.AreEqual(31, ParsedQueryTemplateCache.Snapshot.Hits);
        Assert.AreEqual(1, ParsedQueryTemplateCache.Snapshot.Misses);
    }

    [TestMethod]
    public void SameScript_WhenFirstParseFails_ShouldNotPoisonCache()
    {
        const string script = "select 1 from #A.Entities()";
        var calls = 0;

        Assert.Throws<InvalidOperationException>(() =>
            ParsedQueryTemplateCache.GetOrAdd(script, () =>
            {
                calls++;
                throw new InvalidOperationException("parse failed");
            }));

        var root = ParsedQueryTemplateCache.GetOrAdd(script, () =>
        {
            calls++;
            return Parse(script);
        });

        Assert.IsNotNull(root);
        Assert.AreEqual(2, calls);
        Assert.AreEqual(1, ParsedQueryTemplateCache.Snapshot.Count);
    }

    [TestMethod]
    public void ExactScriptAndParserContract_ShouldDefineCacheIdentity()
    {
        const string script = "select 1 from #A.Entities()";
        var calls = 0;

        ParsedQueryTemplateCache.GetOrAdd(script, "mode-a", () =>
        {
            calls++;
            return Parse(script);
        });
        ParsedQueryTemplateCache.GetOrAdd(" " + script, "mode-a", () =>
        {
            calls++;
            return Parse(script);
        });
        ParsedQueryTemplateCache.GetOrAdd(script, "mode-b", () =>
        {
            calls++;
            return Parse(script);
        });

        Assert.AreEqual(3, calls);
        Assert.AreEqual(3, ParsedQueryTemplateCache.Snapshot.Count);
    }

    [TestMethod]
    public void Cache_ShouldStayWithinEntryAndRetainedTextBounds()
    {
        for (var i = 0; i < 300; i++)
        {
            var script = $"query-{i:D4}-{new string('x', i * 1000)}";
            ParsedQueryTemplateCache.GetOrAdd(script, () => Parse("select 1 from #A.Entities()"));
        }

        var snapshot = ParsedQueryTemplateCache.Snapshot;
        Assert.IsLessThanOrEqualTo(256, snapshot.Count);
        Assert.IsLessThanOrEqualTo(4_000_000, snapshot.RetainedTextCharacters);
    }

    private static RootNode Parse(string script)
    {
        var lexer = new Lexer(script, true);
        return new ParserType(lexer).ComposeAll();
    }

    private static Node[] GetSchemaDefinitions(RootNode root)
    {
        var statements = (StatementsArrayNode)root.Expression;
        return statements.Statements
            .Select(static statement => statement.Node)
            .Where(static node => node is BinarySchemaNode or TextSchemaNode)
            .ToArray();
    }

    private static void AssertInterpretationSchemasAreIsolated(
        RootNode first,
        RootNode second,
        int expectedSchemaCount)
    {
        Assert.AreNotSame(first, second);
        var firstSchemas = GetSchemaDefinitions(first);
        var secondSchemas = GetSchemaDefinitions(second);
        Assert.HasCount(expectedSchemaCount, firstSchemas);
        Assert.HasCount(expectedSchemaCount, secondSchemas);

        for (var index = 0; index < expectedSchemaCount; index++)
        {
            Assert.AreEqual(firstSchemas[index].ToString(), secondSchemas[index].ToString());
            Assert.AreNotSame(firstSchemas[index], secondSchemas[index]);

            switch (firstSchemas[index], secondSchemas[index])
            {
                case (BinarySchemaNode firstBinary, BinarySchemaNode secondBinary):
                    Assert.AreNotSame(firstBinary.Fields, secondBinary.Fields);
                    Assert.AreNotSame(firstBinary.TypeParameters, secondBinary.TypeParameters);
                    Assert.HasCount(firstBinary.Fields.Length, secondBinary.Fields);
                    for (var fieldIndex = 0; fieldIndex < firstBinary.Fields.Length; fieldIndex++)
                    {
                        Assert.AreNotSame(firstBinary.Fields[fieldIndex], secondBinary.Fields[fieldIndex]);
                        AssertSchemaFieldIsIsolated(
                            firstBinary.Fields[fieldIndex],
                            secondBinary.Fields[fieldIndex]);
                    }
                    break;

                case (TextSchemaNode firstText, TextSchemaNode secondText):
                    Assert.AreNotSame(firstText.Fields, secondText.Fields);
                    Assert.HasCount(firstText.Fields.Length, secondText.Fields);
                    for (var fieldIndex = 0; fieldIndex < firstText.Fields.Length; fieldIndex++)
                    {
                        Assert.AreNotSame(firstText.Fields[fieldIndex], secondText.Fields[fieldIndex]);
                        if (firstText.Fields[fieldIndex].CaptureGroups.Length > 0)
                            Assert.AreNotSame(
                                firstText.Fields[fieldIndex].CaptureGroups,
                                secondText.Fields[fieldIndex].CaptureGroups);
                        if (firstText.Fields[fieldIndex].SwitchCases.Length > 0)
                            Assert.AreNotSame(
                                firstText.Fields[fieldIndex].SwitchCases,
                                secondText.Fields[fieldIndex].SwitchCases);
                    }
                    break;
            }
        }
    }

    private static void AssertSchemaFieldIsIsolated(SchemaFieldNode first, SchemaFieldNode second)
    {
        switch (first, second)
        {
            case (FieldDefinitionNode firstField, FieldDefinitionNode secondField):
                Assert.AreNotSame(firstField.TypeAnnotation, secondField.TypeAnnotation);
                if (firstField.AtOffset != null)
                    Assert.AreNotSame(firstField.AtOffset, secondField.AtOffset);
                if (firstField.WhenCondition != null)
                    Assert.AreNotSame(firstField.WhenCondition, secondField.WhenCondition);
                if (firstField.Constraint != null)
                    Assert.AreNotSame(firstField.Constraint, secondField.Constraint);
                if (firstField.ValueValidation != null)
                {
                    Assert.AreNotSame(firstField.ValueValidation, secondField.ValueValidation);
                    Assert.AreNotSame(firstField.ValueValidation.Values, secondField.ValueValidation!.Values);
                }
                AssertTypeAnnotationIsIsolated(firstField.TypeAnnotation, secondField.TypeAnnotation);
                break;

            case (ComputedFieldNode firstComputed, ComputedFieldNode secondComputed):
                Assert.AreNotSame(firstComputed.Expression, secondComputed.Expression);
                break;
        }
    }

    private static void AssertTypeAnnotationIsIsolated(TypeAnnotationNode first, TypeAnnotationNode second)
    {
        switch (first, second)
        {
            case (ByteArrayTypeNode firstType, ByteArrayTypeNode secondType):
                Assert.AreNotSame(firstType.SizeExpression, secondType.SizeExpression);
                break;
            case (StringTypeNode firstType, StringTypeNode secondType):
                Assert.AreNotSame(firstType.SizeExpression, secondType.SizeExpression);
                break;
            case (SchemaReferenceTypeNode firstType, SchemaReferenceTypeNode secondType):
                Assert.AreNotSame(firstType.TypeArguments, secondType.TypeArguments);
                break;
            case (ArrayTypeNode firstType, ArrayTypeNode secondType):
                Assert.AreNotSame(firstType.ElementType, secondType.ElementType);
                Assert.AreNotSame(firstType.SizeExpression, secondType.SizeExpression);
                AssertTypeAnnotationIsIsolated(firstType.ElementType, secondType.ElementType);
                break;
            case (BinarySwitchTypeNode firstType, BinarySwitchTypeNode secondType):
                Assert.AreNotSame(firstType.Cases, secondType.Cases);
                for (var index = 0; index < firstType.Cases.Length; index++)
                {
                    Assert.AreNotSame(firstType.Cases[index], secondType.Cases[index]);
                    if (firstType.Cases[index].CaseValue != null)
                        Assert.AreNotSame(firstType.Cases[index].CaseValue, secondType.Cases[index].CaseValue);
                    Assert.AreNotSame(firstType.Cases[index].BranchType, secondType.Cases[index].BranchType);
                    AssertTypeAnnotationIsIsolated(
                        firstType.Cases[index].BranchType,
                        secondType.Cases[index].BranchType);
                }
                break;
            case (RepeatUntilTypeNode firstType, RepeatUntilTypeNode secondType):
                Assert.AreNotSame(firstType.ElementType, secondType.ElementType);
                if (firstType.Condition != null)
                    Assert.AreNotSame(firstType.Condition, secondType.Condition);
                AssertTypeAnnotationIsIsolated(firstType.ElementType, secondType.ElementType);
                break;
            case (SubstreamTypeNode firstType, SubstreamTypeNode secondType):
                Assert.AreNotSame(firstType.SizeExpression, secondType.SizeExpression);
                if (firstType.Target != null)
                {
                    Assert.AreNotSame(firstType.Target, secondType.Target);
                    AssertTypeAnnotationIsIsolated(firstType.Target, secondType.Target!);
                }
                break;
            case (InlineSchemaTypeNode firstType, InlineSchemaTypeNode secondType):
                Assert.AreNotSame(firstType.Fields, secondType.Fields);
                for (var index = 0; index < firstType.Fields.Length; index++)
                    AssertSchemaFieldIsIsolated(firstType.Fields[index], secondType.Fields[index]);
                break;
        }
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<RootNode>
    {
        public static ReferenceEqualityComparer Instance { get; } = new();

        public bool Equals(RootNode? x, RootNode? y) => ReferenceEquals(x, y);

        public int GetHashCode(RootNode obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
