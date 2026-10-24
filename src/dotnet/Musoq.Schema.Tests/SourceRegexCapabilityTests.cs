using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Optimization;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class SourceRegexCapabilityTests
{
    [TestMethod]
    public void RegexCapability_ShouldMatchColumnNegationSemanticsAndRowFilteringOnly()
    {
        var capability = new SourceRegexMatchCapability(new SourceColumnRef("Name"));
        var match = new SourcePredicateRegexMatch(new SourceColumnRef("name"), "(?i)pattern", TimeSpan.FromMilliseconds(37));
        Assert.IsTrue(capability.Supports(match, SourcePredicateEvaluationPhase.RowFiltering));
        Assert.IsFalse(capability.Supports(match, SourcePredicateEvaluationPhase.CandidateMetadata));
        Assert.IsFalse(capability.Supports(new SourcePredicateRegexMatch(match.Column, match.Pattern, match.MatchTimeout,
            isNegated: true), SourcePredicateEvaluationPhase.RowFiltering));
        Assert.IsFalse(capability.Supports(new SourcePredicateRegexMatch(new SourceColumnRef("Other"), match.Pattern,
            match.MatchTimeout), SourcePredicateEvaluationPhase.RowFiltering));
        Assert.IsTrue(new SourceRegexMatchCapability(match.Column, supportsNegation: true).Supports(
            new SourcePredicateRegexMatch(match.Column, match.Pattern, match.MatchTimeout, isNegated: true),
            SourcePredicateEvaluationPhase.RowFiltering));
    }

    [TestMethod]
    public void RegexDeclarations_ShouldBeFrozenAndAllowLikeOnSameColumn()
    {
        var declarations = new List<SourceRegexMatchCapability> { new(new SourceColumnRef("Name")) };
        var capabilities = new SourcePredicateCapabilities
        {
            RegexMatches = declarations,
            StringMatches = [new SourceStringMatchCapability(new SourceColumnRef("Name"), SourceStringMatchOperations.All)]
        };
        declarations.Clear();
        capabilities.Validate();
        Assert.HasCount(1, capabilities.RegexMatches);
        Assert.Throws<NotSupportedException>(() => ((IList<SourceRegexMatchCapability>)capabilities.RegexMatches).Clear());
        var duplicate = capabilities with
        {
            RegexMatches = [new(new SourceColumnRef("Name")), new(new SourceColumnRef("NAME"))]
        };
        Assert.Throws<ArgumentException>(duplicate.Validate);
        Assert.Throws<ArgumentNullException>(() => new SourcePredicateCapabilities { RegexMatches = null! });
        Assert.Throws<ArgumentNullException>(() => new SourcePredicateCapabilities { RegexMatches = [null!] }.Validate());
    }

    [TestMethod]
    [DataRow(0L)]
    [DataRow(1L)]
    [DataRow(-10000L)]
    [DataRow(long.MaxValue)]
    public void RegexContract_ShouldRejectInvalidTimeouts(long ticks)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourcePredicateRegexMatch(
            new SourceColumnRef("Name"), "pattern", TimeSpan.FromTicks(ticks)));
    }

    [TestMethod]
    public void RegexContract_ShouldRejectUnknownSemanticsAndNullPatterns()
    {
        var column = new SourceColumnRef("Name");
        Assert.Throws<ArgumentNullException>(() => new SourcePredicateRegexMatch(column, null!, TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourcePredicateRegexMatch(column, "", TimeSpan.FromSeconds(1),
            (SourceRegexSemantics)123));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourceRegexMatchCapability(column, (SourceRegexSemantics)123));
        var match = new SourcePredicateRegexMatch(column, "", TimeSpan.FromMilliseconds(1));
        var capabilities = new SourcePredicateCapabilities { RegexMatches = [new(column)] };
        Assert.IsTrue(capabilities.Supports(match, SourcePredicateEvaluationPhase.RowFiltering));
        Assert.IsFalse((capabilities with { ContractVersion = 1 }).Supports(match, SourcePredicateEvaluationPhase.RowFiltering));
    }
}
