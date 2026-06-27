using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.Tests.IR;

public partial class LogicalNodeTests
{
    #region Logical plan printer (Step 2.6)

    [TestMethod]
    public void Printer_WhenSchemaScan_ShouldPrintScanLine()
    {
        var scan = CreateScan();

        var result = LogicalPlanPrinter.Print(scan);

        Assert.AreEqual("SchemaScan [#test.data() as t]", result);
    }

    [TestMethod]
    public void Printer_WhenFilterOverScan_ShouldPrintWithIndentation()
    {
        var scan = CreateScan();
        var predicate = new BinaryOp(BinaryOpKind.GreaterThan,
            new ColumnRef("t", "Id", typeof(int)),
            new Literal(18, typeof(int)),
            typeof(bool));
        var filter = new FilterNode(predicate, scan);

        var result = LogicalPlanPrinter.Print(filter);

        var expected =
            "Filter [(t.Id > 18)]\r\n" +
            "  SchemaScan [#test.data() as t]";
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Printer_WhenProjectAggregateFilterScan_ShouldPrintFullTree()
    {
        var scan = CreateScan();
        var predicate = new BinaryOp(BinaryOpKind.GreaterThan,
            new ColumnRef("t", "Id", typeof(int)),
            new Literal(18, typeof(int)),
            typeof(bool));
        var filter = new FilterNode(predicate, scan);

        var setMethod = typeof(string).GetMethod("Concat", [typeof(string), typeof(string)])!;
        var getMethod = typeof(string).GetMethod("ToUpper", Type.EmptyTypes)!;
        var bindings = new[]
        {
            new AggregateBinding("Count", "Count", setMethod, [], getMethod, [], typeof(int))
        };
        var aggregate = new AggregateNode(
            [new ColumnRef("t", "Name", typeof(string))],
            ["Name"],
            [typeof(string)],
            bindings,
            filter);

        var fields = new[]
        {
            new ProjectedField("Name", new ColumnRef("t", "Name", typeof(string)), 0),
            new ProjectedField("Count", new AggregateRef("Count", typeof(int)), 1)
        };
        var project = new ProjectNode(fields, aggregate);

        var result = LogicalPlanPrinter.Print(project);

        var expected =
            "Project [t.Name as Name, AggRef(Count) as Count]\r\n" +
            "  Aggregate [keys: Name] [aggs: Count]\r\n" +
            "    Filter [(t.Id > 18)]\r\n" +
            "      SchemaScan [#test.data() as t]";
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Printer_WhenJoin_ShouldPrintBothSides()
    {
        var left = CreateScan("a", ("Id", typeof(int)));
        var right = CreateScan("b", ("Id", typeof(int)));
        var on = new BinaryOp(BinaryOpKind.Equal,
            new ColumnRef("a", "Id", typeof(int)),
            new ColumnRef("b", "Id", typeof(int)),
            typeof(bool));
        var join = new JoinNode(JoinKind.Inner, on, left, right);

        var result = LogicalPlanPrinter.Print(join);

        var expected =
            "Join [Inner] [(a.Id = b.Id)]\r\n" +
            "  SchemaScan [#test.data() as a]\r\n" +
            "  SchemaScan [#test.data() as b]";
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Printer_WhenSort_ShouldPrintOrderFields()
    {
        var scan = CreateScan();
        var keys = new[]
        {
            new OrderField(new ColumnRef("t", "Name", typeof(string)), false),
            new OrderField(new ColumnRef("t", "Id", typeof(int)), true)
        };
        var sort = new SortNode(keys, scan);

        var result = LogicalPlanPrinter.Print(sort);

        var expected =
            "Sort [t.Name, t.Id DESC]\r\n" +
            "  SchemaScan [#test.data() as t]";
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Printer_WhenSkipTake_ShouldPrintCounts()
    {
        var scan = CreateScan();
        var sort = new SortNode(
            [new OrderField(new ColumnRef("t", "Id", typeof(int)), false)],
            scan);
        var skip = new SkipNode(10, sort);
        var take = new TakeNode(5, skip);

        var result = LogicalPlanPrinter.Print(take);

        var expected =
            "Take [5]\r\n" +
            "  Skip [10]\r\n" +
            "    Sort [t.Id]\r\n" +
            "      SchemaScan [#test.data() as t]";
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Printer_WhenHaving_ShouldPrintHavingLabel()
    {
        var scan = CreateScan();
        var predicate = new BinaryOp(BinaryOpKind.GreaterThan,
            new AggregateRef("Count", typeof(int)),
            new Literal(5, typeof(int)),
            typeof(bool));
        var having = new HavingFilterNode(predicate, scan);

        var result = LogicalPlanPrinter.Print(having);

        var expected =
            "Having [(AggRef(Count) > 5)]\r\n" +
            "  SchemaScan [#test.data() as t]";
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Printer_WhenSetOperation_ShouldPrintKind()
    {
        var left = CreateScan("a");
        var right = CreateScan("b");
        var setOp = new SetOperationNode(SetOpKind.UnionAll, left, right, []);

        var result = LogicalPlanPrinter.Print(setOp);

        var expected =
            "SetOp [UnionAll]\r\n" +
            "  SchemaScan [#test.data() as a]\r\n" +
            "  SchemaScan [#test.data() as b]";
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Printer_WhenApply_ShouldPrintKind()
    {
        var left = CreateScan("a");
        var right = CreateScan("b");
        var apply = new ApplyNode(ApplyKind.Cross, left, right);

        var result = LogicalPlanPrinter.Print(apply);

        var expected =
            "Apply [Cross]\r\n" +
            "  SchemaScan [#test.data() as a]\r\n" +
            "  SchemaScan [#test.data() as b]";
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Printer_WhenCte_ShouldPrintDefinitionsAndQuery()
    {
        var defPlan = CreateScan("d");
        var queryPlan = CreateScan("q");
        var cte = new CteNode(
            [new CteDefinition("myCte", defPlan)],
            queryPlan);

        var result = LogicalPlanPrinter.Print(cte);

        var expected =
            "Cte\r\n" +
            "  Definition [myCte]\r\n" +
            "    SchemaScan [#test.data() as d]\r\n" +
            "  Query\r\n" +
            "    SchemaScan [#test.data() as q]";
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Printer_WhenInterpretSource_ShouldPrintSchemaName()
    {
        var schema = CreateSchema(("Col", typeof(string)));
        var node = new InterpretSourceNode(
            "csv",
            InterpretSourceKind.Parse,
            [],
            "c",
            typeof(object),
            ApplyKind.Cross,
            schema);
        var result = LogicalPlanPrinter.Print(node);

        Assert.AreEqual("InterpretSource [#csv() as c]", result);
    }

    [TestMethod]
    public void Printer_WhenCteRef_ShouldPrintCteName()
    {
        var schema = CreateSchema(("X", typeof(int)));
        var node = new CteRefNode("myCte", "c", schema);

        var result = LogicalPlanPrinter.Print(node);

        Assert.AreEqual("CteRef [myCte as c]", result);
    }

    [TestMethod]
    public void Printer_WhenDesc_ShouldPrintDescDetails()
    {
        var schema = CreateSchema(("ColumnName", typeof(string)));
        var desc = new DescNode("test", "data", DescType.Table, "*", [], "desc-source", schema);

        var result = LogicalPlanPrinter.Print(desc);

        Assert.AreEqual("Desc [#test.data()] [Table] [*]", result);
    }

    [TestMethod]
    public void Printer_WhenMultiStatement_ShouldPrintAllStatements()
    {
        var stmt1 = CreateScan("a");
        var stmt2 = CreateScan("b");
        var multi = new MultiStatementNode([stmt1, stmt2]);

        var result = LogicalPlanPrinter.Print(multi);

        var expected =
            "MultiStatement\r\n" +
            "  SchemaScan [#test.data() as a]\r\n" +
            "  SchemaScan [#test.data() as b]";
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Printer_WhenQualifyFilter_ShouldPrintQualifyLabel()
    {
        var scan = CreateScan();
        var predicate = new BinaryOp(BinaryOpKind.GreaterThan,
            new ColumnRef("t", "Id", typeof(int)),
            new Literal(1, typeof(int)),
            typeof(bool));
        var qualify = new QualifyFilterNode(predicate, scan);

        var result = LogicalPlanPrinter.Print(qualify);

        var expected =
            "Qualify [(t.Id > 1)]\r\n" +
            "  SchemaScan [#test.data() as t]";
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Printer_WhenWindow_ShouldPrintRegistrations()
    {
        var scan = CreateScan();
        var method = typeof(string).GetMethod("ToUpper", Type.EmptyTypes)!;
        var registrations = new[]
        {
            new WindowRegistration(method, method.Name, [], [], [], 0, typeof(decimal))
        };
        var window = new WindowNode(registrations, scan);

        var result = LogicalPlanPrinter.Print(window);

        var expected =
            "Window [ToUpper(idx:0)]\r\n" +
            "  SchemaScan [#test.data() as t]";
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Printer_WhenComplexTree_ShouldPrintDeepNesting()
    {
        var scanA = CreateScan("a", ("Id", typeof(int)), ("Name", typeof(string)));
        var scanB = CreateScan("b", ("Id", typeof(int)), ("Value", typeof(decimal)));

        var on = new BinaryOp(BinaryOpKind.Equal,
            new ColumnRef("a", "Id", typeof(int)),
            new ColumnRef("b", "Id", typeof(int)),
            typeof(bool));
        var join = new JoinNode(JoinKind.LeftOuter, on, scanA, scanB);

        var filter = new FilterNode(
            new BinaryOp(BinaryOpKind.GreaterThan,
                new ColumnRef("b", "Value", typeof(decimal)),
                new Literal(100m, typeof(decimal)),
                typeof(bool)),
            join);

        var fields = new[]
        {
            new ProjectedField("Name", new ColumnRef("a", "Name", typeof(string)), 0),
            new ProjectedField("Value", new ColumnRef("b", "Value", typeof(decimal)), 1)
        };
        var project = new ProjectNode(fields, filter);

        var sort = new SortNode(
            [new OrderField(new ColumnRef("a", "Name", typeof(string)), false)],
            project);

        var take = new TakeNode(10, sort);

        var result = LogicalPlanPrinter.Print(take);

        var expected =
            "Take [10]\r\n" +
            "  Sort [a.Name]\r\n" +
            "    Project [a.Name as Name, b.Value as Value]\r\n" +
            "      Filter [(b.Value > 100)]\r\n" +
            "        Join [LeftOuter] [(a.Id = b.Id)]\r\n" +
            "          SchemaScan [#test.data() as a]\r\n" +
            "          SchemaScan [#test.data() as b]";
        Assert.AreEqual(expected, result);
    }

    #endregion
}
