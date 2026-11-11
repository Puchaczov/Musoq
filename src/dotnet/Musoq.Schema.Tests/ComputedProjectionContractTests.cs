using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Optimization;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class ComputedProjectionContractTests
{
    [TestMethod]
    public void LegacyDescriptor_DoesNotOptIntoComputedProjection() =>
        Assert.AreEqual(
            SourceComputedProjectionCapabilities.None,
            new SourceDescriptor { Identity = SourceIdentity.Empty }.ComputedProjectionCapabilities
        );

    [TestMethod]
    public void OrdinaryShape_PreservesTheBaselineBinaryFingerprint()
    {
        var shape = new QueryRowShape([new(0, 0, "Id", typeof(int), false), new(1, 1, "Value", typeof(int), false)]);
        Assert.AreEqual("999FBE0BF161983E61577F155D57C6803143200AAB01644305740C0969A1E7E0", shape.Fingerprint);
        Assert.AreEqual(QueryRowFieldOrigin.SourceColumn, shape.Fields[0].Origin);
    }

    [TestMethod]
    public void ComputedField_HasExplicitOriginAndItsExpressionAffectsShapeIdentity()
    {
        var first = QueryRowField.ForComputedProjection(
            0,
            new("Value", new SourceScalarLiteral(1L, typeof(long)), typeof(long))
        );
        var second = QueryRowField.ForComputedProjection(
            0,
            new("Value", new SourceScalarLiteral(2L, typeof(long)), typeof(long))
        );
        Assert.AreEqual(QueryRowFieldOrigin.ComputedProjection, first.Origin);
        Assert.AreEqual(-1, first.SourceColumnIndex);
        Assert.AreNotEqual(new QueryRowShape([first]).Fingerprint, new QueryRowShape([second]).Fingerprint);
        Assert.Throws<ArgumentOutOfRangeException>(() => new QueryRowField(0, -1, "Value", typeof(long), false));
    }

    [TestMethod]
    public void ComputedField_RejectsAnInexactResultTypeAndVolatility()
    {
        var expression = new SourceScalarColumn("Value", typeof(int));
        Assert.Throws<ArgumentException>(() =>
            QueryRowField.ForComputedProjection(0, new("Value", expression, typeof(long)))
        );
        Assert.Throws<ArgumentException>(() =>
            QueryRowField.ForComputedProjection(0, new("Value", expression, typeof(int), ColumnStability.Volatile))
        );
    }

    [TestMethod]
    public void RequestResultAndCoalesce_FreezeCallerOwnedCollections()
    {
        SourceComputedProjection[] projections = [new("Value", new SourceScalarLiteral(1, typeof(int)), typeof(int))];
        var request = new SourcePlanRequest
        {
            Identity = SourceIdentity.Empty,
            RequestedComputedProjections = projections,
        };
        var result = SourcePlanResult.AcceptAll(request);
        projections[0] = new("Changed", new SourceScalarLiteral(2, typeof(int)), typeof(int));
        Assert.AreEqual("Value", request.RequestedComputedProjections[0].Name);
        Assert.AreEqual("Value", result.AcceptedComputedProjections[0].Name);
        SourceScalarExpression[] operands =
        [
            new SourceScalarLiteral(null, typeof(int?)),
            new SourceScalarLiteral(1, typeof(int)),
        ];
        var coalesce = new SourceScalarCoalesce(operands, typeof(int));
        var copy = coalesce with { Expressions = operands };
        operands[1] = new SourceScalarLiteral(2, typeof(int));
        Assert.AreEqual(1, ((SourceScalarLiteral)coalesce.Expressions[1]).Value);
        Assert.AreEqual(1, ((SourceScalarLiteral)copy.Expressions[1]).Value);
    }

    [TestMethod]
    public void LiteralIdentity_IsCultureIndependentAndIncludesTypedNullsAndFloatBits()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL");
            var first = SourceScalarExpressionFingerprint.Compute(new SourceScalarLiteral(1.25m, typeof(decimal)));
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.AreEqual(
                first,
                SourceScalarExpressionFingerprint.Compute(new SourceScalarLiteral(1.25m, typeof(decimal)))
            );
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
        Assert.AreNotEqual(
            SourceScalarExpressionFingerprint.Compute(new SourceScalarLiteral(null, typeof(int?))),
            SourceScalarExpressionFingerprint.Compute(new SourceScalarLiteral(null, typeof(long?)))
        );
        Assert.AreNotEqual(
            SourceScalarExpressionFingerprint.Compute(new SourceScalarLiteral(0d, typeof(double))),
            SourceScalarExpressionFingerprint.Compute(new SourceScalarLiteral(-0d, typeof(double)))
        );
    }

    [TestMethod]
    public void UnknownAndMalformedExpressions_AreNotStableOrFingerprintable()
    {
        var unknown = new UnsupportedScalar();
        Assert.IsFalse(SourceScalarExpressionFacts.IsStable(unknown));
        Assert.Throws<ArgumentException>(() => SourceScalarExpressionFingerprint.Compute(unknown));
        var malformed = new SourceScalarBinary(
            SourceScalarBinaryOperator.Add,
            null!,
            new SourceScalarLiteral(1, typeof(int)),
            typeof(int)
        );
        Assert.IsFalse(SourceScalarExpressionFacts.IsStable(malformed));
        Assert.Throws<ArgumentException>(() => SourceScalarExpressionFingerprint.Compute(malformed));
    }

    private sealed record UnsupportedScalar() : SourceScalarExpression(typeof(int));

    [TestMethod]
    public void DuplicateRequestsAndInexactAcceptance_AreRejected()
    {
        var projection = new SourceComputedProjection("Value", new SourceScalarLiteral(1, typeof(int)), typeof(int));
        Assert.IsFalse(
            SourceComputedProjectionNegotiator.TryPartition(
                [projection, projection],
                [],
                SourceComputedProjectionCapabilities.AllPortable,
                out _,
                out var duplicate
            )
        );
        StringAssert.Contains(duplicate, "unique");
        Assert.IsFalse(
            SourceComputedProjectionNegotiator.TryPartition(
                [projection],
                [projection with { ResultType = typeof(long) }],
                SourceComputedProjectionCapabilities.AllPortable,
                out _,
                out _
            )
        );
    }

    [TestMethod]
    public void InconsistentAndExcessivelyDeepGraphs_FailClosed()
    {
        var inconsistent = new SourceScalarLiteral(1, typeof(int)) with { ValueType = typeof(long) };
        Assert.IsFalse(SourceScalarExpressionFacts.IsStable(inconsistent));
        SourceScalarExpression expression = new SourceScalarColumn("Id", typeof(int));
        for (var index = 0; index < 40; index++)
            expression = new SourceScalarCast(expression, typeof(int));
        Assert.IsFalse(SourceScalarExpressionFacts.IsStable(expression));
        Assert.AreEqual(
            SourceComputedProjectionCapabilities.None,
            SourceComputedProjectionFacts.RequiredCapabilities(expression)
        );
    }

    [TestMethod]
    public void ColumnReadModifiers_AreFrozenAndParticipateInIdentity()
    {
        var modifiers = new System.Collections.Generic.Dictionary<string, string> { ["format"] = "first" };
        var column = new SourceColumnRef("Value") with { ReadModifiers = modifiers };
        var expression = new SourceScalarColumn(column, typeof(int));
        var fingerprint = SourceScalarExpressionFingerprint.Compute(expression);
        modifiers["format"] = "changed";
        Assert.AreEqual(fingerprint, SourceScalarExpressionFingerprint.Compute(expression));
        Assert.AreNotEqual(
            fingerprint,
            SourceScalarExpressionFingerprint.Compute(
                new SourceScalarColumn(column with { ReadModifiers = modifiers }, typeof(int))
            )
        );
    }

    [TestMethod]
    public void NestedExpressionIdentity_HasBoundedEncodingSize()
    {
        var identity = SourceScalarExpressionFingerprint.Compute(NestedCasts(25));
        Assert.IsLessThan(10_000, identity.Length);
        StringAssert.Contains(identity, "Id");
    }

    [TestMethod]
    public void SqlComparisonSemantics_CannotBeChangedByAcceptance()
    {
        var expression = new SourceScalarBinary(
            SourceScalarBinaryOperator.Equal,
            new SourceScalarColumn("Id", typeof(int?)),
            new SourceScalarLiteral(0, typeof(int)),
            typeof(bool?)
        )
        {
            UsesSqlNullSemantics = true,
        };
        var requested = new SourceComputedProjection("Value", expression, typeof(bool?));
        var modified = requested with { Expression = expression with { UsesSqlNullSemantics = false } };
        Assert.AreNotEqual(
            SourceScalarExpressionFingerprint.Compute(expression),
            SourceScalarExpressionFingerprint.Compute(modified.Expression)
        );
        Assert.IsFalse(
            SourceComputedProjectionNegotiator.TryPartition(
                [requested],
                [modified],
                SourceComputedProjectionCapabilities.AllPortable,
                out _,
                out _
            )
        );
    }

    private static SourceScalarExpression NestedCasts(int count)
    {
        SourceScalarExpression expression = new SourceScalarColumn("Id", typeof(int));
        for (var index = 0; index < count; index++)
            expression = new SourceScalarCast(expression, typeof(int));
        return expression;
    }
}
