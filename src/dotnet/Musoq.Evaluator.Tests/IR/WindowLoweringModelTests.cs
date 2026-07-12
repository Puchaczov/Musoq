using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class WindowLoweringModelTests
{
    [TestMethod]
    public void WindowRegistrationBuildResult_Factories_ShouldPreserveWindowKind()
    {
        var registration = CreateRegistration("row_number", typeof(long));
        var pluginFactory = typeof(WindowLoweringModelTests)
            .GetMethod(nameof(TestWindow), BindingFlags.Public | BindingFlags.Static)!;

        var ranking = WindowRegistrationBuildResult.SuccessRanking(
            registration,
            ExecutionRankingWindowFunction.RowNumber);
        var offset = WindowRegistrationBuildResult.SuccessOffset(
            registration,
            ExecutionOffsetWindowFunction.Lag);
        var plugin = WindowRegistrationBuildResult.SuccessPlugin(registration, pluginFactory);
        var unsupported = WindowRegistrationBuildResult.Unsupported("unsupported frame");

        Assert.IsTrue(ranking.Supported);
        Assert.AreSame(registration, ranking.Registration);
        Assert.AreEqual(ExecutionRankingWindowFunction.RowNumber, ranking.RankingFunction);
        Assert.IsNull(ranking.OffsetFunction);
        Assert.IsTrue(offset.Supported);
        Assert.AreEqual(ExecutionOffsetWindowFunction.Lag, offset.OffsetFunction);
        Assert.IsNull(offset.RankingFunction);
        Assert.IsTrue(plugin.Supported);
        Assert.AreSame(pluginFactory, plugin.PluginFactory);
        Assert.IsFalse(unsupported.Supported);
        Assert.AreEqual("unsupported frame", unsupported.UnsupportedReason);
    }

    [TestMethod]
    public void WindowComputationBuildResult_Factories_ShouldPreserveNodeAndUnsupportedSentinel()
    {
        var registration = CreateRegistration("rank", typeof(long));
        var node = new ExecutionContinue();
        var results = new ExecutionVariable("ranks", typeof(long[]));

        var success = WindowComputationBuildResult.Success(registration, node, results);
        var unsupported = WindowComputationBuildResult.Unsupported("bad window");

        Assert.IsTrue(success.Supported);
        Assert.AreSame(registration, success.Registration);
        Assert.AreSame(node, success.Node);
        Assert.AreSame(results, success.Results);
        Assert.IsFalse(unsupported.Supported);
        Assert.IsInstanceOfType<ExecutionMaterializeList>(unsupported.Node);
        Assert.AreEqual(string.Empty, unsupported.Results.Name);
        Assert.AreEqual("bad window", unsupported.UnsupportedReason);
    }

    [TestMethod]
    public void OffsetWindowArgumentsBuildResult_Factories_ShouldPreserveExpressions()
    {
        var value = new ExecutionLiteral("alpha", typeof(string));
        var offset = new ExecutionLiteral(2, typeof(int));
        var defaultValue = new ExecutionLiteral("fallback", typeof(string));

        var success = OffsetWindowArgumentsBuildResult.Success(value, offset, defaultValue);
        var unsupported = OffsetWindowArgumentsBuildResult.Unsupported("bad offset");

        Assert.IsTrue(success.Supported);
        Assert.AreSame(value, success.Value);
        Assert.AreSame(offset, success.Offset);
        Assert.AreSame(defaultValue, success.DefaultValue);
        Assert.IsFalse(unsupported.Supported);
        Assert.AreEqual(typeof(object), unsupported.Value.ReturnType.ClrType);
        Assert.AreEqual("bad offset", unsupported.UnsupportedReason);
    }

    [TestMethod]
    public void PluginWindowArgumentsBuildResult_Factories_ShouldPreserveArgumentsAndTargets()
    {
        var value = new ExecutionLiteral(10, typeof(int));
        var arguments = new ExecutionExpression[]
        {
            new ExecutionLiteral("arg", typeof(string))
        };
        var rowScoped = new[] { true };
        var methodTargets = new[]
        {
            new ExecutionVariable("target", typeof(object))
        };

        var success = PluginWindowArgumentsBuildResult.Success(value, arguments, rowScoped, methodTargets);
        var unsupported = PluginWindowArgumentsBuildResult.Unsupported("bad plugin");

        Assert.IsTrue(success.Supported);
        Assert.AreSame(value, success.Value);
        Assert.AreSame(arguments, success.Arguments);
        Assert.AreSame(rowScoped, success.RowScopedArguments);
        Assert.AreSame(methodTargets, success.MethodTargets);
        Assert.IsFalse(unsupported.Supported);
        Assert.HasCount(0, unsupported.Arguments);
        Assert.HasCount(0, unsupported.RowScopedArguments);
        Assert.HasCount(0, unsupported.MethodTargets);
        Assert.AreEqual("bad plugin", unsupported.UnsupportedReason);
    }

    [TestMethod]
    public void WindowComputationContext_ShouldPreservePlanningRegistries()
    {
        var registration = WindowRegistrationBuildResult.SuccessRanking(
            CreateRegistration("row_number", typeof(long)),
            ExecutionRankingWindowFunction.RowNumber);
        var keyArrays = new WindowKeyArrayRegistry();
        var partitions = new WindowPartitionSetRegistry();
        var sortedPartitions = new WindowPartitionSetRegistry();

        var context = new WindowComputationContext(
            registration,
            new ExecutionVariable("buffer", typeof(object)),
            new ExecutionVariable("item", typeof(object)),
            ExecutionRowAccessMode.Direct,
            null,
            [],
            new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            "result",
            WindowResultNameMode.IndexedByWindow,
            keyArrays,
            partitions,
            sortedPartitions,
            null,
            null,
            "partition-list",
            null,
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            QualifyUpperBound: 10);

        Assert.AreSame(registration, context.RegistrationResult);
        Assert.AreSame(keyArrays, context.KeyArrays);
        Assert.AreSame(partitions, context.Partitions);
        Assert.AreSame(sortedPartitions, context.SortedPartitions);
        Assert.AreEqual(WindowResultNameMode.IndexedByWindow, context.ResultNameMode);
        Assert.AreEqual(10, context.QualifyUpperBound);
    }

    public static long TestWindow(int value)
    {
        return value;
    }

    private static WindowRegistration CreateRegistration(string functionName, Type returnType)
    {
        return new WindowRegistration(
            null,
            functionName,
            [],
            [],
            [],
            WindowIndex: 0,
            returnType);
    }
}
