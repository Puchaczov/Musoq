using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Build;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ExecutionTargetPackageExtractionBaselineTests
{
    [TestMethod]
    public void ExecutionTargetId_ShouldRemainInternal()
    {
        var TargetId = typeof(ExecutionTargetId);

        Assert.AreEqual(
            "Musoq.Targets.Abstractions",
            TargetId.Assembly.GetName().Name,
            "ExecutionTargetId must live in the internal target abstraction assembly after extraction.");
        Assert.IsFalse(TargetId.IsPublic, "ExecutionTargetId must not become public before a target-selection API is designed.");
        Assert.IsFalse(TargetId.IsNestedPublic, "ExecutionTargetId must not become publicly nested before a target-selection API is designed.");
    }

    [TestMethod]
    public void PublicConverterApi_ShouldNotExposeExecutionTargetSelection()
    {
        var converterAssembly = typeof(InstanceCreator).Assembly;
        var TargetId = typeof(ExecutionTargetId);
        var publicTypes = converterAssembly.GetExportedTypes();

        var offenders = publicTypes
            .SelectMany(type => PublicCallableMembers(type).Select(member => new { type, member }))
            .Where(item => ExposesTargetId(item.member, TargetId!) || HasTargetSelectionParameter(item.member))
            .Select(item => $"{item.type.FullName}.{item.member.Name}")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Public converter APIs must not expose target selection yet: " + string.Join(", ", offenders));
    }

    [TestMethod]
    public void TargetBoundaryContracts_ShouldLiveInOwningTargetAssemblies()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var abstractionFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Targets.Abstractions");
        var executionFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Targets.Execution");
        var converterFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Converter");
        var pureContractNames = new[]
        {
            "interface IRenderedQueryFinalizer",
            "interface IRenderedQueryInspector",
            "abstract record RenderedQueryArtifact",
            "abstract record ExecutableQueryArtifact"
        };
        var executionContractNames = new[]
        {
            "interface IQueryExecutionBackend",
            "interface IClrExecutableQueryActivator",
            "record TargetRenderRequest",
            "record TargetRenderResult"
        };

        foreach (var contractName in pureContractNames)
        {
            var abstractionMatches = abstractionFiles
                .Where(file => System.IO.File.ReadAllText(file).Contains(contractName, StringComparison.Ordinal))
                .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
                .ToArray();
            Assert.HasCount(
                1,
                abstractionMatches,
                $"{contractName} must be defined once in Musoq.Targets.Abstractions.");

            var offenders = converterFiles
                .Where(file => System.IO.File.ReadAllText(file).Contains(contractName, StringComparison.Ordinal))
                .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
                .ToArray();

            Assert.IsEmpty(
                offenders,
                $"{contractName} must not be redefined in Musoq.Converter after abstraction extraction: {string.Join(", ", offenders)}");
        }

        foreach (var contractName in executionContractNames)
        {
            var executionMatches = executionFiles
                .Where(file => System.IO.File.ReadAllText(file).Contains(contractName, StringComparison.Ordinal))
                .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
                .ToArray();
            Assert.HasCount(
                1,
                executionMatches,
                $"{contractName} must be defined once in Musoq.Targets.Execution.");

            var offenders = converterFiles
                .Where(file => System.IO.File.ReadAllText(file).Contains(contractName, StringComparison.Ordinal))
                .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
                .ToArray();

            Assert.IsEmpty(
                offenders,
                $"{contractName} must not be redefined in Musoq.Converter after execution SPI extraction: {string.Join(", ", offenders)}");
        }
    }

    [TestMethod]
    public void TargetAbstractionsProject_ShouldNotReferenceConverterOrRoslyn()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var projectFile = System.IO.Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Abstractions",
            "Musoq.Targets.Abstractions.csproj");
        var text = System.IO.File.ReadAllText(projectFile);

        Assert.IsFalse(
            text.Contains("Musoq.Converter.csproj", StringComparison.Ordinal),
            "Target abstractions must not depend on Musoq.Converter.");
        Assert.IsFalse(
            text.Contains("Musoq.Evaluator.csproj", StringComparison.Ordinal),
            "Pure target abstractions must not depend on Musoq.Evaluator.");
        Assert.IsFalse(
            text.Contains("Musoq.Schema.csproj", StringComparison.Ordinal),
            "Pure target abstractions must not depend on Musoq.Schema.");
        Assert.IsFalse(
            text.Contains("Microsoft.CodeAnalysis", StringComparison.Ordinal),
            "Target abstractions must not depend on Roslyn packages.");
        Assert.IsFalse(
            text.Contains("System.Reflection", StringComparison.Ordinal),
            "Target abstractions must not depend on CLR reflection activation packages.");

        var abstractionFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Targets.Abstractions");
        var sourceOffenders = abstractionFiles
            .Where(file => System.IO.File.ReadAllText(file).Contains("Microsoft.CodeAnalysis", StringComparison.Ordinal))
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .ToArray();

        Assert.IsEmpty(
            sourceOffenders,
            "Target abstraction source must not reference Roslyn namespaces: " + string.Join(", ", sourceOffenders));

        var evaluatorOrSchemaOffenders = abstractionFiles
            .Where(file =>
            {
                var fileText = System.IO.File.ReadAllText(file);
                return fileText.Contains("Musoq.Evaluator", StringComparison.Ordinal) ||
                       fileText.Contains("Musoq.Schema", StringComparison.Ordinal);
            })
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .ToArray();

        Assert.IsEmpty(
            evaluatorOrSchemaOffenders,
            "Pure target abstraction source must not reference evaluator or schema namespaces: " +
            string.Join(", ", evaluatorOrSchemaOffenders));

        var reflectionTypePattern = new Regex(
            @"\b(?:System\.)?Type\s+[A-Za-z_]\w*|\b(?:System\.)?Assembly\s+[A-Za-z_]\w*|System\.Reflection",
            RegexOptions.CultureInvariant);
        var reflectionTypeOffenders = abstractionFiles
            .Where(file => reflectionTypePattern.IsMatch(System.IO.File.ReadAllText(file)))
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .ToArray();

        Assert.IsEmpty(
            reflectionTypeOffenders,
            "Pure target abstraction source must not expose CLR reflection Type or Assembly concepts: " +
            string.Join(", ", reflectionTypeOffenders));

        var activationOffenders = abstractionFiles
            .Where(file =>
            {
                var fileText = System.IO.File.ReadAllText(file);
                return fileText.Contains("Assembly.Load(", StringComparison.Ordinal) ||
                       fileText.Contains("Activator.CreateInstance", StringComparison.Ordinal);
            })
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .ToArray();

        Assert.IsEmpty(
            activationOffenders,
            "Target abstraction source must not own CLR assembly loading or activation: " + string.Join(", ", activationOffenders));
    }

    [TestMethod]
    public void TargetPackages_ShouldNotDeclareConverterBuildNamespace()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var targetFiles = RepositorySourceScan.ProductionSourceFiles(
            repositoryRoot,
            "Musoq.Targets.Abstractions",
            "Musoq.Targets.CSharpClr");

        var offenders = targetFiles
            .Where(file => System.IO.File.ReadAllText(file).Contains(
                "namespace Musoq.Converter.Build;",
                StringComparison.Ordinal))
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Target packages must use target-owned namespaces, not converter build namespaces: " + string.Join(", ", offenders));
    }

    [TestMethod]
    public void CSharpTargetProject_ShouldNotReferenceConverter()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var projectFile = System.IO.Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.CSharpClr",
            "Musoq.Targets.CSharpClr.csproj");
        var text = System.IO.File.ReadAllText(projectFile);

        Assert.IsFalse(
            text.Contains("Musoq.Converter.csproj", StringComparison.Ordinal),
            "C# CLR target must not depend on Musoq.Converter; converter composes targets, not the other way around.");
    }

    [TestMethod]
    public void ConcreteCSharpTargetTypes_ShouldLiveInCSharpTargetAssembly()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var converterFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Converter");
        var concreteTargetTypeNames = new[]
        {
            "class CSharpClrExecutionBackend",
            "class CSharpClrRenderedQueryFinalizer",
            "class ClrAssemblyExecutableActivator",
            "class CSharpRenderedQueryInspector",
            "record CSharpRenderedQueryArtifact",
            "record ClrAssemblyExecutableArtifact"
        };

        foreach (var typeName in concreteTargetTypeNames)
        {
            var offenders = converterFiles
                .Where(file => System.IO.File.ReadAllText(file).Contains(typeName, StringComparison.Ordinal))
                .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
                .ToArray();

            Assert.IsEmpty(
                offenders,
                $"{typeName} must be implemented in Musoq.Targets.CSharpClr, not Musoq.Converter: {string.Join(", ", offenders)}");
        }
    }

    private static MemberInfo[] PublicCallableMembers(Type type)
    {
        return type
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(member => member.MemberType is MemberTypes.Constructor or MemberTypes.Method or MemberTypes.Property)
            .ToArray();
    }

    private static bool ExposesTargetId(MemberInfo member, Type TargetId)
    {
        return member switch
        {
            MethodInfo method => method.ReturnType == TargetId || method.GetParameters().Any(parameter => parameter.ParameterType == TargetId),
            ConstructorInfo constructor => constructor.GetParameters().Any(parameter => parameter.ParameterType == TargetId),
            PropertyInfo property => property.PropertyType == TargetId,
            _ => false
        };
    }

    private static bool HasTargetSelectionParameter(MemberInfo member)
    {
        return member switch
        {
            MethodBase method => method
                .GetParameters()
                .Any(parameter => string.Equals(parameter.Name, "executionTarget", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(parameter.Name, "TargetId", StringComparison.OrdinalIgnoreCase)),
            _ => false
        };
    }
}
