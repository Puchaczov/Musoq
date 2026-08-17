using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticDescriptorRegistryTests
{
    [TestMethod]
    public void DescriptorRegistry_HasOneDescriptorForEveryCatalogEntry()
    {
        var catalogCodes = Enum.GetValues<DiagnosticCode>()
            .Where(code => ErrorCatalog.HasTemplate(code) || ErrorMetadataCatalog.GetLegacy(code) != null)
            .ToArray();

        var descriptors = DiagnosticDescriptorRegistry.All.ToArray();

        Assert.AreEqual(catalogCodes.Length, descriptors.Length);
        CollectionAssert.AreEquivalent(catalogCodes, descriptors.Select(static descriptor => descriptor.Code).ToArray());
        Assert.AreEqual(descriptors.Length, descriptors.Select(static descriptor => descriptor.Code).Distinct().Count());
    }

    [TestMethod]
    public void CatalogLookups_UseTheUnifiedDescriptorDefinition()
    {
        var descriptor = DiagnosticDescriptorRegistry.Get(DiagnosticCode.MQ3001_UnknownColumn);

        Assert.IsNotNull(descriptor);
        Assert.AreEqual(descriptor.MessageTemplate, ErrorCatalog.GetTemplate(DiagnosticCode.MQ3001_UnknownColumn));
        Assert.AreEqual(descriptor.DefaultSeverity, ErrorCatalog.GetDefaultSeverity(DiagnosticCode.MQ3001_UnknownColumn));
        Assert.AreEqual(descriptor.Category, ErrorCatalog.GetCategory(DiagnosticCode.MQ3001_UnknownColumn));
        Assert.AreEqual(descriptor.Explanation, ErrorMetadataCatalog.Get(DiagnosticCode.MQ3001_UnknownColumn)!.Explanation);
        Assert.AreEqual(descriptor.DefaultActions.Count, ErrorMetadataCatalog.Get(DiagnosticCode.MQ3001_UnknownColumn)!.SuggestedFixes.Length);
    }

    [TestMethod]
    public void DiagnosticBag_DeduplicatesIdenticalDiagnosticsAndOrdersBySourceAndOffset()
    {
        var bag = new DiagnosticBag();
        var generated = new Diagnostic(
            DiagnosticCode.MQ8001_CodeGenerationFailed,
            DiagnosticSeverity.Error,
            "generated",
            new SourceLocation(0, 1, 1),
            phase: DiagnosticPhase.CodeGeneration,
            sourceKind: DiagnosticSourceKind.GeneratedSource);
        var later = new Diagnostic(
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticSeverity.Error,
            "later",
            new SourceLocation(10, 1, 11));
        var earlier = new Diagnostic(
            DiagnosticCode.MQ3002_AmbiguousColumn,
            DiagnosticSeverity.Warning,
            "earlier",
            new SourceLocation(2, 1, 3));

        Assert.IsTrue(bag.Add(later));
        Assert.IsTrue(bag.Add(earlier));
        Assert.IsTrue(bag.Add(generated));
        Assert.IsFalse(bag.Add(generated));
        Assert.AreEqual(3, bag.Count);

        var ordered = bag.ToSortedList();

        Assert.AreEqual(DiagnosticSourceKind.Query, ordered[0].SourceKind);
        Assert.AreEqual(2, ordered[0].Location.Offset);
        Assert.AreEqual(10, ordered[1].Location.Offset);
        Assert.AreEqual(DiagnosticSourceKind.GeneratedSource, ordered[2].SourceKind);
    }

    [TestMethod]
    public void DiagnosticBag_AddRangeRetainsStructuredPayload()
    {
        var source = new Diagnostic(
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticSeverity.Error,
            "unknown",
            new SourceLocation(3, 1, 4),
            arguments: new[] { new KeyValuePair<string, string>("symbol", "Name") },
            correlationId: "corr");
        var sourceBag = new DiagnosticBag();
        sourceBag.Add(source);
        var destination = new DiagnosticBag();

        destination.AddRange(sourceBag);

        var copied = destination.ToSortedList().Single();
        Assert.AreEqual("Name", copied.Arguments["symbol"]);
        Assert.AreEqual("corr", copied.CorrelationId);
    }
}
