using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.Tests.IR;

internal static class PipelinePlanAssertions
{
    public static LogicalNode UnwrapMultiStatement(LogicalNode node)
    {
        while (node is MultiStatementNode { Statements.Length: > 0 } multi)
            node = multi.Statements[^1];

        return node;
    }

    public static PhysicalNode UnwrapPhysicalMultiStatement(PhysicalNode node)
    {
        while (node is PhysicalMultiStatementNode { Statements.Length: > 0 } multi)
            node = multi.Statements[^1];

        return node;
    }

    public static ProjectNode FindLogicalApplyProject(LogicalNode plan)
    {
        Assert.IsInstanceOfType<MultiStatementNode>(plan);

        var multi = (MultiStatementNode)plan;
        var applyProject = Array.Find(multi.Statements, statement => statement is ProjectNode { Input: ApplyNode });

        Assert.IsNotNull(applyProject);
        return (ProjectNode)applyProject;
    }

    public static PhysicalProjectNode FindPhysicalApplyProject(PhysicalNode plan)
    {
        Assert.IsInstanceOfType<PhysicalMultiStatementNode>(plan);

        var multi = (PhysicalMultiStatementNode)plan;
        var applyProject = Array.Find(
            multi.Statements,
            statement => statement is PhysicalProjectNode { Input: PhysicalNestedLoopApplyNode });

        Assert.IsNotNull(applyProject);
        return (PhysicalProjectNode)applyProject;
    }

    public static void AssertFinalLogicalStatementUsesCteRef(LogicalNode plan)
    {
        var finalStatement = UnwrapMultiStatement(plan);

        Assert.IsInstanceOfType<ProjectNode>(finalStatement);
        var finalProject = (ProjectNode)finalStatement;
        Assert.IsInstanceOfType<CteRefNode>(finalProject.Input);
    }

    public static void AssertFinalPhysicalStatementUsesCteRef(PhysicalNode plan)
    {
        var finalStatement = UnwrapPhysicalMultiStatement(plan);

        Assert.IsInstanceOfType<PhysicalProjectNode>(finalStatement);
        var finalProject = (PhysicalProjectNode)finalStatement;
        Assert.IsInstanceOfType<PhysicalCteRefNode>(finalProject.Input);
    }
}