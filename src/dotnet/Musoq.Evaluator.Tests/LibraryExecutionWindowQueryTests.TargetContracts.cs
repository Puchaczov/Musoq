using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Targets.Execution.Analysis;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
    [TestMethod]
    public void WindowKernel_WhenLowered_ShouldExposePortableOperationsAndExecuteConcreteResults()
    {
        const string query = "select Position, Sum(Amount) over (partition by Partition order by Position) as Total from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(query,
            LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Total", typeof(decimal?)));
        Assert.HasCount(5, rows);
        decimal?[] expected = [1.25m, 4m, 4m, -3m, 2m];
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(expected[index], rows[index].GetValue<decimal?>(1));
        }

        var analysis = InstanceCreator.CreateForAnalyze(query, "TypedWindowTargetContract",
            LibraryExecutionFixture.CreateProvider(), new TestsLoggerResolver());
        var plan = analysis.OptimizedExecutionPlan!;
        var kernel = ExecutionIrAnalysis.FlattenNodes(plan.Body).OfType<ExecutionWindowAggregateKernel>().Single();
        var operations = kernel.Descriptor.LibraryKernel!;
        Assert.AreEqual(kernel.Descriptor.AccumulatorType, operations.StateType);
        Assert.AreEqual(typeof(decimal?), operations.ArgumentTypes.Single().ResolveClrType());
        Assert.AreEqual(typeof(decimal?), operations.ResultType.ResolveClrType());
        var reflectedProperties = typeof(ExecutionAggregateKernelOperations).GetProperties()
            .Where(static property => property.PropertyType == typeof(Type) ||
                typeof(MemberInfo).IsAssignableFrom(property.PropertyType)).ToArray();
        Assert.IsEmpty(reflectedProperties);
        var callables = ExecutionTargetCompatibilityAnalyzer.Analyze(plan).Requirements
            .Where(static requirement => requirement.CallableSymbol is not null)
            .Select(static requirement => requirement.CallableSymbol!.StableName).ToArray();
        Assert.IsTrue(callables.Any(name => name.Contains("SumAggregateKernel") && name.Contains("Reset")));
        Assert.IsTrue(callables.Any(name => name.Contains("SumAggregateKernel") && name.Contains("Set")));
        Assert.IsTrue(callables.Any(name => name.Contains("SumAggregateKernel") && name.Contains("Get")));
        var imports = ExecutionTargetCompatibilityAnalyzer.Analyze(plan).Requirements
            .Where(static requirement => requirement.Kind == ExecutionTargetRequirementKind.PluginInvocation)
            .Select(static requirement => requirement.CallableSymbol!.StableName).ToArray();
        Assert.IsTrue(imports.Any(name => name.Contains("SumAggregateKernel") && name.Contains("Reset")));
        Assert.IsTrue(imports.Any(name => name.Contains("SumAggregateKernel") && name.Contains("Set")));
        Assert.IsTrue(imports.Any(name => name.Contains("SumAggregateKernel") && name.Contains("Get")));
    }
}
