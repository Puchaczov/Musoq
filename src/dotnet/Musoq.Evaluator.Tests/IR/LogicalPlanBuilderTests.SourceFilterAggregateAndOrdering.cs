using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using SkipNode = Musoq.Evaluator.IR.Logical.Nodes.SkipNode;
using TakeNode = Musoq.Evaluator.IR.Logical.Nodes.TakeNode;
using IrDescNode = Musoq.Evaluator.IR.Logical.Nodes.DescNode;
using ParserDescNode = Musoq.Parser.Nodes.DescNode;

namespace Musoq.Evaluator.Tests.IR;

public partial class LogicalPlanBuilderTests
{

    #region SchemaScan (FROM)

    [TestMethod]
    public void WhenSimpleSelect_ShouldProduceSchemaScanWithProject()
    {
        var from = CreateSchemaFrom();
        var select = CreateSelect(Field(Column("Id"), 0, "Id"));
        var query = CreateQuery(select, from);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        Assert.IsInstanceOfType<ProjectNode>(result);
        var project = (ProjectNode)result;
        Assert.HasCount(1, project.Fields);
        Assert.AreEqual("Id", project.Fields[0].OutputName);
        Assert.IsInstanceOfType<SchemaScanNode>(project.Input);
        var scan = (SchemaScanNode)project.Input;
        Assert.AreEqual("test", scan.SchemaName);
        Assert.AreEqual("data", scan.MethodName);
        Assert.AreEqual("t", scan.Alias);
    }

    [TestMethod]
    public void WhenSelectMultipleColumns_ShouldProjectAllFields()
    {
        var from = CreateSchemaFrom();
        var select = CreateSelect(
            Field(Column("Id"), 0, "Id"),
            Field(Column("Name", "t", typeof(string)), 1, "Name"));
        var query = CreateQuery(select, from);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        var project = (ProjectNode)result;
        Assert.HasCount(2, project.Fields);
        Assert.AreEqual("Id", project.Fields[0].OutputName);
        Assert.AreEqual("Name", project.Fields[1].OutputName);
    }

    [TestMethod]
    public void WhenInterpretFromHasGeneratedReturnType_ShouldDeriveOutputSchemaFromInterpreterProperties()
    {
        var parseCall = new ParseCallNode(Column("Line", "f", typeof(string)), "LogLine", typeof(TestInterpreterRow));
        var interpretFrom = new InterpretFromNode("l", parseCall, ApplyType.Cross, typeof(TestInterpreterRow));
        var select = CreateSelect(Field(Column("Timestamp", "l", typeof(string)), 0, "Timestamp"));
        var query = CreateQuery(select, interpretFrom);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        Assert.IsInstanceOfType<ProjectNode>(result);
        var project = (ProjectNode)result;
        Assert.IsInstanceOfType<InterpretSourceNode>(project.Input);

        var interpretSource = (InterpretSourceNode)project.Input;
        Assert.AreEqual("Timestamp", interpretSource.OutputSchema.Columns[0].Name);
        Assert.AreEqual("Level", interpretSource.OutputSchema.Columns[1].Name);
        Assert.AreEqual("Message", interpretSource.OutputSchema.Columns[2].Name);
    }

    #endregion

    #region Filter (WHERE)

    [TestMethod]
    public void WhenSelectWithWhere_ShouldProduceFilterBetweenScanAndProject()
    {
        var from = CreateSchemaFrom();
        var where = new WhereNode(new EqualityNode(Column("Id"), new IntegerNode("1", "i")));
        var select = CreateSelect(Field(Column("Id"), 0, "Id"));
        var query = CreateQuery(select, from, where);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        var project = (ProjectNode)result;
        Assert.IsInstanceOfType<FilterNode>(project.Input);
        var filter = (FilterNode)project.Input;
        Assert.IsInstanceOfType<BinaryOp>(filter.Predicate);
        Assert.IsInstanceOfType<SchemaScanNode>(filter.Input);
    }

    #endregion

    #region Aggregate (GROUP BY)

    [TestMethod]
    public void WhenGroupBy_ShouldProduceAggregateNode()
    {
        var from = CreateSchemaFrom();
        var groupField = Field(Column("Name", "t", typeof(string)), 0, "Name");
        var groupBy = new GroupByNode([groupField], null);
        var select = CreateSelect(Field(Column("Name", "t", typeof(string)), 0, "Name"));
        var query = CreateQuery(select, from, groupBy: groupBy);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        var project = (ProjectNode)result;
        Assert.IsInstanceOfType<AggregateNode>(project.Input);
        var agg = (AggregateNode)project.Input;
        Assert.HasCount(1, agg.GroupKeys);
        Assert.AreEqual("Name", agg.GroupKeyNames[0]);
        Assert.AreEqual(typeof(string), agg.GroupKeyTypes[0]);
    }

    [TestMethod]
    public void WhenGroupByWithHaving_ShouldProduceHavingFilterAboveAggregate()
    {
        var from = CreateSchemaFrom();
        var groupField = Field(Column("Name", "t", typeof(string)), 0, "Name");
        var having = new HavingNode(new EqualityNode(Column("Name", "t", typeof(string)), new StringNode("test")));
        var groupBy = new GroupByNode([groupField], having);
        var select = CreateSelect(Field(Column("Name", "t", typeof(string)), 0, "Name"));
        var query = CreateQuery(select, from, groupBy: groupBy);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        var project = (ProjectNode)result;
        Assert.IsInstanceOfType<HavingFilterNode>(project.Input);
        var havingFilter = (HavingFilterNode)project.Input;
        Assert.IsInstanceOfType<AggregateNode>(havingFilter.Input);
    }

    #endregion

    #region Sort (ORDER BY)

    [TestMethod]
    public void WhenOrderBy_ShouldProduceSortNodeAboveProject()
    {
        var from = CreateSchemaFrom();
        var select = CreateSelect(Field(Column("Id"), 0, "Id"));
        var orderBy = new OrderByNode([
            new FieldOrderedNode(Column("Id"), 0, "Id", Order.Ascending)
        ]);
        var query = CreateQuery(select, from, orderBy: orderBy);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        Assert.IsInstanceOfType<SortNode>(result);
        var sort = (SortNode)result;
        Assert.HasCount(1, sort.Keys);
        Assert.IsFalse(sort.Keys[0].Descending);
        Assert.IsInstanceOfType<ProjectNode>(sort.Input);
    }

    [TestMethod]
    public void WhenOrderByDescending_ShouldSetDescendingFlag()
    {
        var from = CreateSchemaFrom();
        var select = CreateSelect(Field(Column("Id"), 0, "Id"));
        var orderBy = new OrderByNode([
            new FieldOrderedNode(Column("Id"), 0, "Id", Order.Descending)
        ]);
        var query = CreateQuery(select, from, orderBy: orderBy);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        var sort = (SortNode)result;
        Assert.IsTrue(sort.Keys[0].Descending);
    }

    #endregion

    #region Skip / Take

    [TestMethod]
    public void WhenSkip_ShouldProduceSkipNode()
    {
        var from = CreateSchemaFrom();
        var select = CreateSelect(Field(Column("Id"), 0, "Id"));
        var skip = new Musoq.Parser.Nodes.SkipNode(new IntegerNode("5", "i"));
        var query = CreateQuery(select, from, skip: skip);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        Assert.IsInstanceOfType<SkipNode>(result);
        var skipNode = (SkipNode)result;
        Assert.AreEqual(5, skipNode.Count);
        Assert.IsInstanceOfType<ProjectNode>(skipNode.Input);
    }

    [TestMethod]
    public void WhenTake_ShouldProduceTakeNode()
    {
        var from = CreateSchemaFrom();
        var select = CreateSelect(Field(Column("Id"), 0, "Id"));
        var take = new Musoq.Parser.Nodes.TakeNode(new IntegerNode("10", "i"));
        var query = CreateQuery(select, from, take: take);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        Assert.IsInstanceOfType<TakeNode>(result);
        var takeNode = (TakeNode)result;
        Assert.AreEqual(10, takeNode.Count);
        Assert.IsInstanceOfType<ProjectNode>(takeNode.Input);
    }

    [TestMethod]
    public void WhenSkipAndTake_ShouldProduceTakeAboveSkip()
    {
        var from = CreateSchemaFrom();
        var select = CreateSelect(Field(Column("Id"), 0, "Id"));
        var skip = new Musoq.Parser.Nodes.SkipNode(new IntegerNode("5", "i"));
        var take = new Musoq.Parser.Nodes.TakeNode(new IntegerNode("10", "i"));
        var query = CreateQuery(select, from, skip: skip, take: take);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        Assert.IsInstanceOfType<TakeNode>(result);
        var takeNode = (TakeNode)result;
        Assert.IsInstanceOfType<SkipNode>(takeNode.Input);
        var skipNode = (SkipNode)takeNode.Input;
        Assert.IsInstanceOfType<ProjectNode>(skipNode.Input);
    }

    #endregion

    #region Desc

    [TestMethod]
    public void WhenDescSpecificConstructorWithArguments_ShouldPreserveArgumentsAndType()
    {
        var from = new SchemaFromNode(
            "test",
            "data",
            new ArgsListNode([
                new IntegerNode("7", "i"),
                new StringNode("sample")
            ]),
            string.Empty,
            typeof(object),
            0);
        var root = new RootNode(new ParserDescNode(from, DescForType.SpecificConstructor));

        var result = Build(root);

        Assert.IsInstanceOfType<IrDescNode>(result);
        var desc = (IrDescNode)result;
        Assert.AreEqual(DescType.Table, desc.Type);
        Assert.AreEqual("test", desc.SchemaName);
        Assert.AreEqual("data", desc.MethodName);
        Assert.HasCount(2, desc.Arguments);
        Assert.AreEqual(7, ((Literal)desc.Arguments[0]).Value);
        Assert.AreEqual("sample", ((Literal)desc.Arguments[1]).Value);
    }

    [TestMethod]
    public void WhenDescFunctions_ShouldMapToFunctionsType()
    {
        var from = new SchemaFromNode("test", string.Empty, new ArgsListNode([]), string.Empty, typeof(object), 0);
        var root = new RootNode(new ParserDescNode(from, DescForType.FunctionsForSchema));

        var result = Build(root);

        Assert.IsInstanceOfType<IrDescNode>(result);
        var desc = (IrDescNode)result;
        Assert.AreEqual(DescType.Functions, desc.Type);
    }

    #endregion

}
