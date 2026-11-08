using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.SourcePlanning;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class GeneratedPredicateTransferTests : BasicEntityTestBase
{
    private sealed record Expression(int Kind, int Atom, Expression? Left = null, Expression? Right = null)
    {
        public int Depth => Kind switch
        {
            0 => Atom == 6 ? 2 : 1,
            1 => Left!.Depth + 1,
            _ => Math.Max(Left!.Depth, Right!.Depth) + 1
        };

        public string Sql => Kind switch
        {
            1 => $"NOT ({Left!.Sql})",
            2 => $"({Left!.Sql}) AND ({Right!.Sql})",
            3 => $"({Left!.Sql}) OR ({Right!.Sql})",
            _ => AtomSql[Atom]
        };

        public Node ToNode() => Kind switch
        {
            1 => new NotNode(Left!.ToNode()),
            2 => new AndNode(Left!.ToNode(), Right!.ToNode()),
            3 => new OrNode(Left!.ToNode(), Right!.ToNode()),
            _ => AtomNode(Atom)
        };

        public bool? Evaluate(SourcePlanningEntity left, SourcePlanningEntity right) => Kind switch
        {
            1 => !Left!.Evaluate(left, right),
            2 => Left!.Evaluate(left, right) & Right!.Evaluate(left, right),
            3 => Left!.Evaluate(left, right) | Right!.Evaluate(left, right),
            _ => Atom switch
            {
                0 => Member(left.Name, ["beta", null]),
                1 => Member(right.Name, ["beta", "item"]),
                2 => left.Score >= 0 && left.Score <= 2,
                3 => right.Score >= left.Score && right.Score <= left.Score + 2,
                4 => right.Name == null,
                5 => Equal(left.Name, "beta"),
                6 => Equal(right.Name, "beta") == null,
                7 => Member(left.Score, [0, right.Score]),
                8 => left.Name == null || left.Name == "beta", // literal CONTAINS
                _ => true
            }
        };
    }

    private static readonly string[] AtomSql =
    ["l.Name IN ('beta', NULL)", "r.Name IN ('beta', 'item')", "l.Score BETWEEN 0 AND 2",
     "r.Score BETWEEN l.Score AND l.Score + 2", "r.Name IS NULL", "l.Name = 'beta'",
     "(r.Name = 'beta') IS NULL", "l.Score IN (0, r.Score)", "l.Name CONTAINS ('beta', NULL)", "1 = 1"];

    private static readonly SourcePlanningEntity[] Rows =
    [new() { Id = 1, Score = -1, Name = null, JoinKey = 1 },
     new() { Id = 2, Score = 0, Name = "beta", JoinKey = 1 },
     new() { Id = 2, Score = 0, Name = "beta", JoinKey = 1 },
     new() { Id = 3, Score = 2, Name = "item", JoinKey = 1 },
     new() { Id = 4, Score = 3, Name = "other", JoinKey = 2 }];

    private static readonly Expression[] Expressions = Generate();

    [TestMethod]
    public void GeneratedCorpus_ShouldStayWithinBooleanDepthBudget()
    {
        Assert.AreEqual(256, Expressions.Length);
        Assert.IsTrue(Expressions.All(expression => expression.Depth <= 4));
    }

    public static IEnumerable<object[]> ExtractionCases => Enumerable.Range(0, 256).Select(index => new object[] { index });

    public static IEnumerable<object[]> ExecutionCases =>
        from block in Enumerable.Range(0, 16)
        from root in Enumerable.Range(0, 4)
        from cte in new[] { false, true }
        from mode in new[] { SourcePlanningMode.RejectAll, SourcePlanningMode.AcceptPredicate,
            SourcePlanningMode.AcceptFirstPredicate, SourcePlanningMode.AcceptTypedStringPredicate }
        select new object[] { block * 16 + root, cte, mode };

    [TestMethod]
    [DynamicData(nameof(ExtractionCases))]
    public void NullableGeneratedPredicate_ShouldNeverLoseAcceptedPairDuringExtraction(int index)
    {
        var original = Expressions[index];
        foreach (var alias in new[] { "l", "r" })
        {
            var source = new SchemaFromNode("#source", "items", new ArgsListNode([]), alias, typeof(object), 0);
            var visitor = new RewriteWhereExpressionToPassItToDataSourceVisitor(source);
            new WhereNode(original.ToNode()).Accept(new RewriteWhereExpressionToPassItToDataSourceTraverseVisitor(visitor));
            var extracted = visitor.WhereNode.Expression;
            foreach (var left in Rows)
                foreach (var right in Rows)
                {
                    if (original.Evaluate(left, right) != true) continue;
                    Assert.AreEqual(true, EvaluateNode(extracted, left, right),
                        $"Expression: {original.Sql}; alias: {alias}; extracted: {extracted}; left: {Describe(left)}; right: {Describe(right)}");
                }
        }
    }

    [TestMethod]
    [DynamicData(nameof(ExecutionCases))]
    public void NullableGeneratedPredicate_ShouldMatchIndependentCartesianOracle(int index, bool cte, SourcePlanningMode mode)
    {
        var expression = Expressions[index];
        var prefix = cte ? "with lefts as (select x.Id,x.Score,x.Name,x.JoinKey from #left.items() x), rights as (select x.Id,x.Score,x.Name,x.JoinKey from #right.items() x) " : "";
        var sources = cte ? "lefts l join rights r" : "#left.items() l join #right.items() r";
        var query = $"{prefix}select l.Id,r.Id from {sources} on l.JoinKey=r.JoinKey where {expression.Sql}";
        var provider = new SourcePlanningSchemaProvider(mode,
            new Dictionary<string, IReadOnlyList<SourcePlanningEntity>> { ["#left"] = Rows, ["#right"] = Rows });
        var expected = (from left in Rows
                        from right in Rows
                        where left.JoinKey == right.JoinKey && expression.Evaluate(left, right) == true
                        select $"{left.Id}:{right.Id}").Order().ToArray();
        var actual = CreateAndRunVirtualMachine(query, schemaProvider: provider).Run()
            .Select(row => $"{row.ReadValue(0)}:{row.ReadValue(1)}").Order().ToArray();
        CollectionAssert.AreEqual(expected, actual,
            $"Expression: {expression.Sql}; aliases=l,r; {mode}; cte={cte}; inputs=[{string.Join(';', Rows.Select(Describe))}]; missing=[{string.Join(',', Difference(expected, actual))}]; unexpected=[{string.Join(',', Difference(actual, expected))}]");
    }

    private static IEnumerable<string> Difference(string[] rows, string[] other)
    {
        var counts = other.GroupBy(row => row).ToDictionary(group => group.Key, group => group.Count());
        foreach (var row in rows)
        {
            if (counts.TryGetValue(row, out var count) && count > 0)
            {
                counts[row] = count - 1;
                continue;
            }
            yield return row;
        }
    }

    private static Expression[] Generate()
    {
        var random = new Random(61007);
        var result = new List<Expression>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (result.Count < 256)
        {
            var candidate = Build(random, 3, result.Count % 4);
            if (candidate.Depth > 4) continue;
            if (result.Count % 4 == 0 || seen.Add(candidate.Sql)) result.Add(candidate);
        }
        return result.ToArray();
    }

    private static Expression Build(Random random, int depth, int kind)
    {
        if (depth == 0 || kind == 0) return new(0, random.Next(AtomSql.Length));
        var left = Build(random, depth - 1, random.Next(4));
        return kind == 1 ? new(1, 0, left) : new(kind, 0, left, Build(random, depth - 1, random.Next(4)));
    }

    private static Node AtomNode(int atom) => atom switch
    {
        0 => new InNode(Name("l"), new ArgsListNode([new StringNode("beta"), new NullNode()])),
        1 => new InNode(Name("r"), new ArgsListNode([new StringNode("beta"), new StringNode("item")])),
        2 => new BetweenNode(Score("l"), Integer(0), Integer(2)),
        3 => new BetweenNode(Score("r"), Score("l"), new AddNode(Score("l"), Integer(2))),
        4 => new IsNullNode(Name("r"), false),
        5 => new EqualityNode(Name("l"), new StringNode("beta")),
        6 => new IsNullNode(new EqualityNode(Name("r"), new StringNode("beta")), false),
        7 => new InNode(Score("l"), new ArgsListNode([Integer(0), Score("r")])),
        8 => new ContainsNode(Name("l"), new ArgsListNode([new StringNode("beta"), new NullNode()])),
        _ => new EqualityNode(Integer(1), Integer(1))
    };

    private static Node Score(string alias) => new AccessColumnNode("Score", alias, typeof(int), default);
    private static Node Name(string alias) => new AccessColumnNode("Name", alias, typeof(string), default);
    private static Node Integer(int value) => new IntegerNode(value.ToString(), "i");
    private static string Describe(SourcePlanningEntity row) => $"Id={row.Id}, Score={row.Score}, Name={row.Name ?? "NULL"}";

    private static bool? Equal(object? left, object? right) => left == null || right == null ? null : Equals(left, right);
    private static bool? Member(object? value, object?[] members)
    {
        if (members.Length == 0) return false;
        if (value == null) return null;
        if (members.Any(member => member != null && Equals(value, member))) return true;
        return members.Any(member => member == null) ? null : false;
    }

    private static object? Scalar(Node node, SourcePlanningEntity left, SourcePlanningEntity right) => node switch
    {
        AccessColumnNode column => column.Name == "Score"
            ? (column.Alias == "l" ? left.Score : right.Score)
            : column.Alias == "l" ? left.Name : right.Name,
        IntegerNode integer => Convert.ToInt32(integer.ObjValue),
        StringNode text => text.Value,
        NullNode => null,
        AddNode add => (int)Scalar(add.Left, left, right)! + (int)Scalar(add.Right, left, right)!,
        _ => EvaluateNode(node, left, right)
    };

    private static bool? EvaluateNode(Node node, SourcePlanningEntity left, SourcePlanningEntity right) => node switch
    {
        NotNode not => !EvaluateNode(not.Expression, left, right),
        AndNode and => EvaluateNode(and.Left, left, right) & EvaluateNode(and.Right, left, right),
        OrNode or => EvaluateNode(or.Left, left, right) | EvaluateNode(or.Right, left, right),
        EqualityNode equal => Equal(Scalar(equal.Left, left, right), Scalar(equal.Right, left, right)),
        InNode membership => Member(Scalar(membership.Left, left, right), ((ArgsListNode)membership.Right).Args.Select(value => Scalar(value, left, right)).ToArray()),
        ContainsNode contains => contains.ToCompareExpression.Args.Any(value => Equals(Scalar(contains.Left, left, right), Scalar(value, left, right))),
        BetweenNode between => (int)Scalar(between.Expression, left, right)! >= (int)Scalar(between.Min, left, right)! &&
                               (int)Scalar(between.Expression, left, right)! <= (int)Scalar(between.Max, left, right)!,
        IsNullNode check => (Scalar(check.Expression, left, right) == null) ^ check.IsNegated,
        _ => throw new InvalidOperationException($"Unrecognized test expression {node}")
    };
}
