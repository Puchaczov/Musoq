using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public class StandardLibraryCompatibilityTests
{
    private MethodsManager _methodsManager = new();
    private LibraryBase _standardLibrary = new();

    [TestInitialize]
    public void Initialize()
    {
        _methodsManager = new MethodsManager();
        _standardLibrary = new LibraryBase();
        _methodsManager.RegisterLibraries(_standardLibrary);
    }

    [TestMethod]
    public void StandardLibrary_CaseInsensitiveResolutionWorks()
    {
        var testCases = new[]
        {
            ("Trim", [typeof(string)]),
            ("ToUpper", [typeof(string)]),
            ("ToLower", [typeof(string)]),
            ("Abs", new[] { typeof(int?) }),
            ("NewId", Type.EmptyTypes)
        };

        foreach (var (methodName, paramTypes) in testCases)
        {
            var exactSuccess = _methodsManager.TryGetMethod(methodName, paramTypes, null, out _);
            Assert.IsTrue(exactSuccess, $"Exact case should resolve: {methodName}");


            var lowerSuccess =
                _methodsManager.TryGetMethod(methodName.ToLowerInvariant(), paramTypes, null, out var lowerMethod);
            Assert.IsTrue(lowerSuccess, $"Lowercase should resolve: {methodName}");
            lowerMethod = TestNullabilityGuards.Require(lowerMethod, $"lowercase method {methodName}");
            Assert.AreEqual(methodName, lowerMethod.Name, $"Should preserve original name for: {methodName}");


            var upperSuccess =
                _methodsManager.TryGetMethod(methodName.ToUpperInvariant(), paramTypes, null, out var upperMethod);
            Assert.IsTrue(upperSuccess, $"Uppercase should resolve: {methodName}");
            upperMethod = TestNullabilityGuards.Require(upperMethod, $"uppercase method {methodName}");
            Assert.AreEqual(methodName, upperMethod.Name, $"Should preserve original name for: {methodName}");


            var underscoreSuccess = _methodsManager.TryGetMethod(InsertUnderscores(methodName), paramTypes, null,
                out var underscoreMethod);
            Assert.IsTrue(underscoreSuccess, $"Underscore variant should resolve: {methodName}");
            underscoreMethod = TestNullabilityGuards.Require(underscoreMethod, $"underscore method {methodName}");
            Assert.AreEqual(methodName, underscoreMethod.Name, $"Should preserve original name for: {methodName}");
        }
    }

    [TestMethod]
    public void StandardLibrary_DistributionRankingFactoriesShouldBePubliclyDiscoverable()
    {
        var aggregator = new MethodsAggregator(_methodsManager);

        Assert.IsTrue(aggregator.TryResolveWindowFunction("percent_rank", out var percentRank));
        Assert.IsTrue(aggregator.TryResolveWindowFunction("CUME_DIST", out var cumeDist));
        percentRank = TestNullabilityGuards.Require(percentRank, "percent_rank window function");
        cumeDist = TestNullabilityGuards.Require(cumeDist, "CUME_DIST window function");
        Assert.AreEqual(nameof(LibraryBase.WindowPercentRank), percentRank.Name);
        Assert.AreEqual(nameof(LibraryBase.WindowCumeDist), cumeDist.Name);
        Assert.AreEqual(typeof(IWindowFunction<object, double>), percentRank.ReturnType);
        Assert.AreEqual(typeof(IWindowFunction<object, double>), cumeDist.ReturnType);
    }

    [TestMethod]
    public void StandardLibrary_ExactMatchShouldTakePrecedence()
    {
        var commonMethods = new[]
        {
            ("Abs", [typeof(decimal?)]),
            ("Trim", [typeof(string)]),
            ("ToUpper", [typeof(string)]),
            ("ToLower", new[] { typeof(string) }),
            ("NewId", Type.EmptyTypes)
        };

        foreach (var (methodName, paramTypes) in commonMethods)
        {
            var exactSuccess = _methodsManager.TryGetMethod(methodName, paramTypes, null, out var exactMethod);
            Assert.IsTrue(exactSuccess, $"Exact match should work for {methodName}");


            var lowerSuccess =
                _methodsManager.TryGetMethod(methodName.ToLowerInvariant(), paramTypes, null, out var lowerMethod);
            Assert.IsTrue(lowerSuccess, $"Lowercase should work for {methodName}");
            lowerMethod = TestNullabilityGuards.Require(lowerMethod, $"lowercase method {methodName}");

            Assert.AreEqual(exactMethod, lowerMethod,
                $"Exact and case-insensitive should resolve to same method for {methodName}");
            Assert.AreEqual(methodName, lowerMethod.Name, $"Original method name should be preserved for {methodName}");
        }
    }

    [TestMethod]
    public void StandardLibrary_OverloadedMethodsShouldResolveCorrectly()
    {
        var absTests = new[]
        {
            ([typeof(decimal?)], "decimal?"),
            ([typeof(long?)], "long?"),
            (new[] { typeof(int?) }, "int?")
        };

        foreach (var (paramTypes, expectedType) in absTests)
        {
            var exactSuccess = _methodsManager.TryGetMethod("Abs", paramTypes, null, out var exactMethod);
            Assert.IsTrue(exactSuccess, $"Exact 'Abs' should resolve for {expectedType}");


            var lowerSuccess = _methodsManager.TryGetMethod("abs", paramTypes, null, out var lowerMethod);
            Assert.IsTrue(lowerSuccess, $"Lowercase 'abs' should resolve for {expectedType}");
            lowerMethod = TestNullabilityGuards.Require(lowerMethod, $"lowercase Abs {expectedType}");


            var upperSuccess = _methodsManager.TryGetMethod("ABS", paramTypes, null, out var upperMethod);
            Assert.IsTrue(upperSuccess, $"Uppercase 'ABS' should resolve for {expectedType}");
            upperMethod = TestNullabilityGuards.Require(upperMethod, $"uppercase Abs {expectedType}");


            Assert.AreEqual(exactMethod, lowerMethod,
                $"Case variations should resolve to same method for {expectedType}");
            Assert.AreEqual(exactMethod, upperMethod,
                $"Case variations should resolve to same method for {expectedType}");


            Assert.AreEqual("Abs", lowerMethod.Name);
            Assert.AreEqual("Abs", upperMethod.Name);
        }
    }

    [TestMethod]
    public void StandardLibrary_NoNamingConflicts()
    {
        var standardMethods = typeof(LibraryBase)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<BindableMethodAttribute>() != null)
            .GroupBy(m => m.Name)
            .ToArray();

        foreach (var methodGroup in standardMethods)
        {
            var methodName = methodGroup.Key;
            var normalizedName = methodName.ToLowerInvariant().Replace("_", "");


            var conflictingMethods = standardMethods
                .Where(g => g.Key != methodName)
                .Where(g => g.Key.ToLowerInvariant().Replace("_", "") == normalizedName)
                .ToArray();

            Assert.IsEmpty(conflictingMethods,
                $"Method '{methodName}' has potential naming conflicts after normalization: " +
                $"{string.Join(", ", conflictingMethods.Select(g => g.Key))}");
        }
    }

    [TestMethod]
    public void StandardLibrary_PerformanceIsAcceptable()
    {
        var testMethod = "trim";
        var paramTypes = new[] { typeof(string) };


        for (var i = 0; i < 100; i++) _methodsManager.TryGetMethod(testMethod, paramTypes, null, out _);


        var start = DateTime.UtcNow;
        const int iterations = 10000;

        for (var i = 0; i < iterations; i++)
        {
            var success = _methodsManager.TryGetMethod(testMethod, paramTypes, null, out var method);
            Assert.IsTrue(success);
            Assert.IsNotNull(method);
        }

        var elapsed = DateTime.UtcNow - start;
        var msPerResolution = elapsed.TotalMilliseconds / iterations;


        Assert.IsLessThan(0.1,
            msPerResolution, $"Case-insensitive method resolution is too slow: {msPerResolution:F4}ms per resolution");
    }

    [TestMethod]
    public void StandardLibrary_AllMethodsPreserveOriginalName()
    {
        var standardMethods = typeof(LibraryBase)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<BindableMethodAttribute>() != null)
            .Where(m => !HasInjectedParameters(m))
            .Take(10)
            .ToArray();

        foreach (var method in standardMethods)
        {
            var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
            var lowerName = method.Name.ToLowerInvariant();

            var success = _methodsManager.TryGetMethod(lowerName, parameterTypes, null, out var resolvedMethod);

            Assert.IsTrue(success, $"Should resolve {method.Name} via lowercase {lowerName}");
            resolvedMethod = TestNullabilityGuards.Require(resolvedMethod, $"resolved method {lowerName}");
            Assert.AreEqual(method.Name, resolvedMethod.Name,
                $"Should preserve original method name {method.Name}, not return {resolvedMethod.Name}");
        }
    }

    [TestMethod]
    public void StandardLibrary_EdgeCasesHandleCorrectly()
    {
        var success1 = _methodsManager.TryGetMethod("nonexistentmethod", Type.EmptyTypes, null, out _);
        Assert.IsFalse(success1, "Non-existent method should not resolve");


        var success2 = _methodsManager.TryGetMethod("abs", [typeof(string)], null, out _);
        Assert.IsFalse(success2, "Wrong parameter types should not resolve");


        Assert.Throws<ArgumentNullException>(() =>
            _methodsManager.TryGetMethod(null!, Type.EmptyTypes, null, out _));


        Assert.Throws<ArgumentException>(() =>
            _methodsManager.TryGetMethod("", Type.EmptyTypes, null, out _));
    }

    private static string InsertUnderscores(string methodName)
    {
        if (string.IsNullOrEmpty(methodName)) return methodName;

        var result = "";
        for (var i = 0; i < methodName.Length; i++)
        {
            if (i > 0 && char.IsUpper(methodName[i]) && char.IsLower(methodName[i - 1])) result += "_";
            result += char.ToLowerInvariant(methodName[i]);
        }

        return result;
    }

    private static bool HasInjectedParameters(MethodInfo method)
    {
        return method.GetParameters()
            .Any(p => p.GetCustomAttributes()
                .Any(attr => attr.GetType().Name.Contains("Inject")));
    }
}
