using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.IR.Planning.SourcePlanning;
using Musoq.Evaluator.IR.SourcePlanning;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class SourcePredicatePlanningUtilityTests
{
    [TestMethod]
    public void Converter_ShouldConvertSupportedPredicateShapes()
    {
        var predicate = new BinaryOp(
            BinaryOpKind.And,
            Equal("s", "Category", "alpha"),
            new BinaryOp(
                BinaryOpKind.Or,
                new InCheck(
                    Column("s", "Name", typeof(string)),
                    [Literal("Alpha"), Literal("beta")],
                    typeof(bool)),
                new IsNullCheck(Column("s", "Name", typeof(string)), false, typeof(bool)),
                typeof(bool)),
            typeof(bool));

        var converted = SourcePredicateExpressionConverter.TryConvertPredicate(predicate, "s", out var sourcePredicate);

        Assert.IsTrue(converted);
        Assert.IsInstanceOfType<SourcePredicateLogical>(sourcePredicate);
    }

    [TestMethod]
    public void Converter_ShouldRejectMixedAliasPredicates()
    {
        var predicate = new BinaryOp(
            BinaryOpKind.Equal,
            Column("s", "Category", typeof(string)),
            Column("other", "Category", typeof(string)),
            typeof(bool));

        var converted = SourcePredicateExpressionConverter.TryConvertPredicate(predicate, "s", out _);

        Assert.IsFalse(converted);
    }

    [TestMethod]
    public void Converter_ShouldConvertBoundaryLikeLiteralToTypedStringMatch()
    {
        var expression = new PatternMatch(
            Column("s", "Name", typeof(string)),
            Literal("item-0%"),
            PatternKind.Like,
            typeof(bool));

        Assert.IsTrue(SourcePredicateExpressionConverter.TryConvertPredicate(expression, "s", out var predicate));

        var stringMatch = Assert.IsInstanceOfType<SourcePredicateStringMatch>(predicate);
        Assert.AreEqual("Name", stringMatch.Column.Name);
        Assert.AreEqual(SourceStringMatchKind.Prefix, stringMatch.Kind);
        Assert.AreEqual("item-0%", stringMatch.OriginalPattern);
        Assert.AreEqual("item-0", stringMatch.Needle);
        Assert.AreEqual(SourceStringComparison.LikeIgnoreCase, stringMatch.Comparison);
        Assert.IsFalse(stringMatch.IsNegated);
    }

    [TestMethod]
    public void Converter_ShouldConvertNotLikeToNegatedTypedStringMatch()
    {
        var expression = new UnaryOp(
            UnaryOpKind.Not,
            new PatternMatch(
                Column("s", "Name", typeof(string)),
                Literal("%item%"),
                PatternKind.Like,
                typeof(bool)),
            typeof(bool));

        Assert.IsTrue(SourcePredicateExpressionConverter.TryConvertPredicate(expression, "s", out var predicate));

        var stringMatch = Assert.IsInstanceOfType<SourcePredicateStringMatch>(predicate);
        Assert.AreEqual(SourceStringMatchKind.Contains, stringMatch.Kind);
        Assert.AreEqual("%item%", stringMatch.OriginalPattern);
        Assert.AreEqual("item", stringMatch.Needle);
        Assert.IsTrue(stringMatch.IsNegated);
    }

    [TestMethod]
    public void Converter_ShouldRejectLikePatternsWithInteriorOrSingleCharacterWildcards()
    {
        var interiorWildcard = new PatternMatch(
            Column("s", "Name", typeof(string)),
            Literal("item-%0%"),
            PatternKind.Like,
            typeof(bool));
        var singleCharacterWildcard = new PatternMatch(
            Column("s", "Name", typeof(string)),
            Literal("item_0%"),
            PatternKind.Like,
            typeof(bool));

        Assert.IsFalse(SourcePredicateExpressionConverter.TryConvertPredicate(interiorWildcard, "s", out _));
        Assert.IsFalse(SourcePredicateExpressionConverter.TryConvertPredicate(singleCharacterWildcard, "s", out _));
    }

    [TestMethod]
    public void Converter_ShouldKeepUnicodeLikePatternAsEvaluatorResidual()
    {
        var expression = new PatternMatch(
            Column("s", "Name", typeof(string)),
            Literal("Żółć%"),
            PatternKind.Like,
            typeof(bool));

        Assert.IsFalse(SourcePredicateExpressionConverter.TryConvertPredicate(expression, "s", out _));
    }

    [TestMethod]
    public void Negotiator_ShouldDeferTypedMatchNestedOutsideTopLevelAnd()
    {
        var match = new SourcePredicateStringMatch(
            new SourceColumnRef("Name"), SourceStringMatchKind.Prefix, "item%", "item");
        var nested = new SourcePredicateComparison(
            SourcePredicateComparisonOperator.Equal,
            match,
            new SourcePredicateLiteral(true));
        var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with { Predicate = nested };
        var capabilities = new SourcePredicateCapabilities
        {
            StringMatches =
            [new SourceStringMatchCapability(new SourceColumnRef("Name"), SourceStringMatchOperations.All)]
        };

        var negotiation = SourcePredicateCapabilityNegotiator.Negotiate(request, capabilities);

        Assert.IsNull(negotiation.ProviderRequest.Predicate);
        Assert.AreSame(nested, negotiation.DeferredPredicate);
    }

    [TestMethod]
    public void ContractValidator_ShouldPreserveDuplicateTopLevelMatchMultiplicity()
    {
        var match = new SourcePredicateStringMatch(
            new SourceColumnRef("Name"), SourceStringMatchKind.Prefix, "item%", "item");
        var predicate = new SourcePredicateLogical(SourcePredicateLogicalOperator.And, match, match);
        var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with { Predicate = predicate };
        var result = SourcePlanResult.AcceptAll(request);
        var capabilities = new SourcePredicateCapabilities
        {
            StringMatches =
            [new SourceStringMatchCapability(new SourceColumnRef("Name"), SourceStringMatchOperations.All)]
        };

        SourcePredicatePlanContractValidator.Validate(request, result, capabilities, TextSpan.Empty);

        Assert.HasCount(2, result.ExecutionPlan.PredicateApplications);
    }

    [TestMethod]
    public void Comparer_ShouldUseStructuralSemantics()
    {
        var left = new SourcePredicateIn(
            new SourcePredicateColumn(new SourceColumnRef("Category")),
            [new SourcePredicateLiteral("alpha"), new SourcePredicateLiteral("beta")]);
        var same = new SourcePredicateIn(
            new SourcePredicateColumn(new SourceColumnRef("category")),
            [new SourcePredicateLiteral("alpha"), new SourcePredicateLiteral("beta")]);
        var differentOrder = new SourcePredicateIn(
            new SourcePredicateColumn(new SourceColumnRef("Category")),
            [new SourcePredicateLiteral("beta"), new SourcePredicateLiteral("alpha")]);
        var nullCheck = new SourcePredicateNullCheck(
            new SourcePredicateColumn(new SourceColumnRef("Category")));
        var negatedNullCheck = nullCheck with { IsNegated = true };

        Assert.IsTrue(SourcePredicateExpressionComparer.Instance.Equals(left, same));
        Assert.AreEqual(
            SourcePredicateExpressionComparer.Instance.GetHashCode(left),
            SourcePredicateExpressionComparer.Instance.GetHashCode(same));
        Assert.IsFalse(SourcePredicateExpressionComparer.Instance.Equals(left, differentOrder));
        Assert.IsFalse(SourcePredicateExpressionComparer.Instance.Equals(nullCheck, negatedNullCheck));
    }

    [TestMethod]
    public void Matcher_ShouldMapAcceptedAndConjunctsBackToOriginalPredicates()
    {
        var categoryPredicate = Equal("s", "Category", "alpha");
        var scorePredicate = GreaterThan("s", "Score", 10);
        var acceptedPredicate = new SourcePredicateLogical(
            SourcePredicateLogicalOperator.And,
            ToSourcePredicate(categoryPredicate),
            ToSourcePredicate(scorePredicate));
        var plan = new SourcePredicatePlan(
            "source-1",
            "s",
            new WhereNode(new BooleanNode(true)),
            [categoryPredicate, scorePredicate],
            "test",
            PlanningConfidence.High);

        var matched = SourcePredicateConjunctMatcher.MatchAcceptedConjuncts(acceptedPredicate, plan);

        CollectionAssert.AreEqual(new IrExpression[] { categoryPredicate, scorePredicate }, matched.ToArray());
    }

    [TestMethod]
    public void Matcher_ShouldRejectAcceptedOrPredicates()
    {
        var categoryPredicate = Equal("s", "Category", "alpha");
        var scorePredicate = GreaterThan("s", "Score", 10);
        var acceptedPredicate = new SourcePredicateLogical(
            SourcePredicateLogicalOperator.Or,
            ToSourcePredicate(categoryPredicate),
            ToSourcePredicate(scorePredicate));
        var plan = new SourcePredicatePlan(
            "source-1",
            "s",
            new WhereNode(new BooleanNode(true)),
            [categoryPredicate, scorePredicate],
            "test",
            PlanningConfidence.High);

        var matched = SourcePredicateConjunctMatcher.MatchAcceptedConjuncts(acceptedPredicate, plan);

        Assert.AreEqual(0, matched.Count);
    }

    [TestMethod]
    public void Matcher_ShouldMapWholeAcceptedOrPredicateWhenResidualIsClear()
    {
        var predicate = new BinaryOp(
            BinaryOpKind.Or,
            Equal("s", "Category", "alpha"),
            GreaterThan("s", "Score", 10),
            typeof(bool));
        var acceptedPredicate = ToSourcePredicate(predicate);
        var plan = new SourcePredicatePlan(
            "source-1",
            "s",
            new WhereNode(new BooleanNode(true)),
            [predicate],
            "test",
            PlanningConfidence.High);

        var matched = SourcePredicateConjunctMatcher.MatchAcceptedConjuncts(
            acceptedPredicate,
            plan,
            allowWholePredicateMatch: true);

        CollectionAssert.AreEqual(new IrExpression[] { predicate }, matched.ToArray());
    }

    [TestMethod]
    public void Matcher_ShouldRemoveOnlyExactlyAcceptedConjuncts()
    {
        var categoryPredicate = Equal("s", "Category", "alpha");
        var scorePredicate = GreaterThan("s", "Score", 10);
        var predicate = new BinaryOp(
            BinaryOpKind.And,
            categoryPredicate,
            scorePredicate,
            typeof(bool));

        var rewritten = SourcePredicateConjunctMatcher.RemoveAcceptedConjuncts(
            predicate,
            [categoryPredicate]);

        Assert.AreEqual(scorePredicate, rewritten);
    }

    private static SourcePredicateExpression ToSourcePredicate(IrExpression expression)
    {
        Assert.IsTrue(SourcePredicateExpressionConverter.TryConvertPredicate(expression, "s", out var predicate));
        return predicate!;
    }

    private static BinaryOp Equal(string alias, string columnName, object value)
    {
        return new BinaryOp(
            BinaryOpKind.Equal,
            Column(alias, columnName, value.GetType()),
            Literal(value),
            typeof(bool));
    }

    private static BinaryOp GreaterThan(string alias, string columnName, object value)
    {
        return new BinaryOp(
            BinaryOpKind.GreaterThan,
            Column(alias, columnName, value.GetType()),
            Literal(value),
            typeof(bool));
    }

    private static ColumnRef Column(string alias, string columnName, System.Type type)
    {
        return new ColumnRef(alias, columnName, type);
    }

    private static Literal Literal(object value)
    {
        return new Literal(value, value.GetType());
    }
}
