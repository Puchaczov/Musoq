using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class CultureTestClassAttributeTests
{
    [TestMethod]
    [DataRow(UnitTestOutcome.Passed)]
    [DataRow(UnitTestOutcome.Failed)]
    public async Task Wrapper_DelegatesOnceAndPreservesResultsAndAttributeMetadata(UnitTestOutcome outcome)
    {
        using var parent = new CultureScope(CultureInfo.GetCultureInfo("en-US"));
        var method = Mock.Of<ITestMethod>();
        TestResult[] expected = [new() { Outcome = outcome }];
        var calls = 0;
        var original = new RecordingAttribute(async supplied =>
        {
            Interlocked.Increment(ref calls);
            Assert.AreSame(method, supplied);
            AssertDefaultCulture();
            await Task.Yield();
            AssertDefaultCulture();
            return expected;
        })
        {
            DisplayName = "preserved display name",
            UnfoldingStrategy = TestDataSourceUnfoldingStrategy.Fold,
        };
        var wrapper = new CultureTestClassAttribute().GetTestMethodAttribute(original);

        var actual = await wrapper.ExecuteAsync(method);

        Assert.AreSame(expected, actual);
        Assert.AreEqual(1, calls);
        Assert.AreEqual("preserved display name", wrapper.DisplayName);
        Assert.AreEqual(TestDataSourceUnfoldingStrategy.Fold, wrapper.UnfoldingStrategy);
        Assert.AreEqual(original.DeclaringFilePath, wrapper.DeclaringFilePath);
        Assert.AreEqual(original.DeclaringLineNumber, wrapper.DeclaringLineNumber);
        Assert.AreEqual("en-US", CultureInfo.CurrentCulture.Name);
        Assert.AreEqual("en-US", CultureInfo.CurrentUICulture.Name);
    }

    [TestMethod]
    public async Task Wrapper_RestoresCultureWhenOriginalExecutionThrows()
    {
        using var parent = new CultureScope(CultureInfo.GetCultureInfo("en-US"));
        var expected = new InvalidOperationException("original failure");
        var original = new RecordingAttribute(async _ =>
        {
            await Task.Yield();
            AssertDefaultCulture();
            throw expected;
        });
        var wrapper = new CultureTestClassAttribute().GetTestMethodAttribute(original);

        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            wrapper.ExecuteAsync(Mock.Of<ITestMethod>())
        );

        Assert.AreSame(expected, actual);
        Assert.AreEqual("en-US", CultureInfo.CurrentCulture.Name);
        Assert.AreEqual("en-US", CultureInfo.CurrentUICulture.Name);
    }

    [TestMethod]
    public async Task Wrapper_PreservesCancellationAndRestoresCulture()
    {
        using var parent = new CultureScope(CultureInfo.GetCultureInfo("en-US"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var expected = new OperationCanceledException(cancellation.Token);
        var original = new RecordingAttribute(async _ =>
        {
            await Task.Yield();
            AssertDefaultCulture();
            throw expected;
        });
        var wrapper = new CultureTestClassAttribute().GetTestMethodAttribute(original);

        var actual = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            wrapper.ExecuteAsync(Mock.Of<ITestMethod>())
        );

        Assert.AreSame(expected, actual);
        Assert.AreEqual(cancellation.Token, actual.CancellationToken);
        Assert.AreEqual("en-US", CultureInfo.CurrentCulture.Name);
        Assert.AreEqual("en-US", CultureInfo.CurrentUICulture.Name);
    }

    [TestMethod]
    public void Wrapper_RejectsMissingOriginalAttribute()
    {
        var failure = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new CultureTestClassAttribute().GetTestMethodAttribute(null!)
        );

        Assert.AreEqual("testMethodAttribute", failure.ParamName);
    }

    [TestMethod]
    public void Wrapper_WhenOriginalMetadataIsMissing_ShouldProvideItsOwnSourceLocation()
    {
        var original = new RecordingAttribute(_ => Task.FromResult(Array.Empty<TestResult>()), missingMetadata: true);
        Assert.IsNull(original.DeclaringFilePath);
        Assert.IsNull(original.DeclaringLineNumber);

        var wrapper = new CultureTestClassAttribute().GetTestMethodAttribute(original);

        Assert.IsNotNull(wrapper.DeclaringFilePath);
        StringAssert.EndsWith(wrapper.DeclaringFilePath, "CultureTestClassAttribute.cs");
        Assert.IsNotNull(wrapper.DeclaringLineNumber);
        Assert.IsGreaterThan(0, wrapper.DeclaringLineNumber.Value);
    }

    private static void AssertDefaultCulture()
    {
        Assert.AreEqual("pl-PL", CultureInfo.CurrentCulture.Name);
        Assert.AreEqual("pl-PL", CultureInfo.CurrentUICulture.Name);
    }

    private sealed class RecordingAttribute(
        Func<ITestMethod, Task<TestResult[]>> execute,
        bool missingMetadata = false,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0
    ) : TestMethodAttribute(missingMetadata ? null! : filePath, missingMetadata ? 0 : lineNumber)
    {
        public override Task<TestResult[]> ExecuteAsync(ITestMethod testMethod) => execute(testMethod);
    }
}
