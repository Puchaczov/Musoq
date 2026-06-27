using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class TypedActivationGuardrailTests
{
    [TestMethod]
    public void TypedFactories_ShouldUseCompiledRunnableActivators()
    {
        var root = FindRepositoryRoot();
        var converterRoot = Path.Combine(root, "src", "dotnet", "Musoq.Converter");
        var typedFactory = File.ReadAllText(Path.Combine(converterRoot, "TypedRunnableFactory.cs"));
        var profileFactory = File.ReadAllText(Path.Combine(converterRoot, "TypedProfileRunnableFactory.cs"));
        var factoryCore = File.ReadAllText(Path.Combine(converterRoot, "TypedRunnableFactoryCore.cs"));

        Assert.IsFalse(typedFactory.Contains("Activator.CreateInstance", StringComparison.Ordinal));
        Assert.IsFalse(profileFactory.Contains("Activator.CreateInstance", StringComparison.Ordinal));
        Assert.IsFalse(factoryCore.Contains("Activator.CreateInstance", StringComparison.Ordinal));
        StringAssert.Contains(typedFactory, "TypedRunnableFactoryCore<ITypedRunnable<TOut>>");
        StringAssert.Contains(profileFactory, "TypedRunnableFactoryCore<ITableRunnable>");
        StringAssert.Contains(factoryCore, "RunnableActivator.Create<TRunnable>");
    }

    [TestMethod]
    public void PublicTypedWrappers_ShouldNotRecopyCapturedRunOptionParameters()
    {
        var root = FindRepositoryRoot();
        var converterRoot = Path.Combine(root, "src", "dotnet", "Musoq.Converter");
        var publicTypedQuery = File.ReadAllText(Path.Combine(converterRoot, "PublicCompiledTypedQuery.cs"));
        var publicProfileQuery = File.ReadAllText(Path.Combine(converterRoot, "PublicCompiledTypedProfileQuery.cs"));

        Assert.IsFalse(publicTypedQuery.Contains("ParameterSnapshot.CaptureMutableOrEmpty(options.Parameters)", StringComparison.Ordinal));
        Assert.IsFalse(publicProfileQuery.Contains("ParameterSnapshot.CaptureMutableOrEmpty(options.Parameters)", StringComparison.Ordinal));
    }

    [TestMethod]
    public void TypedRunnableWrappers_ShouldUseSharedRunState()
    {
        var root = FindRepositoryRoot();
        var converterRoot = Path.Combine(root, "src", "dotnet", "Musoq.Converter");
        var evaluatorRoot = Path.Combine(root, "src", "dotnet", "Musoq.Evaluator");
        var directTypedQuery = File.ReadAllText(Path.Combine(evaluatorRoot, "CompiledTypedQuery.cs"));
        var directProfileQuery = File.ReadAllText(Path.Combine(converterRoot, "CompiledTypedProfileQuery.cs"));
        var publicTypedQuery = File.ReadAllText(Path.Combine(converterRoot, "PublicCompiledTypedQuery.cs"));
        var publicProfileQuery = File.ReadAllText(Path.Combine(converterRoot, "PublicCompiledTypedProfileQuery.cs"));

        StringAssert.Contains(directTypedQuery, "TypedRunState");
        StringAssert.Contains(directProfileQuery, "TypedRunState");
        StringAssert.Contains(publicTypedQuery, "TypedRunState");
        StringAssert.Contains(publicProfileQuery, "TypedRunState");
        Assert.IsFalse(directTypedQuery.Contains("new Dictionary<string, object?>", StringComparison.Ordinal));
        Assert.IsFalse(directProfileQuery.Contains("new Dictionary<string, object?>", StringComparison.Ordinal));
    }

    [TestMethod]
    public void TypedBuildPaths_ShouldUseSharedBuildProduct()
    {
        var root = FindRepositoryRoot();
        var converterRoot = Path.Combine(root, "src", "dotnet", "Musoq.Converter");
        var typedFactory = File.ReadAllText(Path.Combine(converterRoot, "InstanceCreator.TypedFactory.cs"));
        var profileFactory = File.ReadAllText(Path.Combine(converterRoot, "InstanceCreator.TypedProfileFactory.cs"));
        var artifact = File.ReadAllText(Path.Combine(converterRoot, "InstanceCreator.TypedArtifact.cs"));
        var inspection = File.ReadAllText(Path.Combine(converterRoot, "InstanceCreator.TypedInspection.cs"));
        var buildProduct = File.ReadAllText(Path.Combine(converterRoot, "InstanceCreator.TypedBuildProduct.cs"));

        StringAssert.Contains(typedFactory, "CreateTypedBuildProduct(");
        StringAssert.Contains(profileFactory, "CreateTypedBuildProduct(");
        StringAssert.Contains(artifact, "CreateTypedBuildProduct(");
        StringAssert.Contains(inspection, "CreateTypedBuildProduct(");
        StringAssert.Contains(buildProduct, "TypedQueryDiagnostics.FromMetadata(");
        Assert.IsFalse(typedFactory.Contains("TypedQueryDiagnostics.FromMetadata(", StringComparison.Ordinal));
        Assert.IsFalse(profileFactory.Contains("TypedQueryDiagnostics.FromMetadata(", StringComparison.Ordinal));
        Assert.IsFalse(artifact.Contains("TypedQueryDiagnostics.FromMetadata(", StringComparison.Ordinal));
    }

    [TestMethod]
    public void PublicTypedShorthand_ShouldUseSharedSourceMapper()
    {
        var root = FindRepositoryRoot();
        var converterRoot = Path.Combine(root, "src", "dotnet", "Musoq.Converter");
        var publicApi = File.ReadAllText(Path.Combine(converterRoot, "Musoq.cs"));
        var mapper = File.ReadAllText(Path.Combine(converterRoot, "TypedShorthandSourceMapper.cs"));

        Assert.IsFalse(publicApi.Contains("\"#A\"", StringComparison.Ordinal));
        Assert.IsFalse(publicApi.Contains("\"#B\"", StringComparison.Ordinal));
        Assert.IsFalse(publicApi.Contains("\"#C\"", StringComparison.Ordinal));
        Assert.IsFalse(publicApi.Contains("\"#D\"", StringComparison.Ordinal));
        StringAssert.Contains(publicApi, "TypedShorthandSourceMapper");
        StringAssert.Contains(mapper, "[\"#A\", \"#B\", \"#C\", \"#D\"]");
        StringAssert.Contains(mapper, "private const string SourceName = \"entities\";");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "dotnet", "Musoq.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing src/dotnet/Musoq.sln.");
    }
}
