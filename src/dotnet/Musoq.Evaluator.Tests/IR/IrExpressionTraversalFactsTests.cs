using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class IrExpressionTraversalFactsTests
{
    [TestMethod]
    public void KnownExpressionKinds_ShouldCoverEveryConcreteIrExpression()
    {
        var samples = CreateSamples();
        var concreteTypes = typeof(IrExpression).Assembly
            .GetTypes()
            .Where(static type => type is { IsAbstract: false } &&
                                  typeof(IrExpression).IsAssignableFrom(type) &&
                                  type.Namespace?.StartsWith("Musoq.Evaluator.IR.Expressions", StringComparison.Ordinal) == true)
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        var missing = concreteTypes
            .Where(type => !samples.ContainsKey(type))
            .Select(static type => type.FullName)
            .ToArray();

        Assert.IsEmpty(missing, "Every concrete IrExpression kind should have traversal/facts coverage: " + string.Join(", ", missing));

        foreach (var sample in samples)
            Assert.IsTrue(IrExpressionTraversal.IsKnownExpressionKind(sample.Value), sample.Key.Name);
    }

    [TestMethod]
    public void Facts_WhenMethodCallIsNested_ShouldFindItThroughSharedTraversal()
    {
        var methodCall = CreateMethodCall();
        var expression = new StrictCast(
            new CaseWhen(
                [new CaseWhenBranch(new Literal(true, typeof(bool)), new Coalesce([methodCall], typeof(string)))],
                new Literal("fallback", typeof(string)),
                typeof(string)),
            "string",
            typeof(string));

        Assert.IsTrue(IrExpressionFacts.ContainsMethodCall(expression));
        Assert.IsTrue(ParallelPlanningEligibilityRules.ContainsMethodCall(expression));
    }

    [TestMethod]
    public void ParallelEligibility_WhenExpressionKindIsUnknown_ShouldSkip()
    {
        var result = ParallelPlanningEligibilityRules.CanUseAggregateGroupKeyExpression(
            new UnknownExpression(typeof(object)));

        Assert.IsFalse(result.IsEligible);
        StringAssert.Contains(result.Reason, nameof(UnknownExpression));
    }

    private static Dictionary<Type, IrExpression> CreateSamples()
    {
        var literal = new Literal(1, typeof(int));
        var column = new ColumnRef("p", "Value", typeof(int));
        var textLiteral = new Literal("alpha", typeof(string));
        var scriptParameter = new ScriptParameterRef("ids", typeof(int[]));
        var methodCall = CreateMethodCall();

        return new Dictionary<Type, IrExpression>
        {
            [typeof(AggregateRef)] = new AggregateRef("p.Count", typeof(long)),
            [typeof(ArrayAccess)] = new ArrayAccess(new Literal(new[] { 1 }, typeof(int[])), literal, typeof(int), typeof(int)),
            [typeof(Between)] = new Between(column, new Literal(0, typeof(int)), new Literal(10, typeof(int)), typeof(bool)),
            [typeof(BinaryOp)] = new BinaryOp(BinaryOpKind.Equal, column, literal, typeof(bool)),
            [typeof(CaseWhen)] = new CaseWhen([new CaseWhenBranch(new Literal(true, typeof(bool)), literal)], new Literal(0, typeof(int)), typeof(int)),
            [typeof(Coalesce)] = new Coalesce([textLiteral], typeof(string)),
            [typeof(CollectionInCheck)] = new CollectionInCheck(column, scriptParameter, typeof(int), typeof(bool)),
            [typeof(ColumnRef)] = column,
            [typeof(CteTableRef)] = new CteTableRef("cte"),
            [typeof(InCheck)] = new InCheck(column, [literal], typeof(bool)),
            [typeof(IsNullCheck)] = new IsNullCheck(column, IsNegated: false, typeof(bool)),
            [typeof(Literal)] = literal,
            [typeof(MethodCall)] = methodCall,
            [typeof(PatternMatch)] = new PatternMatch(textLiteral, new Literal("a%", typeof(string)), PatternKind.Like, typeof(bool)),
            [typeof(RowPresence)] = new RowPresence("p", IsPresent: true, typeof(bool)),
            [typeof(ScriptParameterRef)] = scriptParameter,
            [typeof(ScriptVariableRef)] = new ScriptVariableRef("threshold", typeof(int)),
            [typeof(StrictCast)] = new StrictCast(literal, "int", typeof(int)),
            [typeof(UnaryOp)] = new UnaryOp(UnaryOpKind.Negate, literal, typeof(int)),
            [typeof(WildcardLiteral)] = new WildcardLiteral(typeof(object)),
            [typeof(WindowFunctionRef)] = new WindowFunctionRef(0, typeof(int))
        };
    }

    private static MethodCall CreateMethodCall()
    {
        var method = typeof(IrExpressionTraversalFactsTests)
            .GetMethod(nameof(StableMethod), BindingFlags.Public | BindingFlags.Static)!;

        return new MethodCall(method, [new Literal("alpha", typeof(string))], null, typeof(string));
    }

    public static string StableMethod(string value) => value;

    private sealed record UnknownExpression(Type Type) : IrExpression(Type);
}
