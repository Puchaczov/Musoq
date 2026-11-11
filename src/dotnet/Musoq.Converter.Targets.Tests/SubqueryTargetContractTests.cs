using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Targets.Execution.Analysis;
using Musoq.Targets.TestPortable;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class SubqueryTargetContractTests
{
    [TestMethod]
    [DataRow(SubqueryResultKind.Exists)]
    [DataRow(SubqueryResultKind.Any)]
    [DataRow(SubqueryResultKind.All)]
    public void Fallback_ShouldRequireExplicitTargetSupportAndInspectItsInnerOperations(SubqueryResultKind kind)
    {
        var inner = new ExecutionPlan("Inner", [], new ExecutionBlock([
            new ExecutionLet(new ExecutionVariable("inner", typeof(int)), new ExecutionStrictCast(
                new ExecutionLiteral("1", typeof(string)), "int", typeof(int)))
        ]));
        var fallback = new ExecutionSubquery("Inner", kind, inner, [], null,
            ExecutionClrBindingFactory.FromClr(typeof(bool)));
        var outer = new ExecutionPlan("Outer", [], new ExecutionBlock([
            new ExecutionLet(new ExecutionVariable("exists", typeof(bool)), fallback)
        ]));
        var operations = ExecutionTargetOperationAnalyzer.Analyze(outer);

        Assert.IsTrue(ExecutionTargetCapabilities.CSharpClr.Validate(operations).IsSupported);
        var portable = PortableSubsetTarget.Capabilities.Validate(operations);
        Assert.IsFalse(portable.IsSupported);
        Assert.IsTrue(portable.UnsupportedOperations.Any(static operation => operation.Value == "expr.subquery"));
        Assert.IsTrue(operations.Operations.Any(static operation => operation.OperationId.Value == "expr.strict-cast"));
    }
}
