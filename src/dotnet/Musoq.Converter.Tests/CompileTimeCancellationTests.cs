using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class CompileTimeCancellationTests
{
    private const string Query = "select i.Value from #cooperative.items() i";
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void CompileWithDiagnostics_WhenPreCancelled_ShouldNotEnterProvider()
    {
        using var fixture = new CooperativeCompileTimeFixture(CooperativeCompileTimeStage.None);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var exception = Assert.Throws<OperationCanceledException>(() => InstanceCreator.CompileWithDiagnostics(
            Query,
            "CancellationPreflight",
            fixture.Provider,
            _loggerResolver,
            fixture.CreateCompilationOptions(),
            cancellation.Token));

        Assert.AreEqual(cancellation.Token, exception.CancellationToken);
        Assert.AreEqual(0, fixture.GetSchemaCount);
        Assert.IsEmpty(fixture.MetadataTokens);
    }

    [TestMethod]
    public async Task CompileWithDiagnostics_WhenRawConstructorsAreCancelled_ShouldRethrowCancellation()
    {
        using var fixture = new CooperativeCompileTimeFixture(CooperativeCompileTimeStage.RawConstructors);

        var token = await CancelAtStageAsync(
            fixture,
            cancellationToken => InstanceCreator.CompileWithDiagnostics(
                Query,
                $"CancellationRaw_{Guid.NewGuid():N}",
                fixture.Provider,
                _loggerResolver,
                fixture.CreateCompilationOptions(),
                cancellationToken));

        AssertMetadataTokens(fixture, token);
        Assert.IsFalse(fixture.StageCompleted);
    }

    [TestMethod]
    public async Task CompileWithDiagnostics_WhenTableLookupIsCancelled_ShouldRethrowCancellation()
    {
        using var fixture = new CooperativeCompileTimeFixture(CooperativeCompileTimeStage.TableLookup);

        var token = await CancelAtStageAsync(
            fixture,
            cancellationToken => InstanceCreator.CompileWithDiagnostics(
                Query,
                $"CancellationTable_{Guid.NewGuid():N}",
                fixture.Provider,
                _loggerResolver,
                fixture.CreateCompilationOptions(),
                cancellationToken));

        AssertMetadataTokens(fixture, token);
        Assert.IsFalse(fixture.StageCompleted);
    }

    [TestMethod]
    public async Task CompileWithDiagnostics_WhenAliasedLookupIsCancelled_ShouldRethrowCancellation()
    {
        using var fixture = new CooperativeCompileTimeFixture(CooperativeCompileTimeStage.AliasedLookup);

        var token = await CancelAtStageAsync(
            fixture,
            cancellationToken => InstanceCreator.CompileWithDiagnostics(
                "table Items { Value: string }; couple #cooperative.items with table Items as Source; select Value from Source()",
                $"CancellationAlias_{Guid.NewGuid():N}",
                fixture.Provider,
                _loggerResolver,
                fixture.CreateCompilationOptions(),
                cancellationToken));

        AssertMetadataTokens(fixture, token);
        Assert.IsFalse(fixture.StageCompleted);
    }

    [TestMethod]
    public async Task CompileForInspection_WhenSourceDescriptionIsCancelled_ShouldRethrowCancellation()
    {
        using var fixture = new CooperativeCompileTimeFixture(CooperativeCompileTimeStage.SourceDescription);

        var token = await CancelAtStageAsync(
            fixture,
            cancellationToken => InstanceCreator.CompileForInspection(
                Query,
                $"CancellationDescription_{Guid.NewGuid():N}",
                fixture.Provider,
                _loggerResolver,
                fixture.CreateCompilationOptions(),
                cancellationToken));

        AssertMetadataTokens(fixture, token);
        Assert.IsFalse(fixture.StageCompleted);
    }

    [TestMethod]
    public async Task CompileWithDiagnostics_WhenRuntimeSettingsDescriptionIsCancelled_ShouldRethrowCancellation()
    {
        using var fixture = new CooperativeCompileTimeFixture(
            CooperativeCompileTimeStage.RuntimeSettingsDescription);

        var token = await CancelAtStageAsync(
            fixture,
            cancellationToken => InstanceCreator.CompileWithDiagnostics(
                Query,
                $"CancellationSettingsDescription_{Guid.NewGuid():N}",
                fixture.Provider,
                _loggerResolver,
                fixture.CreateCompilationOptions(),
                cancellationToken));

        AssertMetadataTokens(fixture, token);
        Assert.IsFalse(fixture.StageCompleted);
    }

    [TestMethod]
    public async Task CompileWithDiagnostics_WhenRuntimeSettingsResolverIsCancelled_ShouldRethrowCancellation()
    {
        using var fixture = new CooperativeCompileTimeFixture(
            CooperativeCompileTimeStage.RuntimeSettingsResolution);

        var token = await CancelAtStageAsync(
            fixture,
            cancellationToken => InstanceCreator.CompileWithDiagnostics(
                Query,
                $"CancellationSettingsResolution_{Guid.NewGuid():N}",
                fixture.Provider,
                _loggerResolver,
                fixture.CreateCompilationOptions(),
                cancellationToken));

        AssertMetadataTokens(fixture, token);
        Assert.AreEqual(token, fixture.RuntimeSettingsToken);
        Assert.IsFalse(fixture.StageCompleted);
    }

    [TestMethod]
    public async Task CompileForInspection_WhenSourcePlanningIsCancelled_ShouldRethrowCancellation()
    {
        using var fixture = new CooperativeCompileTimeFixture(CooperativeCompileTimeStage.SourcePlanning);

        var token = await CancelAtStageAsync(
            fixture,
            cancellationToken => InstanceCreator.CompileForInspection(
                Query,
                $"CancellationPlanning_{Guid.NewGuid():N}",
                fixture.Provider,
                _loggerResolver,
                fixture.CreateCompilationOptions(),
                cancellationToken));

        AssertMetadataTokens(fixture, token);
        Assert.AreEqual(token, fixture.PlanningToken);
        Assert.IsFalse(fixture.StageCompleted);
    }

    [TestMethod]
    public async Task QueryAnalyzer_WhenMetadataIsCancelled_ShouldRethrowCancellation()
    {
        using var fixture = new CooperativeCompileTimeFixture(CooperativeCompileTimeStage.RawConstructors);
        var analyzer = new QueryAnalyzer(
            fixture.Provider,
            compilationOptions: fixture.CreateCompilationOptions());

        var token = await CancelAtStageAsync(fixture, cancellationToken => analyzer.Analyze(Query, cancellationToken));

        AssertMetadataTokens(fixture, token);
        Assert.IsFalse(fixture.StageCompleted);
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenNotCancelled_ShouldNotReportCompilerCancellationDiagnostics()
    {
        using var fixture = new CooperativeCompileTimeFixture(CooperativeCompileTimeStage.None);
        var result = InstanceCreator.CompileWithDiagnostics(
            Query,
            $"CancellationSuccess_{Guid.NewGuid():N}",
            fixture.Provider,
            _loggerResolver,
            fixture.CreateCompilationOptions());

        try
        {
            Assert.IsTrue(
                result.Succeeded,
                string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString())));
            Assert.IsFalse(result.Diagnostics.Any(IsCancellationDiagnostic));
        }
        finally
        {
            result.CompiledQuery?.Dispose();
        }
    }

    [TestMethod]
    public void LegacyCustomBuildChain_ShouldObserveCancellationTokenNone()
    {
        CancellationToken? observedToken = null;
        var result = InstanceCreator.CompileForExecution(
            "select d.Dummy from #system.dual() d",
            $"CancellationLegacyChain_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver,
            () => new ObservingBuildChain(
                new CreateTree(new TransformTree(new TurnQueryIntoRunnableCode(null), _loggerResolver)),
                token => observedToken = token),
            static _ => { });

        try
        {
            Assert.AreEqual(CancellationToken.None, observedToken);
        }
        finally
        {
            result.Dispose();
        }
    }

    [TestMethod]
    public void CustomBuildChain_WhenItCancelsAfterBuild_ShouldRethrowOriginalCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var exception = Assert.Throws<OperationCanceledException>(() => InstanceCreator.CompileForExecution(
            "select d.Dummy from #system.dual() d",
            $"CancellationAfterBuild_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver,
            () => new CancellingBuildChain(
                new CreateTree(new TransformTree(new TurnQueryIntoRunnableCode(null), _loggerResolver)),
                cancellation),
            static _ => { },
            cancellation.Token));

        Assert.AreEqual(cancellation.Token, exception.CancellationToken);
    }

    private static bool IsCancellationDiagnostic(Diagnostic diagnostic)
    {
        return diagnostic.Code is DiagnosticCode.MQ9001_InternalCompilerError or
            DiagnosticCode.MQ8002_CompiledArtifactIncompatible;
    }

    private static void AssertMetadataTokens(
        CooperativeCompileTimeFixture fixture,
        CancellationToken expectedToken)
    {
        Assert.IsNotEmpty(fixture.MetadataTokens);
        Assert.IsTrue(fixture.MetadataTokens.All(token => token == expectedToken));
    }

    private static async Task<CancellationToken> CancelAtStageAsync(
        CooperativeCompileTimeFixture fixture,
        Func<CancellationToken, object?> operation)
    {
        using var cancellation = new CancellationTokenSource();
        var operationTask = Task.Run(() => operation(cancellation.Token));

        try
        {
            var firstCompleted = await Task.WhenAny(fixture.Entered, operationTask);
            Assert.AreSame(
                fixture.Entered,
                firstCompleted,
                $"The cooperative operation completed before entering its cancellation gate. Status={operationTask.Status}; Exception={operationTask.Exception}; Result={DescribeResult(operationTask)}");
            Assert.IsFalse(operationTask.IsCompleted);

            cancellation.Cancel();
            try
            {
                await operationTask;
                Assert.Fail("The cancelled operation returned normally.");
            }
            catch (OperationCanceledException exception)
            {
                Assert.AreEqual(cancellation.Token, exception.CancellationToken);
            }
            return cancellation.Token;
        }
        finally
        {
            fixture.Release();
            if (!operationTask.IsCompleted)
            {
                cancellation.Cancel();
                try
                {
                    await operationTask;
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
    }

    private sealed class ObservingBuildChain(
        BuildChain successor,
        Action<CancellationToken> observe)
        : BuildChain(successor)
    {
        public override void Build(BuildItems items)
        {
            observe(items.CancellationToken);
            Successor?.Build(items);
        }
    }

    private sealed class CancellingBuildChain(
        BuildChain successor,
        CancellationTokenSource cancellation)
        : BuildChain(successor)
    {
        public override void Build(BuildItems items)
        {
            Successor?.Build(items);
            cancellation.Cancel();
        }
    }

    private static string DescribeResult(Task<object?> operationTask)
    {
        return operationTask.Status != TaskStatus.RanToCompletion
            ? string.Empty
            : operationTask.Result is BuildResult result
                ? string.Join(" | ", result.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()))
                : operationTask.Result?.GetType().FullName ?? "<null>";
    }
}
