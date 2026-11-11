using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class CultureScopeTests
{
    private static readonly TimeSpan DeadlockTimeout = TimeSpan.FromMinutes(2);

    [TestMethod]
    public void Scope_RestoresBothCulturesWithoutChangingProcessDefaults()
    {
        using var parent = new CultureScope(CultureInfo.GetCultureInfo("en-US"));
        var defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        var defaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;

        using (new CultureScope(CultureInfo.GetCultureInfo("pl-PL")))
            AssertCulture("pl-PL");

        AssertCulture("en-US");
        Assert.AreSame(defaultCulture, CultureInfo.DefaultThreadCurrentCulture);
        Assert.AreSame(defaultUiCulture, CultureInfo.DefaultThreadCurrentUICulture);
    }

    [TestMethod]
    public void NestedScopes_RestoreInReverseOrderAndDisposeOnlyOnce()
    {
        using var parent = new CultureScope(CultureInfo.GetCultureInfo("en-US"));
        using (new CultureScope(CultureInfo.GetCultureInfo("tr-TR")))
        {
            using var nested = new CultureScope(CultureInfo.GetCultureInfo("pl-PL"));
            AssertCulture("pl-PL");
            nested.Dispose();
            AssertCulture("tr-TR");
            using (new CultureScope(CultureInfo.GetCultureInfo("fr-FR")))
            {
                nested.Dispose();
                AssertCulture("fr-FR");
            }
        }
        AssertCulture("en-US");
    }

    [TestMethod]
    public async Task Scope_FlowsAcrossAwaitAndWorkerTasks()
    {
        using var parent = new CultureScope(CultureInfo.GetCultureInfo("en-US"));
        using (new CultureScope(CultureInfo.GetCultureInfo("pl-PL")))
        {
            await Task.Yield();
            AssertCulture("pl-PL");
            var worker = await Task.Run(() =>
                new[] { CultureInfo.CurrentCulture.Name, CultureInfo.CurrentUICulture.Name }
            );
            CollectionAssert.AreEqual(new[] { "pl-PL", "pl-PL" }, worker);
        }
        AssertCulture("en-US");
    }

    [TestMethod]
    public async Task OverlappingScopes_KeepIndependentExecutionContexts()
    {
        using var parent = new CultureScope(CultureInfo.GetCultureInfo("en-US"));
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var workers = new[] { Run("tr-TR", firstEntered), Run("pl-PL", secondEntered) };
        try
        {
            await Task.WhenAll(firstEntered.Task, secondEntered.Task).WaitAsync(DeadlockTimeout);
            AssertCulture("en-US");
            release.TrySetResult();
            var names = await Task.WhenAll(workers).WaitAsync(DeadlockTimeout);
            CollectionAssert.AreEqual(new[] { "tr-TR", "pl-PL" }, names);
            AssertCulture("en-US");
        }
        finally
        {
            release.TrySetResult();
            await Task.WhenAll(workers).WaitAsync(DeadlockTimeout);
        }

        async Task<string> Run(string cultureName, TaskCompletionSource entered)
        {
            using var scope = new CultureScope(CultureInfo.GetCultureInfo(cultureName));
            entered.TrySetResult();
            await release.Task.WaitAsync(DeadlockTimeout);
            AssertCulture(cultureName);
            return CultureInfo.CurrentCulture.Name;
        }
    }

    [TestMethod]
    public void NullCulture_IsRejectedWithoutChangingTheContext()
    {
        var culture = CultureInfo.CurrentCulture;
        var uiCulture = CultureInfo.CurrentUICulture;

        var failure = Assert.ThrowsExactly<ArgumentNullException>(() => new CultureScope(null!));

        Assert.AreEqual("culture", failure.ParamName);
        Assert.AreSame(culture, CultureInfo.CurrentCulture);
        Assert.AreSame(uiCulture, CultureInfo.CurrentUICulture);
    }

    private static void AssertCulture(string name)
    {
        Assert.AreEqual(name, CultureInfo.CurrentCulture.Name);
        Assert.AreEqual(name, CultureInfo.CurrentUICulture.Name);
    }
}
