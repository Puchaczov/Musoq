using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class CaseNullabilityAnalysisTests
{
    [TestMethod]
    [DataRow(true, "a", typeof(int))]
    [DataRow(false, "a", typeof(int?))]
    [DataRow(true, "other", typeof(int?))]
    public void SemanticCaseNullGuard_ShouldRequireTheSameStableColumn(bool stable, string checkedAlias, Type expected)
    {
        var value = new AccessColumnNode("Value", "a", typeof(int?), TextSpan.Empty);
        var condition = new IsNullNode(
            new AccessColumnNode("Value", checkedAlias, typeof(int?), TextSpan.Empty),
            false
        );
        var source = new CaseNode([(new WhenNode(condition), new ThenNode(new IntegerNode(0)))], new ElseNode(value));

        var result = CaseExpressionBinding.Bind(
            source,
            source.WhenThenPairs,
            source.Else,
            [typeof(int), typeof(int?)],
            _ => stable
        );

        Assert.AreEqual(expected, result.ReturnType);
    }

    [TestMethod]
    [DataRow(ColumnStability.Stable, "a", typeof(int))]
    [DataRow(ColumnStability.Volatile, "a", typeof(int?))]
    [DataRow(ColumnStability.Stable, "other", typeof(int?))]
    public void IrCaseNullGuard_ShouldRequireTheSameStableColumn(
        ColumnStability stability,
        string checkedAlias,
        Type expected
    )
    {
        var value = new ColumnRef("a", "Value", typeof(int?)) { Stability = stability };
        var condition = new IsNullCheck(new ColumnRef(checkedAlias, "Value", typeof(int?)), false, typeof(bool));

        var result = IrExpressionNullSemantics.CaseResultType(
            typeof(int),
            [new CaseWhenBranch(condition, new Literal(0, typeof(int)))],
            value
        );

        Assert.AreEqual(expected, result);
    }
}
