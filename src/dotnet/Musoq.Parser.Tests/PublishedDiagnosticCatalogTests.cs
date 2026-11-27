using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class PublishedDiagnosticCatalogTests
{
    [TestMethod]
    [DataRow(DiagnosticCode.MQ2023_MissingAsKeyword, DiagnosticPhase.Parse)]
    [DataRow(DiagnosticCode.MQ2026_InvalidCaseExpression, DiagnosticPhase.Parse)]
    [DataRow(DiagnosticCode.MQ2028_MissingThenClause, DiagnosticPhase.Parse)]
    [DataRow(DiagnosticCode.MQ2029_MissingEndKeyword, DiagnosticPhase.Parse)]
    [DataRow(DiagnosticCode.MQ4009_InvalidSchemaName, DiagnosticPhase.Schema)]
    [DataRow(DiagnosticCode.MQ4010_MissingRequiredField, DiagnosticPhase.Schema)]
    public void CatalogedDiagnostic_ShouldRetainItsPhaseAndPublishedGuidance(DiagnosticCode code, DiagnosticPhase phase)
    {
        var descriptor = DiagnosticDescriptorRegistry.Get(code);
        Assert.IsNotNull(descriptor);
        Assert.AreEqual(code, descriptor.Code);
        Assert.AreEqual(phase, descriptor.DefaultPhase);
        Assert.AreEqual(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
        Assert.IsFalse(string.IsNullOrWhiteSpace(descriptor.MessageTemplate));
        Assert.IsNotEmpty(descriptor.SuggestedFixes);
    }

    [TestMethod]
    [DataRow(5001)]
    [DataRow(5002)]
    [DataRow(5004)]
    [DataRow(5005)]
    [DataRow(5006)]
    [DataRow(5007)]
    [DataRow(5009)]
    [DataRow(5012)]
    public void ExcludedWarningNumber_ShouldRemainUnassigned(int number)
    {
        Assert.IsNull(DiagnosticDescriptorRegistry.Get((DiagnosticCode)number));
        var active = DiagnosticDescriptorRegistry.Get(DiagnosticCode.MQ5024_NullSensitiveNotIn);
        Assert.IsNotNull(active);
        Assert.AreEqual(DiagnosticSeverity.Warning, active.DefaultSeverity);
    }
}
