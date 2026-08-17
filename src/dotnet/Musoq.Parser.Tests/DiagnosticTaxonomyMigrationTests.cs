using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticTaxonomyMigrationTests
{
    [TestMethod]
    public void RemovedV17Values_ShouldRemainUnregisteredAndUnavailableForNewDiagnostics()
    {
        var removedValues = new[]
        {
            3003, 3004, 3006, 3009, 3013, 3014, 3029, 3030, 3031, 3082,
            5001, 5002, 5004, 5005, 5006, 5007, 5009, 5012,
            6001, 6002, 6003, 6004, 7001, 7002, 9999
        };

        foreach (var value in removedValues)
        {
            var code = (DiagnosticCode)value;
            Assert.IsFalse(
                Array.IndexOf(Enum.GetNames<DiagnosticCode>(), code.ToString()) >= 0,
                $"Removed numeric value {value} still has an enum member.");
            Assert.IsNull(ErrorMetadataCatalog.Get(code), $"Removed code {value} still has metadata.");
            Assert.AreEqual($"Error {code}", ErrorCatalog.GetTemplate(code));
        }
    }

    [TestMethod]
    public void InternalAndCallableFailures_ShouldUseTheV18RootClassifications()
    {
        var activeCodes = new[]
        {
            DiagnosticCode.MQ9001_InternalCompilerError,
            DiagnosticCode.MQ3086_UnknownCallable,
            DiagnosticCode.MQ3087_InvalidCallableArity,
            DiagnosticCode.MQ3088_NoMatchingCallableOverload,
            DiagnosticCode.MQ3089_AmbiguousCallableOverload
        };

        foreach (var code in activeCodes)
        {
            var metadata = ErrorMetadataCatalog.Get(code);
            Assert.IsNotNull(metadata, $"Missing active v18 metadata for {code}.");
            Assert.AreEqual(code, metadata.Code);
            Assert.IsFalse(string.IsNullOrWhiteSpace(ErrorCatalog.GetTemplate(code)));
        }
    }
}
