using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    private static int CountBudgetedLines(string filePath) => File.ReadLines(filePath)
        .Count(static line => !line.TrimStart().StartsWith("ArgumentNullException.ThrowIfNull(", StringComparison.Ordinal));

    private static string[] EnumerateProductionSourceFiles(string repositoryRoot)
    {
        var sourceRoot = Path.Combine(repositoryRoot, "src", "dotnet");

        return Directory
            .EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.Contains(".Tests", StringComparison.Ordinal))
            .Where(file => !file.Contains(".Benchmarks", StringComparison.Ordinal))
            .ToArray();
    }

    private static int CountOccurrences(string filePath, string pattern)
    {
        return File.ReadLines(filePath)
            .Count(line => line.Contains(pattern, StringComparison.Ordinal));
    }

    private static string ToAbsolutePath(string repositoryRoot, string relativePath)
    {
        return Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var evaluatorDirectory = Path.Combine(current.FullName, "src", "dotnet", "Musoq.Evaluator");
            if (Directory.Exists(evaluatorDirectory))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from the test output directory.");
    }

    private static MethodInfo AssertConcreteLibraryMethod(
        string name,
        Type returnType,
        string category,
        params Type[] parameterTypes)
    {
        var method = typeof(LibraryBase)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .SingleOrDefault(candidate =>
                candidate.Name == name &&
                !candidate.IsGenericMethodDefinition &&
                candidate.ReturnType == returnType &&
                candidate.GetParameters()
                    .Select(static parameter => parameter.ParameterType)
                    .SequenceEqual(parameterTypes));

        Assert.IsNotNull(method, $"Missing LibraryBase overload {name}({string.Join(", ", parameterTypes.Select(static type => type.Name))}).");
        AssertBindableCategory(method, category);
        return method;
    }

    private static MethodInfo AssertConcreteLibraryMethodSignature(
        string name,
        Type returnType,
        params Type[] parameterTypes)
    {
        var method = typeof(LibraryBase)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .SingleOrDefault(candidate =>
                candidate.Name == name &&
                !candidate.IsGenericMethodDefinition &&
                candidate.ReturnType == returnType &&
                candidate.GetParameters()
                    .Select(static parameter => parameter.ParameterType)
                    .SequenceEqual(parameterTypes));

        Assert.IsNotNull(method, $"Missing LibraryBase overload {name}({string.Join(", ", parameterTypes.Select(static type => type.Name))}).");
        return method;
    }

    private static void AssertGenericLibraryMethod(string name, string category, int parameterCount)
    {
        var method = typeof(LibraryBase)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .SingleOrDefault(candidate =>
                candidate.Name == name &&
                candidate.IsGenericMethodDefinition &&
                candidate.GetGenericArguments().Length == 1 &&
                candidate.GetParameters().Length == parameterCount);

        Assert.IsNotNull(method, $"Missing generic LibraryBase overload {name} with {parameterCount} parameter(s).");
        AssertBindableCategory(method, category);
    }

    private static void AssertBindableMethod(
        string name,
        Type returnType,
        params Type[] parameterTypes)
    {
        var method = typeof(LibraryBase)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .SingleOrDefault(candidate =>
                candidate.Name == name &&
                !candidate.IsGenericMethodDefinition &&
                candidate.ReturnType == returnType &&
                candidate.GetParameters()
                    .Select(static parameter => parameter.ParameterType)
                    .SequenceEqual(parameterTypes));

        Assert.IsNotNull(method, $"Missing LibraryBase overload {name}({string.Join(", ", parameterTypes.Select(static type => type.Name))}).");
        Assert.IsNotNull(
            method.GetCustomAttributes(typeof(BindableMethodAttribute), inherit: false).SingleOrDefault(),
            $"Missing BindableMethodAttribute on {method}.");
    }

    private static void AssertBindableCategory(MethodInfo method, string category)
    {
        Assert.IsNotNull(
            method.GetCustomAttributes(typeof(BindableMethodAttribute), inherit: false).SingleOrDefault(),
            $"Missing BindableMethodAttribute on {method}.");

        var methodCategory = method.GetCustomAttributes(typeof(MethodCategoryAttribute), inherit: false)
            .Cast<MethodCategoryAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(methodCategory, $"Missing MethodCategoryAttribute on {method}.");
        Assert.AreEqual(category, methodCategory.Category, $"Unexpected method category on {method}.");
    }

    private sealed record SourceFileBudget(string FileName, int LineCount, int Budget = RuntimeV2FileLineBudget);

    private sealed record SourceFamilyBudget(string RelativeDirectory, string SearchPattern, int MaxFileLines);

    private sealed record SourceFamilyTotalBudget(string RelativeDirectory, string SearchPattern, int MaxTotalLines);
}
