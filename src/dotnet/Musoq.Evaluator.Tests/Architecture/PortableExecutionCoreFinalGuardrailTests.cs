using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Targets.Abstractions;
using Musoq.Targets.Execution;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class PortableExecutionCoreFinalGuardrailTests
{
    [TestMethod]
    public void ClrSidecars_ShouldStayInternalAndOutsideSharedTargetPackages()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var sharedTargetFiles = RepositorySourceScan.ProductionSourceFiles(
            repositoryRoot,
            "Musoq.Converter",
            "Musoq.Targets.Execution",
            "Musoq.Targets.Execution.Analysis",
            "Musoq.Targets.TestPortable");
        var forbiddenPatterns = new[]
        {
            new Regex(@"\.ClrType\b", RegexOptions.CultureInvariant),
            new Regex(@"\.ClrMethod\b", RegexOptions.CultureInvariant),
            new Regex(@"\bRequireClrType\(", RegexOptions.CultureInvariant),
            new Regex(@"\bRequireClrMethod\(", RegexOptions.CultureInvariant)
        };
        var sharedOffenders = sharedTargetFiles
            .SelectMany(file => forbiddenPatterns
                .Where(pattern => pattern.IsMatch(File.ReadAllText(file)))
                .Select(pattern => $"{RepositorySourceScan.ToRelative(repositoryRoot, file)}:{pattern}"))
            .ToArray();

        Assert.IsEmpty(
            sharedOffenders,
            "Shared converter, execution SPI, analysis, and portable target code must consume portable descriptors, not CLR sidecars: " +
            string.Join(", ", sharedOffenders));

        var csharpFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Targets.CSharpClr");
        var directAccessFiles = csharpFiles
            .Where(file =>
            {
                var text = File.ReadAllText(file);
                return Regex.IsMatch(text, @"\.ClrType\b", RegexOptions.CultureInvariant) ||
                       Regex.IsMatch(text, @"\.ClrMethod\b", RegexOptions.CultureInvariant);
            })
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();
        CollectionAssert.AreEqual(
            new[]
            {
                "src/dotnet/Musoq.Targets.CSharpClr/CSharpClrExecutionCallableCompatibility.cs",
                "src/dotnet/Musoq.Targets.CSharpClr/CSharpClrExecutionTypeCompatibility.cs"
            },
            directAccessFiles,
            "CSharp lowering must access CLR sidecars only through its compatibility helpers.");

        var legacyCodeGeneration = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "Visitors",
            "CodeGeneration",
            "LegacyCodeGenerationSyntaxFactory.cs"));
        var executionMetadataCompatibility = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "Execution",
            "ExecutionColumnMetadataFields.cs"));
        Assert.IsFalse(Regex.IsMatch(
            legacyCodeGeneration,
            @"\.ClrType\b",
            RegexOptions.CultureInvariant));
        StringAssert.Contains(
            legacyCodeGeneration,
            "ExecutionColumnMetadataFields.RequireClrTypeForLegacyCodeGeneration");
        StringAssert.Contains(executionMetadataCompatibility, "return field.Type.ClrType;");
    }

    [TestMethod]
    public void HostAbi_ShouldHaveOneTypedInventoryModel()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var productionFiles = RepositorySourceScan.ProductionSourceFiles(
            repositoryRoot,
            "Musoq.Targets.Abstractions",
            "Musoq.Targets.Execution",
            "Musoq.Targets.Execution.Analysis",
            "Musoq.Evaluator",
            "Musoq.Converter",
            "Musoq.Targets.CSharpClr");
        var inventoryDeclaration = new Regex(
            @"\b(?:class|record)\s+TargetHostAbiInventory\b",
            RegexOptions.CultureInvariant);
        var declarations = productionFiles
            .Where(file => inventoryDeclaration.IsMatch(File.ReadAllText(file)))
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .ToArray();
        var legacyHostImports = productionFiles
            .Where(file => Regex.IsMatch(
                File.ReadAllText(file),
                @"\bTargetHostImport\b",
                RegexOptions.CultureInvariant))
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { "src/dotnet/Musoq.Targets.Abstractions/TargetHostAbiInventory.cs" },
            declarations);
        Assert.IsEmpty(legacyHostImports, "The parallel TargetHostImport model must not return.");
        Assert.AreEqual(
            typeof(TargetHostAbiInventory),
            typeof(TargetExportArtifact).GetProperty(nameof(TargetExportArtifact.HostAbiInventory))!.PropertyType);
        Assert.AreEqual(
            typeof(TargetHostAbiInventory),
            typeof(TargetArtifactPackage).GetProperty(nameof(TargetArtifactPackage.HostAbiInventory))!.PropertyType);
    }

    [TestMethod]
    public void ProductionCapabilities_ShouldExplicitlyCoverOperationsAndSemanticsVersionOne()
    {
        CollectionAssert.AreEquivalent(
            ExecutionOperationCatalog.AllOperationIds.ToArray(),
            ExecutionTargetCapabilities.CSharpClr.SupportedOperations.ToArray());
        CollectionAssert.AreEqual(
            new[] { ExecutionSemanticsContract.Version1.Version },
            ExecutionTargetCapabilities.CSharpClr.SupportedSemanticsVersions.Order().ToArray());
        Assert.AreEqual(TargetContractVersions.ExecutionIr, new ExecutionPlan(
            "Q_VersionGuardrail",
            [],
            new ExecutionBlock([])).ExecutionIrVersion);

        var versionsText = File.ReadAllText(Path.Combine(
            RepositorySourceScan.RepositoryRoot(),
            "src",
            "dotnet",
            "Musoq.Targets.Abstractions",
            "TargetContractVersions.cs"));
        StringAssert.Contains(versionsText, "const int ExecutionIr = 1");
        StringAssert.Contains(versionsText, "const int HostAbi = 1");
        StringAssert.Contains(versionsText, "const int PackageFormat = 1");
    }

    [TestMethod]
    public void Documentation_ShouldDescribePortableExecutionCoreInvariants()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var files = new[]
        {
            Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Converter", "ExecutionTargets", "README.md"),
            Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Converter", "copilot-instructions.md"),
            Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Evaluator", "copilot-instructions.md")
        };
        var combined = string.Join("\n", files.Select(File.ReadAllText));
        var requiredTerms = new[]
        {
            "ExecutionTypeRef",
            "ExecutionCallableRef",
            "ExecutionConstantValue",
            "ExecutionRawExpression",
            "ExecutionOperationId",
            "ExecutionTargetOperationReport",
            "ExecutionSemanticsContract",
            "TargetDiagnostic",
            "TargetContractVersions",
            "TargetHostAbiInventory",
            "Musoq.Targets.TestPortable",
            "PortableSubsetProgram",
            "intentional breaking change",
            "no public target selector",
            "public artifact format remains version `2`"
        };

        foreach (var term in requiredTerms)
            StringAssert.Contains(combined, term);
    }
}
