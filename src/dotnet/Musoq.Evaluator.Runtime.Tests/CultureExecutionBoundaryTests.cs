using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

[CultureTestClass]
public sealed class CultureExecutionBoundaryTests
{
    private readonly string _constructorCulture = CultureInfo.CurrentCulture.Name;
    private readonly string _constructorUiCulture = CultureInfo.CurrentUICulture.Name;
    private string _setupCulture = string.Empty;
    private string _setupUiCulture = string.Empty;

    [TestInitialize]
    public void Initialize()
    {
        _setupCulture = CultureInfo.CurrentCulture.Name;
        _setupUiCulture = CultureInfo.CurrentUICulture.Name;
    }

    [TestMethod]
    [DataRow("sync")]
    [DataRow("async")]
    public async Task DataDrivenInvocation_PreservesFixtureAndMethodCulture(string mode)
    {
        if (mode == "async")
            await Task.Yield();
        Assert.AreEqual("pl-PL", _constructorCulture);
        Assert.AreEqual("pl-PL", _constructorUiCulture);
        Assert.AreEqual("pl-PL", _setupCulture);
        Assert.AreEqual("pl-PL", _setupUiCulture);
        Assert.AreEqual("pl-PL", CultureInfo.CurrentCulture.Name);
        Assert.AreEqual("pl-PL", CultureInfo.CurrentUICulture.Name);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Assert.AreEqual("pl-PL", CultureInfo.CurrentCulture.Name);
        Assert.AreEqual("pl-PL", CultureInfo.CurrentUICulture.Name);
    }
}
