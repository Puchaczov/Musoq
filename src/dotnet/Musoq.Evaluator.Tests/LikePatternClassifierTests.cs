using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LikePatternClassifierTests
{
    [TestMethod]
    public void Classify_ExactAsciiPattern_ShouldReturnExactLiteral()
    {
        var classification = LikePatternClassifier.Classify("Google");

        Assert.AreEqual(
            new LikePatternClassification(
                LikePatternMatchKind.Exact,
                LikePatternCharacterDomain.Ascii,
                0,
                6),
            classification.Classification);
    }

    [TestMethod]
    public void Classify_BoundaryPercentPattern_ShouldReturnPrefix()
    {
        var classification = LikePatternClassifier.Classify("Google%%");

        Assert.AreEqual(
            new LikePatternClassification(
                LikePatternMatchKind.Prefix,
                LikePatternCharacterDomain.Ascii,
                0,
                6),
            classification.Classification);
    }

    [TestMethod]
    public void Classify_BoundaryPercentPattern_ShouldReturnSuffix()
    {
        var classification = LikePatternClassifier.Classify("%%example.com");

        Assert.AreEqual(
            new LikePatternClassification(
                LikePatternMatchKind.Suffix,
                LikePatternCharacterDomain.Ascii,
                2,
                11),
            classification.Classification);
    }

    [TestMethod]
    public void Classify_BoundaryPercentPattern_ShouldReturnContains()
    {
        var classification = LikePatternClassifier.Classify("%%Google%%");

        Assert.AreEqual(
            new LikePatternClassification(
                LikePatternMatchKind.Contains,
                LikePatternCharacterDomain.Ascii,
                2,
                6),
            classification.Classification);
    }

    [TestMethod]
    public void Classify_AllPercentPattern_ShouldReturnEmptyContainsLiteral()
    {
        var classification = LikePatternClassifier.Classify("%%");

        Assert.AreEqual(
            new LikePatternClassification(
                LikePatternMatchKind.Contains,
                LikePatternCharacterDomain.Ascii,
                2,
                0),
            classification.Classification);
    }

    [TestMethod]
    public void Classify_SingleCharacterWildcardPattern_ShouldReturnNoClassification()
    {
        var classification = LikePatternClassifier.Classify("h_t");

        Assert.IsFalse(classification.IsClassified);
        Assert.AreEqual(LikePatternRejectionReason.SingleCharacterWildcard, classification.RejectionReason);
    }

    [TestMethod]
    public void Classify_InteriorPercentPattern_ShouldReturnNoClassification()
    {
        var classification = LikePatternClassifier.Classify("a%b");

        Assert.IsFalse(classification.IsClassified);
        Assert.AreEqual(LikePatternRejectionReason.InteriorPercentWildcard, classification.RejectionReason);
    }

    [TestMethod]
    public void Classify_NonAsciiPattern_ShouldReturnShapeAndUnicodeDomain()
    {
        var classification = LikePatternClassifier.Classify("Ż%");

        Assert.AreEqual(
            new LikePatternClassification(
                LikePatternMatchKind.Prefix,
                LikePatternCharacterDomain.Unicode,
                0,
                1),
            classification.Classification);
        Assert.AreEqual(LikePatternCharacterDomain.Unicode, classification.Domain);
    }

    [TestMethod]
    public void Classify_UnicodePatternWithUnsupportedWildcard_ShouldRetainDomainAndShapeRejection()
    {
        var classification = LikePatternClassifier.Classify("Ż_");

        Assert.IsFalse(classification.IsClassified);
        Assert.AreEqual(LikePatternCharacterDomain.Unicode, classification.Domain);
        Assert.AreEqual(LikePatternRejectionReason.SingleCharacterWildcard, classification.RejectionReason);
    }

}
