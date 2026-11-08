using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.IR.Planning.SourcePlanning;
using Musoq.Evaluator.IR.SourcePlanning;
using Musoq.Parser;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class SourceRegexPlanningUtilityTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Converter_ShouldRetainConstantPatternAndNegation(bool negated)
    {
        IrExpression expression = new PatternMatch(new ColumnRef("s", "Name", typeof(string)),
            new Literal(@"\A(ab)\1\z", typeof(string)), PatternKind.RLike, typeof(bool));
        if (negated)
            expression = new UnaryOp(UnaryOpKind.Not, expression, typeof(bool));
        Assert.IsTrue(SourcePredicateExpressionConverter.TryConvertPredicate(expression, "s", out var converted));
        var match = Assert.IsInstanceOfType<SourcePredicateRegexMatch>(converted);
        Assert.AreEqual(@"\A(ab)\1\z", match.Pattern);
        Assert.AreEqual(negated, match.IsNegated);
        Assert.AreEqual(global::Musoq.Evaluator.Runtime.RuntimeCacheOptions.DefaultRegexTimeout, match.MatchTimeout);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void Converter_ShouldRejectColumnPatternsComputedInputsAndOtherAliases(int variant)
    {
        var column = new ColumnRef("s", "Name", typeof(string));
        var expression = new[]
        {
            new PatternMatch(column, column, PatternKind.RLike, typeof(bool)),
            new PatternMatch(new Literal("input", typeof(string)), new Literal("pattern", typeof(string)), PatternKind.RLike, typeof(bool)),
            new PatternMatch(column with { Alias = "other" }, new Literal("pattern", typeof(string)), PatternKind.RLike, typeof(bool)),
            new PatternMatch(column, new Literal(null, typeof(string)), PatternKind.RLike, typeof(bool)),
            new PatternMatch(column, new Literal("pattern", typeof(string)), (PatternKind)99, typeof(bool))
        }[variant];
        Assert.IsFalse(SourcePredicateExpressionConverter.TryConvertPredicate(expression, "s", out _));
    }

    [TestMethod]
    public void RegexComparer_ShouldIncludeEverySemanticFieldAndPreserveHashEquality()
    {
        var original = Match();
        var equal = new SourcePredicateRegexMatch(new SourceColumnRef("name"), original.Pattern, original.MatchTimeout);
        var comparer = SourcePredicateExpressionComparer.Instance;
        Assert.IsTrue(comparer.Equals(original, equal));
        Assert.AreEqual(comparer.GetHashCode(original), comparer.GetHashCode(equal));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void RegexComparer_ShouldDistinguishEverySemanticField(int variant)
    {
        var original = Match();
        var different = new[]
        {
            new SourcePredicateRegexMatch(new SourceColumnRef("Other"), original.Pattern, original.MatchTimeout),
            new SourcePredicateRegexMatch(original.Column, "other", original.MatchTimeout),
            new SourcePredicateRegexMatch(original.Column, original.Pattern, TimeSpan.FromMilliseconds(17)),
            new SourcePredicateRegexMatch(original.Column, original.Pattern, original.MatchTimeout, isNegated: true)
        }[variant];
        Assert.IsFalse(SourcePredicateExpressionComparer.Instance.Equals(original, different));
    }

    [TestMethod]
    public void DuplicateRegexConjuncts_ShouldRequireAnApplicationForEachOccurrence()
    {
        var match = Match();
        var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with
        {
            Predicate = new SourcePredicateLogical(SourcePredicateLogicalOperator.And, match, match)
        };
        var result = SourcePlanResult.AcceptAll(request);
        SourcePredicatePlanContractValidator.Validate(request, result, Capabilities(), TextSpan.Empty);
        Assert.HasCount(2, result.ExecutionPlan.PredicateApplications);
        var missing = result with { ExecutionPlan = result.ExecutionPlan with
        {
            PredicateApplications = result.ExecutionPlan.PredicateApplications.Take(1).ToArray()
        } };
        Assert.Throws<SourcePlanContractException>(() =>
            SourcePredicatePlanContractValidator.Validate(request, missing, Capabilities(), TextSpan.Empty));
    }

    [TestMethod]
    public void NestedRegex_ShouldBeDeferredAndCannotBeAcceptedByProvider()
    {
        var match = Match();
        var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with
        {
            Predicate = new SourcePredicateLogical(SourcePredicateLogicalOperator.Or, match, match)
        };
        var negotiation = SourcePredicateCapabilityNegotiator.Negotiate(request, Capabilities());
        Assert.IsNull(negotiation.ProviderRequest.Predicate);
        Assert.AreSame(request.Predicate, negotiation.DeferredPredicate);
        var fabricated = SourcePlanResult.AcceptAll(request);
        Assert.Throws<SourcePlanContractException>(() =>
            SourcePredicatePlanContractValidator.Validate(request, fabricated, Capabilities(), TextSpan.Empty));
    }

    [TestMethod]
    public void OrdinaryPredicateMovement_ShouldStillRejectPotentiallyThrowingRegex()
    {
        var expression = new PatternMatch(new ColumnRef("s", "Name", typeof(string)),
            new Literal("pattern", typeof(string)), PatternKind.RLike, typeof(bool));
        Assert.IsFalse(PredicateMovementPlanner.IsTransferSafe(expression));
        Assert.IsFalse(PredicateMovementPlanner.IsTransferSafe(expression with
        {
            Expression = new ColumnRef("s", "Name", typeof(string)) { Stability = ColumnStability.Volatile }
        }));
    }

    private static SourcePredicateRegexMatch Match() => new(new SourceColumnRef("Name"), "pattern", TimeSpan.FromSeconds(1));

    private static SourcePredicateCapabilities Capabilities() => new()
    {
        RegexMatches = [new SourceRegexMatchCapability(new SourceColumnRef("Name"))]
    };
}
