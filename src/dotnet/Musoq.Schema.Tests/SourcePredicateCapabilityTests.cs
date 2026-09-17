using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Optimization;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class SourcePredicateCapabilityTests
{
    [TestMethod]
    public void StringMatchCapability_ShouldMatchItsColumnOperationAndPhases()
    {
        var capability = new SourceStringMatchCapability(
            new SourceColumnRef("Name"),
            SourceStringMatchOperations.Prefix | SourceStringMatchOperations.Contains,
            SourceStringComparison.LikeIgnoreCase,
            supportsNegation: true,
            SourcePredicateEvaluationPhases.All);
        var prefix = new SourcePredicateStringMatch(
            new SourceColumnRef("name"),
            SourceStringMatchKind.Prefix,
            "item%",
            "item");

        Assert.IsTrue(capability.Supports(prefix, SourcePredicateEvaluationPhase.RowFiltering));
        Assert.IsTrue(capability.Supports(prefix, SourcePredicateEvaluationPhase.CandidateMetadata));
        Assert.IsFalse(capability.Supports(
            new SourcePredicateStringMatch(new SourceColumnRef("Name"), SourceStringMatchKind.Suffix, "%item", "item"),
            SourcePredicateEvaluationPhase.RowFiltering));
        Assert.IsFalse(capability.Supports(
            new SourcePredicateStringMatch(new SourceColumnRef("Other"), SourceStringMatchKind.Prefix, "item%", "item"),
            SourcePredicateEvaluationPhase.RowFiltering));
    }

    [TestMethod]
    public void StringMatchCapability_ShouldRequireExplicitNegationSupport()
    {
        var capability = new SourceStringMatchCapability(
            new SourceColumnRef("Name"),
            SourceStringMatchOperations.All);
        var predicate = new SourcePredicateStringMatch(
            new SourceColumnRef("Name"),
            SourceStringMatchKind.Contains,
            "%item%",
            "item",
            isNegated: true);

        Assert.IsFalse(capability.Supports(predicate, SourcePredicateEvaluationPhase.RowFiltering));
    }

    [TestMethod]
    public void PredicateCapabilities_ShouldFreezeDeclaredCapabilitiesAndExposeVersion()
    {
        var declared = new List<SourceStringMatchCapability>
        {
            new(new SourceColumnRef("Name"), SourceStringMatchOperations.Exact)
        };
        var capabilities = new SourcePredicateCapabilities { StringMatches = declared };
        declared.Clear();

        Assert.IsTrue(capabilities.IsKnownVersion);
        Assert.HasCount(1, capabilities.StringMatches);
        Assert.IsTrue(capabilities.Supports(
            new SourcePredicateStringMatch(new SourceColumnRef("Name"), SourceStringMatchKind.Exact, "item", "item"),
            SourcePredicateEvaluationPhase.RowFiltering));

        var unknown = capabilities with { ContractVersion = SourcePredicateCapabilities.CurrentContractVersion + 1 };
        Assert.IsFalse(unknown.IsKnownVersion);
        Assert.IsFalse(unknown.Supports(
            new SourcePredicateStringMatch(new SourceColumnRef("Name"), SourceStringMatchKind.Exact, "item", "item"),
            SourcePredicateEvaluationPhase.RowFiltering));
    }

    [TestMethod]
    public void PredicateCapabilities_None_ShouldExposeAnImmutableEmptyList()
    {
        Assert.Throws<NotSupportedException>(() =>
            ((IList<SourceStringMatchCapability>)SourcePredicateCapabilities.None.StringMatches).Add(
                new SourceStringMatchCapability(new SourceColumnRef("Name"), SourceStringMatchOperations.Exact)));
    }

    [TestMethod]
    public void PredicateApplication_ShouldExposeOnlyDirectTopLevelAndConjunctsWithMultiplicity()
    {
        var repeated = new SourcePredicateStringMatch(
            new SourceColumnRef("Name"), SourceStringMatchKind.Prefix, "a%", "a");
        var predicate = new SourcePredicateLogical(
            SourcePredicateLogicalOperator.And,
            repeated,
            new SourcePredicateLogical(
                SourcePredicateLogicalOperator.And,
                repeated,
                new SourcePredicateLogical(
                    SourcePredicateLogicalOperator.Or,
                    new SourcePredicateStringMatch(
                        new SourceColumnRef("Category"), SourceStringMatchKind.Exact, "alpha", "alpha"),
                    new SourcePredicateLiteral(true))));

        var applications = SourcePredicateApplication.ForRowFiltering(predicate);

        Assert.HasCount(2, applications);
        Assert.AreSame(repeated, applications[0].Predicate);
        Assert.AreEqual(SourcePredicateEvaluationPhase.RowFiltering, applications[0].Phase);
        Assert.AreSame(repeated, applications[1].Predicate);
    }

    [TestMethod]
    public void StringMatchContracts_ShouldRejectUnknownValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourcePredicateStringMatch(
            new SourceColumnRef("Name"),
            (SourceStringMatchKind)99,
            "item",
            "item"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourcePredicateStringMatch(
            new SourceColumnRef("Name"),
            SourceStringMatchKind.Exact,
            "item",
            "item",
            (SourceStringComparison)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourceStringMatchCapability(
            new SourceColumnRef("Name"),
            (SourceStringMatchOperations)128));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourceStringMatchCapability(
            new SourceColumnRef("Name"),
            SourceStringMatchOperations.Exact,
            phases: SourcePredicateEvaluationPhases.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourceStringMatchCapability(
            new SourceColumnRef("Name"),
            SourceStringMatchOperations.Exact,
            phases: (SourcePredicateEvaluationPhases)128));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourcePredicateApplication(
            new SourcePredicateStringMatch(
                new SourceColumnRef("Name"), SourceStringMatchKind.Exact, "item", "item"),
            (SourcePredicateEvaluationPhase)99));
    }

    [TestMethod]
    public void PredicateCapabilities_ShouldRejectNonPositiveVersions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SourcePredicateCapabilities { ContractVersion = 0 }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SourcePredicateCapabilities { ContractVersion = -1 }.Validate());
    }

    [TestMethod]
    public void PredicateCapabilities_ShouldAllowPositiveFutureVersionsButNotUseThem()
    {
        var capabilities = new SourcePredicateCapabilities
        {
            ContractVersion = SourcePredicateCapabilities.CurrentContractVersion + 1,
            StringMatches =
            [
                new SourceStringMatchCapability(new SourceColumnRef("Name"), SourceStringMatchOperations.Exact)
            ]
        };

        capabilities.Validate();

        Assert.IsFalse(capabilities.IsKnownVersion);
        Assert.IsFalse(capabilities.Supports(
            new SourcePredicateStringMatch(new SourceColumnRef("Name"), SourceStringMatchKind.Exact, "item", "item"),
            SourcePredicateEvaluationPhase.RowFiltering));
    }

    [TestMethod]
    public void PredicateCapabilities_ShouldRejectNullAndDuplicateColumnsCaseInsensitively()
    {
        var nullEntry = new SourcePredicateCapabilities
        {
            StringMatches = [null!]
        };
        var duplicates = new SourcePredicateCapabilities
        {
            StringMatches =
            [
                new SourceStringMatchCapability(new SourceColumnRef("Path"), SourceStringMatchOperations.Prefix),
                new SourceStringMatchCapability(new SourceColumnRef("path"), SourceStringMatchOperations.Contains)
            ]
        };

        Assert.Throws<ArgumentNullException>(nullEntry.Validate);
        Assert.Throws<ArgumentException>(duplicates.Validate);
    }

    [TestMethod]
    public void StringMatchPredicate_ShouldRetainOriginalPatternAndClassifiedNeedle()
    {
        var predicate = new SourcePredicateStringMatch(
            new SourceColumnRef("Path"), SourceStringMatchKind.Contains, "%%item%%", "item");

        Assert.AreEqual("%%item%%", predicate.OriginalPattern);
        Assert.AreEqual("item", predicate.Needle);
        Assert.AreEqual(SourceStringComparison.LikeIgnoreCase, predicate.Comparison);
    }

    [TestMethod]
    public void StringMatchPublicApi_ShouldExposeStronglyTypedVersionOneShape()
    {
        var constructor = typeof(SourcePredicateStringMatch).GetConstructors().Single();
        var parameterTypes = constructor.GetParameters().Select(static parameter => parameter.ParameterType).ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                typeof(SourceColumnRef), typeof(SourceStringMatchKind), typeof(string), typeof(string),
                typeof(SourceStringComparison), typeof(bool)
            },
            parameterTypes);
        Assert.AreEqual(
            typeof(SourcePredicateStringMatch),
            typeof(SourcePredicateApplication).GetProperty(nameof(SourcePredicateApplication.Predicate))!.PropertyType);
        Assert.IsNull(typeof(SourcePredicateStringMatch).GetProperty("Literal"));
        CollectionAssert.AreEqual(new[] { "LikeIgnoreCase" }, Enum.GetNames<SourceStringComparison>());
    }

}
