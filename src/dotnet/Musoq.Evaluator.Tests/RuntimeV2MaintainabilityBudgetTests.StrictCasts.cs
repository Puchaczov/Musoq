using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    [TestMethod]
    public void StrictCastLogicalLowering_ShouldUseDedicatedStrictCastIr()
    {
        var repositoryRoot = FindRepositoryRoot();
        var converterText = File.ReadAllText(ToAbsolutePath(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/CastExpressionConverter.cs"));

        Assert.Contains("private StrictCast ConvertCast(CastNode node)", converterText);
        Assert.Contains("return new StrictCast(", converterText);
        Assert.IsFalse(
            converterText.Contains("new MethodCall(", StringComparison.Ordinal),
            "Postfix casts must not lower to generic MethodCall IR.");
        Assert.IsFalse(
            converterText.Contains("TryGetMethod", StringComparison.Ordinal),
            "Postfix cast lowering must not resolve methods by reflection.");
    }

    [TestMethod]
    public void StrictCastExecutionLowering_WhenCompiled_ShouldPreserveDedicatedExecutionIr()
    {
        var inspection = CompileGeneratedSampleForInspection("Q150_RuntimeV2CastProjection.cs");
        Assert.IsNotNull(inspection.ExecutionPlan);

        var expressions = ExecutionIrAnalysis
            .FlattenExpressions(inspection.ExecutionPlan.Body)
            .ToArray();
        var strictCasts = expressions.OfType<ExecutionStrictCast>().ToArray();
        var castLikeMethodCalls = expressions
            .OfType<ExecutionMethodCall>()
            .Where(static method => IsStrictCastHelperName(method.Method.Name))
            .Select(static method => $"{method.Method.DeclaringType?.FullName}.{method.Method.Name}")
            .ToArray();

        Assert.HasCount(3, strictCasts);
        Assert.IsEmpty(
            castLikeMethodCalls,
            "Postfix casts must remain ExecutionStrictCast nodes, not execution MethodCall nodes: " +
            string.Join(", ", castLikeMethodCalls));
    }

    [TestMethod]
    public void StrictCastGeneratedCode_WhenSourceTypesAreKnown_ShouldUseStrictRuntimeCallsWithoutFallbackOrBoxing()
    {
        string[] sampleFileNames =
        [
            "Q150_RuntimeV2CastProjection.cs",
            "Q151_RuntimeV2CastExpressions.cs",
            "Q152_RuntimeV2CastAggregateGrouping.cs",
            "Q154_RuntimeV2GroupByAllCasts.cs",
            "Q158_RuntimeV2CombinedGrouping.cs"
        ];
        string[] forbiddenPatterns =
        [
            "System.Reflection",
            "MethodInfo",
            ".GetMethod(",
            "Convert.ChangeType",
            "int.Parse(",
            "decimal.Parse(",
            "Guid.Parse(",
            "System.Convert.To",
            "__agg0Input = (object)"
        ];

        var failures = sampleFileNames
            .Select(fileName => new
            {
                FileName = fileName,
                Code = CompileGeneratedSampleForInspection(fileName).GeneratedCSharpCode
            })
            .SelectMany(sample => forbiddenPatterns
                .Where(pattern => sample.Code.Contains(pattern, StringComparison.Ordinal))
                .Select(pattern => $"{sample.FileName}: {pattern}"))
            .ToArray();

        Assert.IsEmpty(
            failures,
            "Known typed postfix casts should render direct strict runtime calls, without reflection/fallback/boxing patterns: " +
            string.Join(", ", failures));

        foreach (var fileName in sampleFileNames)
        {
            var code = CompileGeneratedSampleForInspection(fileName).GeneratedCSharpCode;
            Assert.Contains("global::Musoq.Evaluator.Helpers.StrictCastRuntime.", code);
        }
    }

    [TestMethod]
    public void StrictCastCodegenFiles_ShouldNotReferenceReflectionLookupApis()
    {
        var repositoryRoot = FindRepositoryRoot();
        string[] relativeFiles =
        [
            "src/dotnet/Musoq.Evaluator/IR/CastExpressionConverter.cs",
            "src/dotnet/Musoq.Evaluator/IR/Execution/ExecutionExpressionConverter.cs",
            "src/dotnet/Musoq.Evaluator/IR/Execution/ExecutionCSharpRenderer.StrictCastExpressionRendering.cs",
            "src/dotnet/Musoq.Evaluator/Helpers/StrictCastRuntime.cs"
        ];
        string[] forbiddenPatterns =
        [
            "System.Reflection",
            "MethodInfo",
            ".GetMethod(",
            ".GetMethods(",
            "TryGetMethod",
            "Convert.ChangeType"
        ];

        var offenders = relativeFiles
            .Select(relativePath => new
            {
                RelativePath = relativePath,
                Text = File.ReadAllText(ToAbsolutePath(repositoryRoot, relativePath))
            })
            .SelectMany(file => forbiddenPatterns
                .Where(pattern => file.Text.Contains(pattern, StringComparison.Ordinal))
                .Select(pattern => $"{file.RelativePath}: {pattern}"))
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Strict-cast lowering/rendering/runtime code must not use reflection lookup APIs: " +
            string.Join(", ", offenders));
    }

    private static QueryInspectionResult CompileGeneratedSampleForInspection(string fileName)
    {
        var sample = GeneratedCodeSamplesCatalog.GetByFileName(fileName);

        return InstanceCreator.CompileForInspection(
            sample.Query,
            $"GeneratedSample_{Path.GetFileNameWithoutExtension(sample.FileName)}_StrictCastGuard",
            sample.CreateSchemaProvider(),
            new TestsLoggerResolver(),
            sample.CompilationOptions);
    }

    private static bool IsStrictCastHelperName(string methodName)
    {
        return methodName is
            "ToBoolean" or
            "ToByte" or
            "ToSByte" or
            "ToInt16" or
            "ToUInt16" or
            "ToInt32" or
            "ToUInt32" or
            "ToInt64" or
            "ToUInt64" or
            "ToSingle" or
            "ToDouble" or
            "ToDecimal" or
            "ToChar" or
            "ToString" or
            "ToDateTime" or
            "ToDateTimeOffset" or
            "ToTimeSpan" or
            "ToGuid";
    }
}
