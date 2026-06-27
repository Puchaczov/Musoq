using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Plugins;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;
using Musoq.Schema;
using LogicalWindowNode = Musoq.Evaluator.IR.Logical.Nodes.WindowNode;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public partial class LogicalPlanBuilderTests
{
    #region Helpers

    private static SchemaFromNode CreateSchemaFrom(string alias = "t", string schema = "test", string method = "data")
    {
        return new SchemaFromNode(schema, method, new ArgsListNode([]), alias, typeof(object), 0);
    }

    private static AccessColumnNode Column(string name, string alias = "t", Type? type = null)
    {
        return new AccessColumnNode(name, alias, type ?? typeof(int), default);
    }

    private static FieldNode Field(Node expr, int order, string name)
    {
        return new FieldNode(expr, order, name);
    }

    private static SelectNode CreateSelect(params FieldNode[] fields)
    {
        return new SelectNode(fields);
    }

    private static AccessMethodNode WindowFunctionCall(string name, System.Reflection.MethodInfo method, params Node[] arguments)
    {
        return new AccessMethodNode(
            new FunctionToken(name, default),
            new ArgsListNode(arguments),
            null,
            false,
            method,
            string.Empty);
    }

    private static WindowFunctionNode RowNumberWindow(WindowSpecificationNode specification)
    {
        var windowFunction = new WindowFunctionNode(
            WindowFunctionCall("RowNumber", typeof(LibraryBase).GetMethod(nameof(LibraryBase.WindowRowNumber))!),
            specification);
        windowFunction.SetReturnType(typeof(long));
        return windowFunction;
    }

    private static WindowFunctionNode RowNumberWindow(string windowName)
    {
        var windowFunction = new WindowFunctionNode(
            WindowFunctionCall("RowNumber", typeof(LibraryBase).GetMethod(nameof(LibraryBase.WindowRowNumber))!),
            windowName);
        windowFunction.SetReturnType(typeof(long));
        return windowFunction;
    }

    private static QueryNode CreateQuery(
        SelectNode select,
        FromNode from,
        WhereNode? where = null,
        GroupByNode? groupBy = null,
        OrderByNode? orderBy = null,
        Musoq.Parser.Nodes.SkipNode? skip = null,
        Musoq.Parser.Nodes.TakeNode? take = null,
        Musoq.Parser.Nodes.WindowNode? window = null,
        QualifyNode? qualify = null)
    {
        return new QueryNode(select, from, where, groupBy, orderBy, skip, take, window, qualify, default);
    }

    private static LogicalNode Build(Node astRoot)
    {
        var builder = new LogicalPlanBuilder();
        var traverser = new LogicalPlanBuildTraverseVisitor(builder);
        astRoot.Accept(traverser);
        return traverser.Result ?? throw new AssertFailedException("Expected logical plan builder to produce a node.");
    }

    private static LogicalNode Build(Node astRoot, IReadOnlyDictionary<string, ISchemaColumn[]> inferredColumns)
    {
        var builder = new LogicalPlanBuilder(inferredColumns);
        var traverser = new LogicalPlanBuildTraverseVisitor(builder);
        astRoot.Accept(traverser);
        return traverser.Result ?? throw new AssertFailedException("Expected logical plan builder to produce a node.");
    }

    #endregion

    #region Window

    [TestMethod]
    public void WhenSelectWithWindowFunction_ShouldProduceProjectOverWindowWithRegistrationDetails()
    {
        var from = CreateSchemaFrom();
        var windowFunction = RowNumberWindow(
            new WindowSpecificationNode(
                [Field(Column("City", "t", typeof(string)), 0, "City")],
                [new FieldOrderedNode(Column("Name", "t", typeof(string)), 0, "Name", Order.Descending)]));
        var select = CreateSelect(
            Field(Column("Name", "t", typeof(string)), 0, "Name"),
            Field(windowFunction, 1, "RowNum"));
        var query = CreateQuery(select, from);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        Assert.IsInstanceOfType<ProjectNode>(result);
        var project = (ProjectNode)result;
        Assert.IsInstanceOfType<LogicalWindowNode>(project.Input);
        var window = (LogicalWindowNode)project.Input;
        Assert.HasCount(1, window.Registrations);
        Assert.AreEqual(typeof(LibraryBase).GetMethod(nameof(LibraryBase.WindowRowNumber)), window.Registrations[0].Function);
        Assert.HasCount(1, window.Registrations[0].PartitionKeys);
        Assert.HasCount(1, window.Registrations[0].OrderKeys);
        Assert.AreEqual("t", ((ColumnRef)window.Registrations[0].PartitionKeys[0]).Alias);
        Assert.AreEqual("City", ((ColumnRef)window.Registrations[0].PartitionKeys[0]).ColumnName);
        Assert.IsTrue(window.Registrations[0].OrderKeys[0].Descending);
        Assert.IsNull(window.Registrations[0].Frame);
        Assert.IsInstanceOfType<WindowFunctionRef>(project.Fields[1].Expression);
        Assert.AreEqual(0, ((WindowFunctionRef)project.Fields[1].Expression).WindowIndex);
    }

    [TestMethod]
    public void WhenNamedWindowReference_ShouldResolveSpecificationAndPreserveFrame()
    {
        var from = CreateSchemaFrom();
        var frame = new WindowFrameNode(
            WindowFrameType.Rows,
            new WindowFrameBoundNode(WindowFrameBoundType.OffsetPreceding, 1),
            new WindowFrameBoundNode(WindowFrameBoundType.CurrentRow));
        var definition = new WindowDefinitionNode(
            "city_window",
            new WindowSpecificationNode(
                [Field(Column("City", "t", typeof(string)), 0, "City")],
                [new FieldOrderedNode(Column("Name", "t", typeof(string)), 0, "Name", Order.Ascending)],
                frame));
        var windowFunction = RowNumberWindow("city_window");
        var select = CreateSelect(
            Field(Column("Name", "t", typeof(string)), 0, "Name"),
            Field(windowFunction, 1, "RowNum"));
        var query = CreateQuery(select, from, window: new Musoq.Parser.Nodes.WindowNode([definition]));
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        var project = (ProjectNode)result;
        var window = (LogicalWindowNode)project.Input;
        Assert.HasCount(1, window.Registrations);
        Assert.AreSame(frame, window.Registrations[0].Frame);
        Assert.HasCount(1, window.Registrations[0].PartitionKeys);
        Assert.HasCount(1, window.Registrations[0].OrderKeys);
        Assert.IsFalse(window.Registrations[0].OrderKeys[0].Descending);
    }

    [TestMethod]
    public void WhenSelectContainsDuplicateWindowFunctions_ShouldReuseSingleRegistration()
    {
        var from = CreateSchemaFrom();
        var specification = new WindowSpecificationNode(
            [],
            [new FieldOrderedNode(Column("Name", "t", typeof(string)), 0, "Name", Order.Ascending)]);
        var firstWindowFunction = RowNumberWindow(specification);
        var secondWindowFunction = RowNumberWindow(new WindowSpecificationNode(
            [],
            [new FieldOrderedNode(Column("Name", "t", typeof(string)), 0, "Name", Order.Ascending)]));
        var select = CreateSelect(
            Field(Column("Name", "t", typeof(string)), 0, "Name"),
            Field(firstWindowFunction, 1, "RowNum1"),
            Field(secondWindowFunction, 2, "RowNum2"));
        var query = CreateQuery(select, from);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        var project = (ProjectNode)result;
        var window = (LogicalWindowNode)project.Input;
        Assert.HasCount(1, window.Registrations);
        Assert.AreEqual(0, ((WindowFunctionRef)project.Fields[1].Expression).WindowIndex);
        Assert.AreEqual(0, ((WindowFunctionRef)project.Fields[2].Expression).WindowIndex);
    }

    #endregion
}
