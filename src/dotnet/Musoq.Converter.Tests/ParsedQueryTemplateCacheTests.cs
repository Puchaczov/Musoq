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
using ParserType = global::Musoq.Parser.Parser;

namespace Musoq.Converter.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ParsedQueryTemplateCacheTests
{
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

    private sealed class ReferenceEqualityComparer : IEqualityComparer<RootNode>
    {
        public static ReferenceEqualityComparer Instance { get; } = new();

        public bool Equals(RootNode? x, RootNode? y) => ReferenceEquals(x, y);

        public int GetHashCode(RootNode obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
