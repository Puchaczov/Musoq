using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionExpressionConverterTests
{
    [TestMethod]
    public void Convert_WhenScriptParameterRef_ShouldCreateExecutionScriptParameterRead()
    {
        var expression = ExecutionExpressionConverter.Convert(new ScriptParameterRef("author", typeof(string)));

        Assert.IsInstanceOfType<ExecutionScriptParameterRead>(expression);
        var parameterRead = (ExecutionScriptParameterRead)expression;
        Assert.AreEqual("author", parameterRead.Name);
        Assert.AreEqual(typeof(string), parameterRead.ReturnType.ResolveClrType());
    }

    [TestMethod]
    public void Convert_WhenArrayScriptParameterRef_ShouldCreateReadOnlyListParameterRead()
    {
        var expression = ExecutionExpressionConverter.Convert(new ScriptParameterRef("ids", typeof(int[])));

        Assert.IsInstanceOfType<ExecutionScriptParameterRead>(expression);
        var parameterRead = (ExecutionScriptParameterRead)expression;
        Assert.AreEqual("ids", parameterRead.Name);
        Assert.AreEqual(typeof(IReadOnlyList<int>), parameterRead.ReturnType.ResolveClrType());
    }

    [TestMethod]
    public void Convert_WhenCollectionInCheck_ShouldCreateExecutionCollectionInCheck()
    {
        var expression = ExecutionExpressionConverter.Convert(new CollectionInCheck(
            new ColumnRef("p", "Id", typeof(int)),
            new ScriptParameterRef("ids", typeof(int[])),
            typeof(int),
            typeof(bool)));

        Assert.IsInstanceOfType<ExecutionCollectionInCheck>(expression);
        var collectionInCheck = (ExecutionCollectionInCheck)expression;
        Assert.IsInstanceOfType<ExecutionFieldRead>(collectionInCheck.Expression);
        Assert.AreEqual("ids", collectionInCheck.Collection.Name);
        Assert.AreEqual(typeof(IReadOnlyList<int>), collectionInCheck.Collection.ReturnType.ResolveClrType());
        Assert.AreEqual(typeof(int), collectionInCheck.ElementType.ResolveClrType());
        Assert.AreEqual(typeof(bool), collectionInCheck.ReturnType.ResolveClrType());
    }

    [TestMethod]
    public void Convert_WhenScriptVariableRef_ShouldCreateExecutionScriptVariableRead()
    {
        var expression = ExecutionExpressionConverter.Convert(new ScriptVariableRef("topic", typeof(string)));

        Assert.IsInstanceOfType<ExecutionScriptVariableRead>(expression);
        var variableRead = (ExecutionScriptVariableRead)expression;
        Assert.AreEqual("topic", variableRead.Name);
        Assert.AreEqual(typeof(string), variableRead.ReturnType.ResolveClrType());
    }

    [TestMethod]
    public void Convert_WhenTransitionRowColumnNameContainsOriginalAlias_ShouldResolveRootField()
    {
        var shape = new TableRowShape(
            "countriescitiespopulation",
            [
                new FieldBinding(
                    "Population",
                    "countriescitiespopulation.Population",
                    4,
                    typeof(decimal),
                    FieldNullability.Unknown,
                    new PositionalAccess(4))
            ]);
        var lookup = new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase)
        {
            [shape.Alias] = shape
        };

        var expression = ExecutionExpressionConverter.Convert(
            new ColumnRef("countriescitiespopulation", "population.Population", typeof(decimal)),
            lookup);

        Assert.IsInstanceOfType<ExecutionFieldRead>(expression);
        var fieldRead = (ExecutionFieldRead)expression;
        Assert.AreEqual("countriescitiespopulation", fieldRead.Alias);
        Assert.AreEqual("Population", fieldRead.FieldName);
        Assert.AreEqual(new PositionalAccess(4), fieldRead.AccessStrategy);
    }
}
