using Musoq.Evaluator.KaitaiStruct.Tests.Infrastructure;

namespace Musoq.Evaluator.KaitaiStruct.Tests.Tests;

[TestClass]
public sealed class CaseSetIntegrityTests
{
    [TestMethod]
    public void Retained_case_data_is_complete()
    {
        var failures = CaseSetIntegrity.FindFailures();

        Assert.AreEqual(0, failures.Length, string.Join(Environment.NewLine, failures));
    }
}
