using System;
using System.Collections.Generic;
using System.Globalization;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests;

public sealed partial class NegatedPredicateTests
{
    private enum PredicateKind { Comparison, Not, And, Or }
    private enum ComparisonKind { Equal, Different, Less, LessOrEqual, Greater, GreaterOrEqual }
    private enum ValueKind { Left, Right, Zero, Two, LeftPlusTwo, RightPlusTwo }

    private abstract record TestPredicate
    {
        public abstract bool Evaluate(int left, int right);
        public abstract string Sql { get; }
        public abstract Node ToNode();
    }

    private sealed record ComparisonPredicate(ComparisonKind Comparison, ValueKind LeftValue, ValueKind RightValue)
        : TestPredicate
    {
        public override bool Evaluate(int left, int right) => Compare(Value(LeftValue, left, right), Value(RightValue, left, right));

        private bool Compare(int left, int right) => Comparison switch
        {
            ComparisonKind.Equal => left == right,
            ComparisonKind.Different => left != right,
            ComparisonKind.Less => left < right,
            ComparisonKind.LessOrEqual => left <= right,
            ComparisonKind.Greater => left > right,
            _ => left >= right
        };

        public override string Sql => $"{ValueSql(LeftValue)} {ComparisonSql(Comparison)} {ValueSql(RightValue)}";
        public override Node ToNode() => ComparisonNode(Comparison, ValueNode(LeftValue), ValueNode(RightValue));
    }

    private sealed record NegationPredicate(TestPredicate Operand) : TestPredicate
    {
        public override bool Evaluate(int left, int right) => !Operand.Evaluate(left, right);
        public override string Sql => $"NOT ({Operand.Sql})";
        public override Node ToNode() => new NotNode(Operand.ToNode());
    }

    private sealed record BinaryPredicate(PredicateKind Kind, TestPredicate Left, TestPredicate Right) : TestPredicate
    {
        public override bool Evaluate(int left, int right) => Kind switch
        {
            PredicateKind.And => Left.Evaluate(left, right) && Right.Evaluate(left, right),
            PredicateKind.Or => Left.Evaluate(left, right) || Right.Evaluate(left, right),
            _ => throw new InvalidOperationException($"Unexpected binary predicate {Kind}")
        };

        public override string Sql => Kind == PredicateKind.And
            ? $"({Left.Sql}) AND ({Right.Sql})"
            : $"({Left.Sql}) OR ({Right.Sql})";

        public override Node ToNode() => Kind switch
        {
            PredicateKind.And => new AndNode(Left.ToNode(), Right.ToNode()),
            PredicateKind.Or => new OrNode(Left.ToNode(), Right.ToNode()),
            _ => throw new InvalidOperationException($"Unexpected binary predicate {Kind}")
        };
    }

    private static int Value(ValueKind kind, int left, int right) => kind switch
    {
        ValueKind.Left => left,
        ValueKind.Right => right,
        ValueKind.Zero => 0,
        ValueKind.Two => 2,
        ValueKind.LeftPlusTwo => left + 2,
        _ => right + 2
    };

    private static string ValueSql(ValueKind kind) => kind switch
    {
        ValueKind.Left => "l.Score",
        ValueKind.Right => "r.Score",
        ValueKind.Zero => "0",
        ValueKind.Two => "2",
        ValueKind.LeftPlusTwo => "l.Score + 2",
        _ => "r.Score + 2"
    };

    private static string ComparisonSql(ComparisonKind kind) => kind switch
    {
        ComparisonKind.Equal => "=",
        ComparisonKind.Different => "<>",
        ComparisonKind.Less => "<",
        ComparisonKind.LessOrEqual => "<=",
        ComparisonKind.Greater => ">",
        _ => ">="
    };

    private static Node ValueNode(ValueKind kind) => kind switch
    {
        ValueKind.Left => Column("l"),
        ValueKind.Right => Column("r"),
        ValueKind.Zero => new IntegerNode("0", "i"),
        ValueKind.Two => new IntegerNode("2", "i"),
        ValueKind.LeftPlusTwo => new AddNode(Column("l"), new IntegerNode("2", "i")),
        _ => new AddNode(Column("r"), new IntegerNode("2", "i"))
    };

    private static AccessColumnNode Column(string alias) => new("Score", alias, typeof(int), default);

    private static Node ComparisonNode(ComparisonKind kind, Node left, Node right) => kind switch
    {
        ComparisonKind.Equal => new EqualityNode(left, right),
        ComparisonKind.Different => new DiffNode(left, right),
        ComparisonKind.Less => new LessNode(left, right),
        ComparisonKind.LessOrEqual => new LessOrEqualNode(left, right),
        ComparisonKind.Greater => new GreaterNode(left, right),
        _ => new GreaterOrEqualNode(left, right)
    };

    private static readonly TestPredicate[] GeneratedPredicates = GeneratePredicates();

    private static TestPredicate[] GeneratePredicates()
    {
        var random = new Random(61006);
        var result = new List<TestPredicate>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (result.Count < 256)
        {
            var rootKind = (PredicateKind)(result.Count % 4);
            var candidate = GeneratePredicate(random, 3, rootKind);
            if (seen.Add(candidate.Sql))
                result.Add(candidate);
        }
        return result.ToArray();
    }

    private static TestPredicate GeneratePredicate(Random random, int depth, PredicateKind kind)
    {
        if (depth == 0 || kind == PredicateKind.Comparison)
            return new ComparisonPredicate((ComparisonKind)random.Next(6),
                (ValueKind)random.Next(6), (ValueKind)random.Next(6));

        var left = GeneratePredicate(random, depth - 1, (PredicateKind)random.Next(4));
        if (kind == PredicateKind.Not)
            return new NegationPredicate(left);

        return new BinaryPredicate(kind, left,
            GeneratePredicate(random, depth - 1, (PredicateKind)random.Next(4)));
    }

    private static bool EvaluateExtracted(Node node, int left, int right) => node switch
    {
        NotNode not => !EvaluateExtracted(not.Expression, left, right),
        AndNode and => EvaluateExtracted(and.Left, left, right) && EvaluateExtracted(and.Right, left, right),
        OrNode or => EvaluateExtracted(or.Left, left, right) || EvaluateExtracted(or.Right, left, right),
        EqualityNode equal => Scalar(equal.Left, left, right) == Scalar(equal.Right, left, right),
        DiffNode different => Scalar(different.Left, left, right) != Scalar(different.Right, left, right),
        LessNode less => Scalar(less.Left, left, right) < Scalar(less.Right, left, right),
        LessOrEqualNode less => Scalar(less.Left, left, right) <= Scalar(less.Right, left, right),
        GreaterNode greater => Scalar(greater.Left, left, right) > Scalar(greater.Right, left, right),
        GreaterOrEqualNode greater => Scalar(greater.Left, left, right) >= Scalar(greater.Right, left, right),
        _ => throw new InvalidOperationException($"Unexpected extracted expression {node.GetType().Name}")
    };

    private static int Scalar(Node node, int left, int right) => node switch
    {
        AccessColumnNode { Alias: "l" } => left,
        AccessColumnNode { Alias: "r" } => right,
        IntegerNode literal => Convert.ToInt32(literal.ObjValue, CultureInfo.InvariantCulture),
        AddNode add => Scalar(add.Left, left, right) + Scalar(add.Right, left, right),
        _ => throw new InvalidOperationException($"Unexpected extracted scalar {node.GetType().Name}")
    };
}
