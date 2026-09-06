using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Evaluator.Build;
using Musoq.Evaluator.Runtime;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class CompileTimeCancellationApiShapeTests
{
    [TestMethod]
    public void InstanceCreatorCompileTimeApis_ShouldKeepLegacyAndTokenFinalOverloads()
    {
        AssertLegacyAndTokenOverloads(
            typeof(InstanceCreator),
            "CreateForAnalyze",
            "CompileForInspection",
            "GetLogicalPlanText",
            "GetPhysicalPlanText",
            "GetGeneratedCSharpCode",
            "CompileForStore",
            "CompileForStoreAsync",
            "CompileForExecution",
            "CompileForTypedExecution",
            "CompileWithDiagnostics",
            "CompileWithDiagnosticsAsync",
            "CompileArtifactWithDiagnostics",
            "CreateExecutableFromArtifactWithDiagnostics",
            "CompileForTypedArtifact",
            "LoadTypedArtifact",
            "CompileForTypedInspection",
            "CompileForTypedProfile",
            "Profile",
            "CompileForProfile",
            "ExplainAnalyze",
            "CompileForExplainAnalyze");
    }

    [TestMethod]
    public void BuilderAndShorthandCompileTimeApis_ShouldKeepLegacyAndTokenFinalOverloads()
    {
        AssertLegacyAndTokenOverloads(
            typeof(MusoqQueryBuilder),
            "Compile",
            "CompileArtifact",
            "InspectTyped",
            "CompileForProfile",
            "CompileAndRun");
        AssertLegacyAndTokenOverloads(typeof(Musoq), "Compile", "Load", "CompileAndRun");
        AssertLegacyAndTokenOverloads(typeof(QueryAnalyzer), "Analyze");
        AssertLegacyAndTokenOverloads(typeof(InterpreterCompilationUnit), "Compile");
    }

    [TestMethod]
    public void CompatibilityBoundaries_ShouldRetainTheirLegacyShapes()
    {
        var build = typeof(BuildChain).GetMethod(nameof(BuildChain.Build));
        Assert.IsNotNull(build);
        Assert.HasCount(1, build.GetParameters());
        Assert.AreEqual(typeof(BuildItems), build.GetParameters()[0].ParameterType);

        Assert.HasCount(1, typeof(CompiledQueryArtifactLoader).GetMethod("Invoke")!.GetParameters());
        Assert.HasCount(2, typeof(CompiledQueryArtifactLoaderWithCancellation).GetMethod("Invoke")!.GetParameters());
        Assert.HasCount(1, typeof(CompiledQueryArtifactTypeLoader).GetMethod("Invoke")!.GetParameters());
        Assert.HasCount(2, typeof(CompiledQueryArtifactTypeLoaderWithCancellation).GetMethod("Invoke")!.GetParameters());
        Assert.IsNotNull(typeof(CompilationContextManager).GetConstructor(
            [typeof(CSharpCompilation), typeof(EvaluatorRuntimeEnvironment)]));
        Assert.IsNotNull(typeof(CompilationContextManager).GetConstructor(
            [typeof(CSharpCompilation), typeof(EvaluatorRuntimeEnvironment), typeof(CancellationToken)]));
        Assert.AreEqual(CancellationToken.None, new BuildItems().CancellationToken);
    }

    private static void AssertLegacyAndTokenOverloads(Type type, params string[] names)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (var name in names)
        {
            var overloads = methods.Where(method => method.Name == name).ToArray();
            Assert.IsNotEmpty(overloads, $"No public {type.Name}.{name} overload was found.");
            Assert.IsTrue(
                overloads.Any(method => HasFinalCancellationToken(method)),
                $"No token-final {type.Name}.{name} overload was found.");
            Assert.IsTrue(
                overloads.Any(method => !HasFinalCancellationToken(method)),
                $"No legacy {type.Name}.{name} overload was found.");
            Assert.IsTrue(
                overloads
                    .Where(method => method.GetParameters().Any(parameter =>
                        parameter.ParameterType == typeof(CancellationToken)))
                    .All(HasFinalCancellationToken),
                $"A {type.Name}.{name} overload places CancellationToken before its final parameter.");
        }
    }

    private static bool HasFinalCancellationToken(MethodInfo method)
    {
        var parameters = method.GetParameters();
        return parameters.Length > 0 && parameters[^1].ParameterType == typeof(CancellationToken);
    }
}
