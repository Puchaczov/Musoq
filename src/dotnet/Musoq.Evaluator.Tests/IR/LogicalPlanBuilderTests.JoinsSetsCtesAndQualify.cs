using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using JoinNode = Musoq.Evaluator.IR.Logical.Nodes.JoinNode;

namespace Musoq.Evaluator.Tests.IR;

public partial class LogicalPlanBuilderTests
{
    #region Join

    [TestMethod]
    public void WhenInnerJoin_ShouldProduceJoinNode()
    {
        var left = CreateSchemaFrom("a", "test", "users");
        var right = CreateSchemaFrom("b", "test", "orders");
        var onExpr = new EqualityNode(Column("Id", "a"), Column("UserId", "b"));
        var joinFrom = new JoinSourcesTableFromNode(left, right, onExpr, JoinType.Inner, typeof(object));

        var select = CreateSelect(Field(Column("Id", "a"), 0, "Id"));
        var query = CreateQuery(select, joinFrom);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        var project = (ProjectNode)result;
        Assert.IsInstanceOfType<JoinNode>(project.Input);
        var join = (JoinNode)project.Input;
        Assert.AreEqual(JoinKind.Inner, join.Kind);
        Assert.IsInstanceOfType<BinaryOp>(join.OnPredicate);
        Assert.IsInstanceOfType<SchemaScanNode>(join.Left);
        Assert.IsInstanceOfType<SchemaScanNode>(join.Right);
    }

    [TestMethod]
    public void WhenLeftOuterJoin_ShouldMapKindCorrectly()
    {
        var left = CreateSchemaFrom("a");
        var right = CreateSchemaFrom("b");
        var onExpr = new EqualityNode(Column("Id", "a"), Column("Id", "b"));
        var joinFrom = new JoinSourcesTableFromNode(left, right, onExpr, JoinType.OuterLeft, typeof(object));

        var select = CreateSelect(Field(Column("Id", "a"), 0, "Id"));
        var query = CreateQuery(select, joinFrom);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        var project = (ProjectNode)result;
        var join = (JoinNode)project.Input;
        Assert.AreEqual(JoinKind.LeftOuter, join.Kind);
    }

    [TestMethod]
    public void WhenInterpretFrom_ShouldProduceInterpretSourceNode()
    {
        var interpretCall = new InterpretCallNode(new StringNode("payload"), "Packet", typeof(PacketInterpreter));
        var from = new Musoq.Evaluator.Parser.InterpretFromNode(
            "p",
            interpretCall,
            ApplyType.Cross,
            typeof(PacketInterpreter));
        var select = CreateSelect(Field(Column("Value", "p", typeof(string)), 0, "Value"));
        var query = CreateQuery(select, from);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(
            root,
            new Dictionary<string, ISchemaColumn[]>
            {
                ["p"] = [new SchemaColumn("Value", 0, typeof(string))]
            });

        var project = (ProjectNode)result;
        var interpret = (InterpretSourceNode)project.Input;

        Assert.AreEqual("Packet", interpret.SchemaName);
        Assert.AreEqual(InterpretSourceKind.Interpret, interpret.Kind);
        Assert.AreEqual(ApplyKind.Cross, interpret.ApplyKind);
        Assert.AreEqual(typeof(PacketInterpreter), interpret.ResultType);
        Assert.AreEqual("payload", ((Literal)interpret.Arguments[0]).Value);
    }

internal sealed class PacketInterpreter
{
    public string Value { get; set; } = string.Empty;
}

    #endregion

    #region Set Operations

    [TestMethod]
    public void WhenUnionAll_ShouldProduceSetOperationNode()
    {
        var from1 = CreateSchemaFrom("a");
        var select1 = CreateSelect(Field(Column("Id", "a"), 0, "Id"));
        var query1 = new QueryNode(select1, from1, null, null, null, null, null);

        var from2 = CreateSchemaFrom("b");
        var select2 = CreateSelect(Field(Column("Id", "b"), 0, "Id"));
        var query2 = new QueryNode(select2, from2, null, null, null, null, null);

        var unionAll = new UnionAllNode("result", ["Id"], query1, query2, false, true);
        var root = new RootNode(unionAll);

        var result = Build(root);

        Assert.IsInstanceOfType<SetOperationNode>(result);
        var setOp = (SetOperationNode)result;
        Assert.AreEqual(SetOpKind.UnionAll, setOp.Kind);
        Assert.IsInstanceOfType<ProjectNode>(setOp.Left);
        Assert.IsInstanceOfType<ProjectNode>(setOp.Right);
    }

    #endregion

    #region CTE

    [TestMethod]
    public void WhenCte_ShouldProduceCteNode()
    {
        var innerFrom = CreateSchemaFrom();
        var innerSelect = CreateSelect(Field(Column("Id"), 0, "Id"));
        var innerQuery = new QueryNode(innerSelect, innerFrom, null, null, null, null, null);
        var innerCte = new CteInnerExpressionNode(innerQuery, "MyCte");

        var outerFrom = new InMemoryTableFromNode("MyCte", "c", typeof(object));
        var outerSelect = CreateSelect(Field(Column("Id", "c"), 0, "Id"));
        var outerQuery = new QueryNode(outerSelect, outerFrom, null, null, null, null, null);

        var cteExpr = new CteExpressionNode([innerCte], outerQuery);
        var root = new RootNode(cteExpr);

        var result = Build(root);

        Assert.IsInstanceOfType<CteNode>(result);
        var cte = (CteNode)result;
        Assert.HasCount(1, cte.Definitions);
        Assert.AreEqual("MyCte", cte.Definitions[0].Name);
        Assert.IsInstanceOfType<ProjectNode>(cte.Query);
    }

    #endregion

    #region InMemoryTable (CteRef)

    [TestMethod]
    public void WhenInMemoryTable_ShouldProduceCteRefNode()
    {
        var from = new InMemoryTableFromNode("MyCte", "c", typeof(object));
        var select = CreateSelect(Field(Column("Id", "c"), 0, "Id"));
        var query = CreateQuery(select, from);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        var project = (ProjectNode)result;
        Assert.IsInstanceOfType<CteRefNode>(project.Input);
        var cteRef = (CteRefNode)project.Input;
        Assert.AreEqual("MyCte", cteRef.CteName);
        Assert.AreEqual("c", cteRef.Alias);
    }

    #endregion

    #region Qualify

    [TestMethod]
    public void WhenQualify_ShouldProduceQualifyFilterNode()
    {
        var from = CreateSchemaFrom();
        var select = CreateSelect(Field(Column("Id"), 0, "Id"));
        var qualify = new QualifyNode(new EqualityNode(Column("Id"), new IntegerNode("1", "i")));
        var query = CreateQuery(select, from, qualify: qualify);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        Assert.IsInstanceOfType<ProjectNode>(result);
        var project = (ProjectNode)result;
        Assert.IsInstanceOfType<QualifyFilterNode>(project.Input);
        var qualifyFilter = (QualifyFilterNode)project.Input;
        Assert.IsInstanceOfType<BinaryOp>(qualifyFilter.Predicate);
    }

    #endregion

    #region Combined query

    [TestMethod]
    public void WhenCompleteQuery_ShouldAssembleCorrectNodeOrder()
    {
        var from = CreateSchemaFrom();
        var where = new WhereNode(new EqualityNode(Column("Id"), new IntegerNode("1", "i")));
        var groupField = Field(Column("Name", "t", typeof(string)), 0, "Name");
        var groupBy = new GroupByNode([groupField], null);
        var select = CreateSelect(Field(Column("Name", "t", typeof(string)), 0, "Name"));
        var orderBy = new OrderByNode([
            new FieldOrderedNode(Column("Name", "t", typeof(string)), 0, "Name", Order.Ascending)
        ]);
        var query = CreateQuery(select, from, where, groupBy, orderBy: orderBy);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        // Expected order (outermost to innermost):
        // Sort → Project → Aggregate → Filter → SchemaScan
        Assert.IsInstanceOfType<SortNode>(result);
        var sort = (SortNode)result;
        Assert.IsInstanceOfType<ProjectNode>(sort.Input);
        var project = (ProjectNode)sort.Input;
        Assert.IsInstanceOfType<AggregateNode>(project.Input);
        var agg = (AggregateNode)project.Input;
        Assert.IsInstanceOfType<FilterNode>(agg.Input);
        var filter = (FilterNode)agg.Input;
        Assert.IsInstanceOfType<SchemaScanNode>(filter.Input);
    }

    #endregion
}
