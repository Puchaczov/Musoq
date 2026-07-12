using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class ReleasePackagingGuardrailTests
{
    [TestMethod]
    public void InternalTargetProjects_ShouldNotBePackableAndShouldGenerateDocumentation()
    {
        foreach (var project in InternalTargetProjects)
        {
            var text = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "dotnet", project, $"{project}.csproj"));

            StringAssert.Contains(text, "<IsPackable>false</IsPackable>");
            StringAssert.Contains(text, "<GenerateDocumentationFile>true</GenerateDocumentationFile>");
        }
    }

    [TestMethod]
    public void ConverterPackage_ShouldBundleOnlyItsInternalTargetAssemblies()
    {
        var converterProject = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "Musoq.Converter.csproj"));

        foreach (var targetProject in InternalTargetProjects)
        {
            StringAssert.Contains(converterProject, $"..\\{targetProject}\\{targetProject}.csproj\" PrivateAssets=\"All\"");
            StringAssert.Contains(converterProject, targetProject);
        }

        StringAssert.Contains(converterProject, "IncludeBundledTargetAssemblies");
        StringAssert.Contains(converterProject, "IncludeBundledTargetAssemblySymbols");
        StringAssert.Contains(converterProject, "Microsoft.CodeAnalysis.CSharp");
        Assert.IsFalse(converterProject.Contains("Musoq.Targets.TestPortable", StringComparison.Ordinal));
    }

    [TestMethod]
    public void EvaluatorPackage_ShouldBundleTargetAbstractionsForStandaloneEvaluatorConsumers()
    {
        var evaluatorProject = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "Musoq.Evaluator.csproj"));

        StringAssert.Contains(evaluatorProject, "..\\Musoq.Targets.Abstractions\\Musoq.Targets.Abstractions.csproj\" PrivateAssets=\"All\"");
        StringAssert.Contains(evaluatorProject, "IncludeBundledTargetAbstractions");
        StringAssert.Contains(evaluatorProject, "IncludeBundledTargetAbstractionSymbols");
        Assert.IsFalse(evaluatorProject.Contains("Musoq.Targets.TestPortable", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ReleaseRegistry_ShouldContainOnlyPublicPackages()
    {
        var registryPath = Path.Combine(RepositoryRoot, "scripts", "release", "packages.json");
        using var document = JsonDocument.Parse(File.ReadAllText(registryPath));
        var packageIds = document.RootElement
            .GetProperty("packages")
            .EnumerateArray()
            .Select(static package => package.GetProperty("packageId").GetString())
            .OrderBy(static packageId => packageId, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                "Musoq.Converter",
                "Musoq.Evaluator",
                "Musoq.Parser",
                "Musoq.Plugins",
                "Musoq.Schema"
            },
            packageIds);
    }

    [TestMethod]
    public void PackageSmoke_ShouldValidateBundledTargetAssembliesBeforeConsumerExecution()
    {
        var smokeScript = File.ReadAllText(Path.Combine(RepositoryRoot, "scripts", "release", "Test-PackageSmoke.ps1"));

        StringAssert.Contains(smokeScript, "Test-ConverterPackageContents");
        StringAssert.Contains(smokeScript, "Test-EvaluatorPackageContents");
        StringAssert.Contains(smokeScript, "Test-only portable target files must not be included");
        StringAssert.Contains(smokeScript, "Consumer restore must not resolve internal Musoq.Targets NuGet packages.");
    }

    private static string[] InternalTargetProjects { get; } =
    [
        "Musoq.Targets.Abstractions",
        "Musoq.Targets.Execution",
        "Musoq.Targets.Execution.Analysis",
        "Musoq.Targets.CSharpClr"
    ];

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "scripts", "release", "packages.json")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Repository root was not found.");
        }
    }
}
