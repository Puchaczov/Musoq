using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Optimization;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class EnumSourcePlanningContractTests
{
    [TestMethod]
    public void EnumLiteral_ShouldCarryValueTypeBitsAndPortableFingerprintOnly()
    {
        var literal = new SourcePredicateEnumLiteral(
            EnumScalarValue.FromUInt64(ulong.MaxValue),
            new string('a', 64));

        Assert.AreEqual(EnumScalarValue.FromUInt64(ulong.MaxValue), literal.Value);
        Assert.AreEqual(new string('A', 64), literal.EnumFingerprint);
        Assert.AreEqual(typeof(EnumScalarValue),
            typeof(SourcePredicateEnumLiteral).GetProperty(nameof(SourcePredicateEnumLiteral.Value))!.PropertyType);
        Assert.IsFalse(typeof(SourcePredicateEnumLiteral)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(static property => property.PropertyType == typeof(object) || property.PropertyType == typeof(Type)));
    }

    [TestMethod]
    public void EnumLiteral_WhenFingerprintIsMalformed_ShouldRejectIt()
    {
        Assert.Throws<ArgumentException>(() => new SourcePredicateEnumLiteral(
            EnumScalarValue.FromInt32(1),
            "not-a-fingerprint"));
    }

    [TestMethod]
    public void FlagsPredicate_ShouldExposeExplicitAnyAndAllModes()
    {
        var column = new SourcePredicateColumn(new SourceColumnRef("Access"));
        var literal = new SourcePredicateEnumLiteral(EnumScalarValue.FromUInt32(3), new string('B', 64));

        var any = new SourcePredicateFlags(column, literal, SourcePredicateFlagsMatchMode.Any);
        var all = new SourcePredicateFlags(column, literal, SourcePredicateFlagsMatchMode.All);

        Assert.AreEqual(SourcePredicateFlagsMatchMode.Any, any.MatchMode);
        Assert.AreEqual(SourcePredicateFlagsMatchMode.All, all.MatchMode);
        Assert.AreSame(literal, any.Mask);
    }
}
