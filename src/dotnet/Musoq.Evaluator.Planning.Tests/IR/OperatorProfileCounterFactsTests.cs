using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class OperatorProfileCounterFactsTests
{
    [TestMethod]
    public void CounterNames_ShouldUseOperatorId()
    {
        var descriptor = CreateDescriptor("op7", "HashProbe");

        Assert.AreEqual("__op7Handle", OperatorProfileCounterFacts.CreateHandleVariableName(descriptor));
        Assert.AreEqual("__op7Scope", OperatorProfileCounterFacts.CreateScopeVariableName(descriptor));
        Assert.AreEqual("__op7InputRows", OperatorProfileCounterFacts.CreateInputRowsVariableName(descriptor));
        Assert.AreEqual("__op7OutputRows", OperatorProfileCounterFacts.CreateOutputRowsVariableName(descriptor));
    }

    [TestMethod]
    public void CounterOnlyFacts_ShouldRecognizeNodesAndProbeMatchLoops()
    {
        var table = new ExecutionVariable("result", typeof(Table));
        var row = new ExecutionVariable("row", typeof(object));
        var matchesLoop = new ExecutionForEach(
            row,
            new ExecutionVariableRead(new ExecutionVariable("hashMatches", typeof(IEnumerable<object>))),
            new ExecutionBlock([]));

        Assert.IsTrue(OperatorProfileCounterFacts.IsCounterOnlyNode(new ExecutionAppendExistingRow(table, row)));
        Assert.IsTrue(OperatorProfileCounterFacts.IsCounterOnlyNode(matchesLoop));
        Assert.IsFalse(OperatorProfileCounterFacts.IsCounterOnlyNode(new ExecutionContinue()));
        Assert.IsTrue(OperatorProfileCounterFacts.IsCounterOnlyDescriptor(
            CreateDescriptor("op3", "ForEach", "ForEach [b in hashMatches]")));
    }

    [TestMethod]
    public void CounterStatements_ShouldRenderInputOutputAndFlushRows()
    {
        var descriptor = CreateDescriptor("op2", "HashProbe");
        var probe = new ExecutionHashProbe(
            new ExecutionVariable("hash", typeof(Dictionary<int, object>)),
            new ExecutionVariable("matches", typeof(IEnumerable<object>)),
            new ExecutionLiteral(1, typeof(int)),
            typeof(int),
            typeof(object),
            new ExecutionBlock([]));

        var input = OperatorProfileCounterFacts.CreateCounterInputRowStatements(descriptor, probe).Single();
        var output = OperatorProfileCounterFacts.CreateCounterOutputRowStatements(
            CreateDescriptor("op4", "AppendExistingRow"),
            new ExecutionAppendExistingRow(new ExecutionVariable("result", typeof(Table)), new ExecutionVariable("row", typeof(object))))
            .Single();
        var flush = OperatorProfileCounterFacts.CreateCounterFlushStatement(
            "profileRecorder",
            nameof(QueryProfileRecorder.AddOperatorOutputRows),
            descriptor,
            "__op2OutputRows");

        Assert.AreEqual("__op2InputRows += 1;", Render(input));
        Assert.AreEqual("__op4OutputRows += 1;", Render(output));
        StringAssert.Contains(Render(flush), "profileRecorder?.AddOperatorOutputRows(__op2Handle, __op2OutputRows);");
    }

    private static ExecutionPlanOperatorDescriptor CreateDescriptor(
        string id,
        string kind,
        string? displayName = null)
    {
        return new ExecutionPlanOperatorDescriptor(
            id,
            displayName ?? kind,
            kind,
            ExecutionPlanOperatorRowCountStrategy.RowProducer);
    }

    private static string Render(StatementSyntax statement)
    {
        return statement.NormalizeWhitespace().ToFullString();
    }
}
