using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using static Musoq.Evaluator.Tests.LibraryExecutionAggregateQueryTestsFixtures;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionAggregateQueryTests
{
    [TestMethod]
    [DataRow("MissingReset", "Reset(ref State)")]
    [DataRow("ReadOnlySet", "Set(ref State, args...)")]
    [DataRow("MutableGet", "Get(in State)")]
    [DataRow("MutableMergeSource", "Merge(ref State, in State)")]
    [DataRow("SharedState", "shared static fields")]
    [DataRow("ErasedState", "concrete state type")]
    [DataRow("UninferredKernel", "cannot be inferred")]
    [DataRow("UninferredResult", "type parameter R cannot be inferred")]
    public void InvalidKernel_WhenCalledBySql_ShouldDiagnoseItsResponsibleContract(string function, string contract)
    {
        var query = $"select {function}(Amount) as Value from #schema.first()";

        var error = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<LibraryExecutionEntity, InvalidKernelLibrary>(
                query,
                LibraryExecutionFixture.CreateRows()
            )
        );

        Assert.Contains("MQ3071", error.Message);
        Assert.Contains(function, error.Message);
        Assert.Contains(contract, error.Message);
    }
}
