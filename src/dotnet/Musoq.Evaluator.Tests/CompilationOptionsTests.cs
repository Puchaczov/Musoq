using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CompilationOptionsTests
{
    [TestMethod]
    public void WithInstrumentationMode_WhenTableResultMaterializationIsForced_ShouldPreserveFlag()
    {
        var options = new CompilationOptions()
            .WithTableResultMaterialization()
            .WithInstrumentationMode(QueryInstrumentationMode.Full);

        Assert.IsTrue(options.ForceTableResultMaterialization);
        Assert.AreEqual(QueryInstrumentationMode.Full, options.InstrumentationMode);
    }
}
