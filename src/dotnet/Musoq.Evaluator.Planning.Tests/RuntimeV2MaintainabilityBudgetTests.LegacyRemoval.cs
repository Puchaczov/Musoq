using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    [TestMethod]
    public void RemovedExecutionContracts_ShouldRemainAbsentFromProductionAssembly()
    {
        string[] removedTypes =
        [
            "Musoq.Evaluator.ParameterSnapshot",
            "Musoq.Evaluator.IStructuralParameterSnapshotProvider",
            "Musoq.Evaluator.StructuralParameterSnapshot",
            "Musoq.Evaluator.StructuralParameterSnapshotter",
            "Musoq.Evaluator.SynchronizedParameterDictionary",
            "Musoq.Evaluator.IR.Execution.ExecutionAdaptExpando",
            "Musoq.Evaluator.IR.Execution.ExecutionMaterializeExpandoList",
            "Musoq.Evaluator.IR.Execution.ExpandoAdapterShape",
            "Musoq.Evaluator.IR.Execution.ExpandoDictionaryAccess"
        ];
        var retained = removedTypes.Where(name => typeof(StrictCastRuntime).Assembly.GetType(name) is not null).ToArray();
        Assert.IsEmpty(retained, "Removed contracts must not return: " + string.Join(", ", retained));
    }

    [TestMethod]
    public void StrictCastExecutionOperations_ShouldHaveNoErasedValueParameters()
    {
        var erased = typeof(StrictCastRuntime).GetMethods()
            .Where(method => method.IsStatic && method.Name.StartsWith("To", StringComparison.Ordinal))
            .Where(method => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(object)))
            .Select(method => method.ToString()).ToArray();
        Assert.IsEmpty(erased, "Execution CAST overloads must remain concretely bound.");
    }

    [TestMethod]
    public void OffsetRangeExecution_ShouldHaveNoErasedKeyReads()
    {
        var text = File.ReadAllText(Path.Combine(FindRepositoryRoot(),
            "src/dotnet/Musoq.Evaluator/Helpers/RangeFrameResolver.cs"));
        Assert.DoesNotContain("Array.GetValue", text);
        Assert.DoesNotContain("object", text);
        Assert.DoesNotContain("Convert.ToDecimal", text);
        Assert.Contains("INumber<TNumber>", text);
    }
    [TestMethod]
    public void MembershipExecutionOperations_ShouldExcludeErasedOverloads()
    {
        var erased = typeof(Operators).GetMethods()
            .Where(method => method.Name is "SqlIn" or "LiteralContains")
            .Where(method => method.GetParameters().Any(parameter =>
                parameter.ParameterType == typeof(object) || parameter.ParameterType == typeof(object[])))
            .Select(method => method.ToString()).ToArray();
        Assert.IsEmpty(erased, "Membership must execute concrete comparisons without object argument arrays.");
    }

    [TestMethod]
    public void MembershipRenderer_ShouldExcludePerRowArgumentArraysAndErasedFactories()
    {
        var root = FindRepositoryRoot();
        var membership = File.ReadAllText(Path.Combine(root,
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionMembershipSyntaxFactory.cs"));
        var dynamicRows = File.ReadAllText(Path.Combine(root,
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.Flow.StateAndDynamic.cs"));
        Assert.DoesNotContain("CreateArrayCreation(typeof(object)", membership);
        Assert.DoesNotContain("Func<object", membership);
        Assert.DoesNotContain("Operators.LiteralContains", membership);
        Assert.DoesNotContain("CreateExpandoAdapterCreation", dynamicRows);
        Assert.DoesNotContain("RenderDynamicResolverRead", dynamicRows);
    }
    [TestMethod]
    public void RowCarrierRenderer_ShouldNotEmitErasedDynamicAdapterClasses()
    {
        var root = FindRepositoryRoot();
        var classes = File.ReadAllText(Path.Combine(root,
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.RowCarrierClasses.cs"));
        var members = File.ReadAllText(Path.Combine(root,
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.ClassMembers.cs"));
        Assert.DoesNotContain("RenderExpandoAdapterClass", classes);
        Assert.DoesNotContain("RenderExpandoAdapterClass", members);
    }
    [TestMethod]
    public void SourceShapeBinding_ShouldRejectErasedDictionaryInsteadOfConstructingAnAdapter()
    {
        var directory = Path.Combine(FindRepositoryRoot(), "src/dotnet/Musoq.Evaluator/IR/Execution");
        var sources = File.ReadAllText(Path.Combine(directory, "ExecutionShapeResolver.Sources.cs"));
        var creation = File.ReadAllText(Path.Combine(directory, "ExecutionShapeResolver.ShapeCreation.cs"));
        Assert.DoesNotContain("new ExpandoAdapterShape", sources);
        Assert.DoesNotContain("new ExpandoAdapterShape", creation);
        Assert.Contains("SourcePlanContractException", sources);
        Assert.Contains("SourcePlanContractException", creation);
    }
}
