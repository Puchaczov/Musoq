using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Execution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExpressionCseHoistingPassTests
{
    [TestMethod]
    public void Optimize_WhenAppendRowRepeatsDeterministicExpression_ShouldInsertLet()
    {
        var expression = new ExecutionBinary(
            BinaryOpKind.Add,
            new ExecutionLiteral(1, typeof(int)),
            new ExecutionLiteral(2, typeof(int)),
            typeof(int));
        var plan = CreatePlan(CreateAppendRow(expression, expression));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[0];
        var append = (ExecutionAppendRow)result.Plan.Body.Nodes[1];
        var first = (ExecutionVariableRead)append.Values[0].Value;
        var second = (ExecutionVariableRead)append.Values[1].Value;

        Assert.AreEqual(expression, let.Value);
        Assert.AreSame(let.Variable, first.Variable);
        Assert.AreSame(let.Variable, second.Variable);
    }

    [TestMethod]
    public void Optimize_WhenStabilityAwareReuseSharesExpressionAcrossOperators_ShouldInsertOneRegionLet()
    {
        var expression = new ExecutionBinary(
            BinaryOpKind.Add,
            new ExecutionLiteral(10, typeof(int)),
            new ExecutionLiteral(20, typeof(int)),
            typeof(int));
        var plan = CreatePlan(
            CreateAppendRow(expression, new ExecutionLiteral(1, typeof(int))),
            CreateAppendRow(expression, new ExecutionLiteral(2, typeof(int))));

        var result = Optimize(plan, enableExpressionCse: false, enableStabilityAwareReuse: true);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[0];
        var first = (ExecutionAppendRow)result.Plan.Body.Nodes[1];
        var second = (ExecutionAppendRow)result.Plan.Body.Nodes[2];
        Assert.AreEqual(expression, let.Value);
        Assert.AreSame(let.Variable, ((ExecutionVariableRead)first.Values[0].Value).Variable);
        Assert.AreSame(let.Variable, ((ExecutionVariableRead)second.Values[0].Value).Variable);
    }

    [TestMethod]
    public void Optimize_WhenGeneratedRowProloguePrecedesStableReuse_ShouldKeepPrologueExpressionInPlace()
    {
        var expression = new ExecutionFieldRead("p", "Value", typeof(int));
        var generatedRow = new ExecutionCreateGeneratedRow(
            new ExecutionVariable("generated", typeof(object)),
            CreateRowShape(),
            [new ExecutionRowValue("First", expression)],
            []);
        var plan = CreatePlan(
            generatedRow,
            CreateAppendRow(expression, new ExecutionLiteral(1, typeof(int))),
            CreateAppendRow(expression, new ExecutionLiteral(2, typeof(int))));

        var result = Optimize(plan, enableExpressionCse: false, enableStabilityAwareReuse: true);

        Assert.IsTrue(result.IsChanged);
        Assert.IsInstanceOfType<ExecutionCreateGeneratedRow>(result.Plan.Body.Nodes[0]);
        var preservedPrologue = (ExecutionCreateGeneratedRow)result.Plan.Body.Nodes[0];
        Assert.AreSame(expression, preservedPrologue.Values[0].Value);
        var let = (ExecutionLet)result.Plan.Body.Nodes[1];
        Assert.AreEqual(expression, let.Value);
        Assert.IsInstanceOfType<ExecutionAppendRow>(result.Plan.Body.Nodes[2]);
        Assert.IsInstanceOfType<ExecutionAppendRow>(result.Plan.Body.Nodes[3]);
    }

    [TestMethod]
    public void Optimize_WhenAsOfIndexProloguePrecedesStableReuse_ShouldKeepIndexKeyInPlace()
    {
        var expression = new ExecutionFieldRead("candidate", "Timestamp", typeof(int));
        var index = new ExecutionVariable("asOfIndex", typeof(object));
        var candidate = new ExecutionVariable("candidate", typeof(object));
        var createIndex = new ExecutionCreateAsOfIndex(
            index,
            candidate,
            new ExecutionVariableRead(new ExecutionVariable("candidates", typeof(object))),
            [],
            expression,
            BinaryOpKind.GreaterOrEqual,
            typeof(int));
        var plan = CreatePlan(
            createIndex,
            CreateAppendRow(expression, new ExecutionLiteral(1, typeof(int))),
            CreateAppendRow(expression, new ExecutionLiteral(2, typeof(int))));

        var result = Optimize(plan, enableExpressionCse: false, enableStabilityAwareReuse: true);

        Assert.IsTrue(result.IsChanged);
        Assert.IsInstanceOfType<ExecutionCreateAsOfIndex>(result.Plan.Body.Nodes[0]);
        var preservedPrologue = (ExecutionCreateAsOfIndex)result.Plan.Body.Nodes[0];
        Assert.AreSame(expression, preservedPrologue.CandidateKey);
        var let = (ExecutionLet)result.Plan.Body.Nodes[1];
        Assert.AreEqual(expression, let.Value);
        Assert.IsInstanceOfType<ExecutionAppendRow>(result.Plan.Body.Nodes[2]);
        Assert.IsInstanceOfType<ExecutionAppendRow>(result.Plan.Body.Nodes[3]);
    }

    [TestMethod]
    public void Optimize_WhenStabilityAwareReuseSharesWindowInputs_ShouldInsertOneRegionLet()
    {
        var expression = CreateRepeatedLiteralExpression();
        var row = new ExecutionVariable("row", typeof(object));
        var window = new ExecutionComputeOffsetWindow(
            new ExecutionVariable("windowRows", typeof(object)),
            row,
            ExecutionRowAccessMode.Direct,
            expression,
            [new ExecutionWindowOrderKey(expression, false)],
            expression,
            new ExecutionLiteral(1, typeof(int)),
            new ExecutionLiteral(0, typeof(int)),
            ExecutionOffsetWindowFunction.Lag,
            new ExecutionVariable("windowResults", typeof(object)));
        var plan = CreatePlan(window, CreateAppendRow(expression, expression));

        var result = Optimize(plan, enableExpressionCse: false, enableStabilityAwareReuse: true);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[0];
        var optimizedWindow = (ExecutionComputeOffsetWindow)result.Plan.Body.Nodes[1];
        var optimizedAppend = (ExecutionAppendRow)result.Plan.Body.Nodes[2];
        Assert.AreEqual(expression, let.Value);
        Assert.AreSame(let.Variable, ((ExecutionVariableRead)optimizedWindow.PartitionKey!).Variable);
        Assert.AreSame(let.Variable, ((ExecutionVariableRead)optimizedWindow.OrderKeys[0].Expression).Variable);
        Assert.AreSame(let.Variable, ((ExecutionVariableRead)optimizedWindow.Value).Variable);
        Assert.AreSame(let.Variable, ((ExecutionVariableRead)optimizedAppend.Values[0].Value).Variable);
        Assert.AreSame(let.Variable, ((ExecutionVariableRead)optimizedAppend.Values[1].Value).Variable);
    }

    [TestMethod]
    public void Optimize_WhenStabilityAwareReuseSharesSpecializedJoinKeys_ShouldInsertOneRegionLet()
    {
        var expression = new ExecutionFieldRead("r", "Timestamp", typeof(int));
        var index = new ExecutionVariable("index", typeof(object));
        var candidate = new ExecutionVariable("candidate", typeof(object));
        var candidates = new ExecutionVariableRead(new ExecutionVariable("candidates", typeof(object)));
        var first = new ExecutionCreateRangeIndex(
            index,
            candidate,
            candidates,
            expression,
            typeof(int),
            BinaryOpKind.LessThan);
        var second = new ExecutionCreateRangeIndex(
            index,
            candidate,
            candidates,
            expression,
            typeof(int),
            BinaryOpKind.LessThan);
        var plan = CreatePlan(first, second);

        var result = Optimize(plan, enableExpressionCse: false, enableStabilityAwareReuse: true);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[0];
        var firstIndex = (ExecutionCreateRangeIndex)result.Plan.Body.Nodes[1];
        var secondIndex = (ExecutionCreateRangeIndex)result.Plan.Body.Nodes[2];
        Assert.AreEqual(expression, let.Value);
        Assert.AreSame(let.Variable, ((ExecutionVariableRead)firstIndex.CandidateKey).Variable);
        Assert.AreSame(let.Variable, ((ExecutionVariableRead)secondIndex.CandidateKey).Variable);
    }

    [TestMethod]
    public void Optimize_WhenStabilityAwareReuseSharesRecursiveCtePayload_ShouldInsertOneRegionLet()
    {
        var expression = new ExecutionFieldRead("r", "Value", typeof(int));
        var result = new ExecutionVariable("result", typeof(object));
        var frontier = new ExecutionVariable("frontier", typeof(object));
        var appendRow = CreateAppendRow(expression, expression);
        var first = new ExecutionRecursiveCteAppend("walk", result, frontier, null, [], 100, appendRow);
        var second = new ExecutionRecursiveCteAppend("walk", result, frontier, null, [], 100, appendRow);
        var plan = CreatePlan(first, second);

        var optimized = Optimize(plan, enableExpressionCse: false, enableStabilityAwareReuse: true);

        Assert.IsTrue(optimized.IsChanged);
        var let = (ExecutionLet)optimized.Plan.Body.Nodes[0];
        var firstAppend = (ExecutionRecursiveCteAppend)optimized.Plan.Body.Nodes[1];
        var secondAppend = (ExecutionRecursiveCteAppend)optimized.Plan.Body.Nodes[2];
        Assert.AreEqual(expression, let.Value);
        Assert.AreSame(let.Variable, ((ExecutionVariableRead)firstAppend.AppendRow.Values[0].Value).Variable);
        Assert.AreSame(let.Variable, ((ExecutionVariableRead)secondAppend.AppendRow.Values[1].Value).Variable);
    }

    [TestMethod]
    public void Optimize_WhenStabilityAwareReuseSharesAggregateFilter_ShouldInsertOneRegionLet()
    {
        var expression = new ExecutionFieldRead("p", "Age", typeof(int));
        var group = new ExecutionVariable("group", typeof(object));
        var aggregateMethod = typeof(LibraryBase).GetMethod(nameof(LibraryBase.Count), [typeof(int?), typeof(int)])!;
        var accumulator = new AggregateAccumulatorField(
            "Count(Age)",
            "__countAge",
            AggregateKernelDescriptor.Create(aggregateMethod));
        var first = new ExecutionAggregateSet(
            group,
            aggregateMethod,
            [expression],
            new ExecutionBinary(
                BinaryOpKind.GreaterThan,
                expression,
                new ExecutionLiteral(0, typeof(int)),
                typeof(bool)),
            accumulator,
            null);
        var second = first with { FilterPredicate = first.FilterPredicate };
        var plan = CreatePlan(first, second);

        var result = Optimize(plan, enableExpressionCse: false, enableStabilityAwareReuse: true);

        Assert.IsTrue(result.IsChanged);
        var lets = result.Plan.Body.Nodes.OfType<ExecutionLet>().ToArray();
        var aggregates = result.Plan.Body.Nodes.OfType<ExecutionAggregateSet>().ToArray();
        Assert.HasCount(2, lets);
        Assert.HasCount(2, aggregates);
        var let = lets.Single(item => item.Value.Equals(expression));
        var filterLet = lets.Single(item => item.Value is ExecutionBinary);
        var firstAggregate = aggregates[0];
        var secondAggregate = aggregates[1];
        Assert.AreEqual(expression, let.Value);
        Assert.AreSame(let.Variable, ((ExecutionVariableRead)firstAggregate.Arguments[0]).Variable);
        Assert.AreSame(let.Variable, ((ExecutionVariableRead)secondAggregate.Arguments[0]).Variable);
        Assert.AreSame(filterLet.Variable, ((ExecutionVariableRead)firstAggregate.FilterPredicate!).Variable);
        Assert.AreSame(filterLet.Variable, ((ExecutionVariableRead)secondAggregate.FilterPredicate!).Variable);
    }

    [TestMethod]
    public void Optimize_WhenStabilityAwareReuseOnlySeesConditionalArms_ShouldNotCreateEagerLocal()
    {
        var expression = new ExecutionFieldRead("p", "Age", typeof(int));
        var conditional = new ExecutionCaseWhen(
            [new ExecutionCaseWhenBranch(
                new ExecutionLiteral(true, typeof(bool)),
                expression)],
            expression,
            typeof(int));
        var plan = CreatePlan(CreateAppendRow(conditional, conditional));

        var result = Optimize(plan, enableExpressionCse: false, enableStabilityAwareReuse: true);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
    }

    [TestMethod]
    public void Optimize_WhenRepeatedMethodIsNonDeterministic_ShouldLeavePlanUnchanged()
    {
        var method = new ExecutionMethodCall(
            GetType().GetMethod(nameof(RandomValue), BindingFlags.Public | BindingFlags.Static)!,
            [],
            null,
            typeof(int));
        var plan = CreatePlan(CreateAppendRow(method, method));

        var result = Optimize(plan);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
    }

    [TestMethod]
    public void Optimize_WhenRepeatedStaticDeterministicMethodCall_ShouldInsertLet()
    {
        var method = new ExecutionMethodCall(
            GetType().GetMethod(nameof(Identity), BindingFlags.Public | BindingFlags.Static)!,
            [new ExecutionLiteral(1, typeof(int))],
            null,
            typeof(int));
        var plan = CreatePlan(CreateAppendRow(method, method));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[0];
        var append = (ExecutionAppendRow)result.Plan.Body.Nodes[1];
        var first = (ExecutionVariableRead)append.Values[0].Value;
        var second = (ExecutionVariableRead)append.Values[1].Value;

        var hoistedCall = (ExecutionMethodCall)let.Value;
        Assert.AreEqual(method.Method, hoistedCall.Method);
        CollectionAssert.AreEqual(method.Arguments.ToArray(), hoistedCall.Arguments.ToArray());
        Assert.AreSame(let.Variable, first.Variable);
        Assert.AreSame(let.Variable, second.Variable);
    }

    [TestMethod]
    public void Optimize_WhenShortCircuitConditionSharesBodyMethodLet_ShouldSplitAfterPrecedingGuard()
    {
        var value = new ExecutionVariable("value", typeof(int));
        var name = new ExecutionVariable("name", typeof(string));
        var call = new ExecutionMethodCall(
            GetType().GetMethod(nameof(Identity), BindingFlags.Public | BindingFlags.Static)!,
            [new ExecutionVariableRead(value)],
            null,
            typeof(int));
        var condition = new ExecutionBinary(
            BinaryOpKind.And,
            new ExecutionBinary(
                BinaryOpKind.GreaterThan,
                new ExecutionVariableRead(value),
                new ExecutionLiteral(100, typeof(int)),
                typeof(bool)),
            new ExecutionBinary(
                BinaryOpKind.And,
                new ExecutionBinary(
                    BinaryOpKind.GreaterThan,
                    call,
                    new ExecutionLiteral(50, typeof(int)),
                    typeof(bool)),
                new ExecutionIsNullCheck(new ExecutionVariableRead(name), true, typeof(bool)),
                typeof(bool)),
            typeof(bool));
        var plan = CreatePlan(new ExecutionIf(
            condition,
            new ExecutionBlock([CreateAppendRow(call, call)])));

        var result = Optimize(plan, enableCrossNodeExpressionCse: true);

        Assert.IsTrue(result.IsChanged);
        var outer = (ExecutionIf)result.Plan.Body.Nodes[0];
        var let = (ExecutionLet)outer.Body.Nodes[0];
        var inner = (ExecutionIf)outer.Body.Nodes[1];
        var append = (ExecutionAppendRow)inner.Body.Nodes[0];
        var first = (ExecutionVariableRead)append.Values[0].Value;
        var second = (ExecutionVariableRead)append.Values[1].Value;

        Assert.AreEqual(ExecutionLetCacheMode.SuppressMethodCache, let.CacheMode);
        Assert.AreSame(let.Variable, first.Variable);
        Assert.AreSame(let.Variable, second.Variable);
        Assert.IsFalse(ExecutionIrAnalysis.FlattenExpressions(outer.Condition).OfType<ExecutionMethodCall>().Any());
        Assert.IsFalse(ExecutionIrAnalysis.FlattenExpressions(inner.Condition).OfType<ExecutionMethodCall>().Any());
    }

    [TestMethod]
    public void Optimize_WhenRepeatedUnboundReusableInstanceMethodCall_ShouldLeavePlanUnchanged()
    {
        var method = new ExecutionMethodCall(
            typeof(InstanceLibrary).GetMethod(nameof(InstanceLibrary.Normalize), [typeof(int)])!,
            [new ExecutionLiteral(1, typeof(int))],
            null,
            typeof(int));
        var plan = CreatePlan(CreateAppendRow(method, method));

        var result = Optimize(plan);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
    }

    [TestMethod]
    public void Optimize_WhenRepeatedInjectedSourceMethodCall_ShouldLeavePlanUnchanged()
    {
        var method = new ExecutionMethodCall(
            GetType().GetMethod(nameof(Identity), BindingFlags.Public | BindingFlags.Static)!,
            [new ExecutionLiteral(1, typeof(int))],
            null,
            typeof(int),
            new ExecutionLiteral("source", typeof(string)));
        var plan = CreatePlan(CreateAppendRow(method, method));

        var result = Optimize(plan);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
    }

    [TestMethod]
    public void Optimize_WhenRepeatedMethodReturnsUnknownReferenceType_ShouldLeavePlanUnchanged()
    {
        var method = new ExecutionMethodCall(
            GetType().GetMethod(nameof(Box), BindingFlags.Public | BindingFlags.Static)!,
            [new ExecutionLiteral(1, typeof(int))],
            null,
            typeof(object));
        var plan = CreatePlan(CreateAppendRow(method, method));

        var result = Optimize(plan);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
    }

    [TestMethod]
    public void Optimize_WhenMethodExpressionAppearsOnce_ShouldLeavePlanUnchanged()
    {
        var method = new ExecutionMethodCall(
            GetType().GetMethod(nameof(Identity), BindingFlags.Public | BindingFlags.Static)!,
            [new ExecutionLiteral(1, typeof(int))],
            null,
            typeof(int));
        var expression = new ExecutionBinary(
            BinaryOpKind.Add,
            method,
            new ExecutionLiteral(1, typeof(int)),
            typeof(int));
        var plan = CreatePlan(CreateAppendRow(expression, new ExecutionLiteral(0, typeof(int))));

        var result = Optimize(plan);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
    }

    [TestMethod]
    public void Optimize_WhenExpressionCseIsDisabled_ShouldLeavePlanUnchanged()
    {
        var expression = new ExecutionBinary(
            BinaryOpKind.Add,
            new ExecutionLiteral(1, typeof(int)),
            new ExecutionLiteral(2, typeof(int)),
            typeof(int));
        var plan = CreatePlan(CreateAppendRow(expression, expression));

        var result = Optimize(plan, enableExpressionCse: false);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
        Assert.Contains("disabled by compilation options", result.Reason);
    }

    [TestMethod]
    public void Optimize_WhenArrayAccessExpressionRepeats_ShouldHoistWholeArrayAccess()
    {
        var source = new ExecutionFieldRead("a", "Array", typeof(int));
        var expression = new ExecutionArrayAccess(
            source,
            new ExecutionLiteral(0, typeof(int)),
            typeof(int),
            typeof(int));
        var plan = CreatePlan(CreateAppendRow(expression, expression));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[0];

        Assert.AreEqual(expression, let.Value);
        Assert.AreEqual(typeof(int), let.Variable.Type.ResolveClrType());
    }

    [TestMethod]
    public void Optimize_WhenAppendRowRepeatsPostfixCast_ShouldHoistWholeCast()
    {
        var cast = CreateStrictCast(new ExecutionFieldRead(null, "Population", typeof(string)), "Int32", typeof(int?));
        var plan = CreatePlan(CreateAppendRow(cast, cast));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[0];
        var append = (ExecutionAppendRow)result.Plan.Body.Nodes[1];
        var first = (ExecutionVariableRead)append.Values[0].Value;
        var second = (ExecutionVariableRead)append.Values[1].Value;

        Assert.AreEqual("populationInt32", let.Variable.Name);
        Assert.AreEqual(cast, let.Value);
        Assert.AreSame(let.Variable, first.Variable);
        Assert.AreSame(let.Variable, second.Variable);
    }

    [TestMethod]
    public void Optimize_WhenFilterConditionAndAppendValueSharePostfixCast_ShouldHoistBeforeIf()
    {
        var cast = CreateStrictCast(new ExecutionFieldRead(null, "Population", typeof(string)), "Int32", typeof(int?));
        var condition = new ExecutionBinary(
            BinaryOpKind.GreaterThan,
            cast,
            new ExecutionLiteral(1000, typeof(int)),
            typeof(bool));
        var append = CreateAppendRow(cast, new ExecutionLiteral(0, typeof(int)));
        var plan = CreatePlan(new ExecutionIf(condition, new ExecutionBlock([append])));

        var result = Optimize(plan, enableCrossNodeExpressionCse: true);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[0];
        var branch = (ExecutionIf)result.Plan.Body.Nodes[1];
        var rewrittenCondition = (ExecutionBinary)branch.Condition;
        var appendRow = (ExecutionAppendRow)branch.Body.Nodes[0];
        var conditionRead = (ExecutionVariableRead)rewrittenCondition.Left;
        var appendRead = (ExecutionVariableRead)appendRow.Values[0].Value;

        Assert.AreEqual(cast, let.Value);
        Assert.AreSame(let.Variable, conditionRead.Variable);
        Assert.AreSame(let.Variable, appendRead.Variable);
    }

    [TestMethod]
    public void Optimize_WhenChainedPostfixCastRepeats_ShouldReuseInnerCastLocal()
    {
        var inner = CreateStrictCast(new ExecutionFieldRead(null, "Population", typeof(string)), "Int32", typeof(int?));
        var outer = CreateStrictCast(inner, "String", typeof(string));
        var plan = CreatePlan(CreateAppendRow(outer, outer));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var innerLet = (ExecutionLet)result.Plan.Body.Nodes[0];
        var outerLet = (ExecutionLet)result.Plan.Body.Nodes[1];
        var append = (ExecutionAppendRow)result.Plan.Body.Nodes[2];
        var outerValue = (ExecutionStrictCast)outerLet.Value;
        var outerSource = (ExecutionVariableRead)outerValue.Expression;
        var first = (ExecutionVariableRead)append.Values[0].Value;
        var second = (ExecutionVariableRead)append.Values[1].Value;

        Assert.AreEqual("populationInt32", innerLet.Variable.Name);
        Assert.AreEqual("castString", outerLet.Variable.Name);
        Assert.AreEqual(inner, innerLet.Value);
        Assert.AreSame(innerLet.Variable, outerSource.Variable);
        Assert.AreSame(outerLet.Variable, first.Variable);
        Assert.AreSame(outerLet.Variable, second.Variable);
    }

    [TestMethod]
    public void Optimize_WhenAggregateBlockRepeatsPostfixCast_ShouldHoistAtBlockStart()
    {
        var cast = CreateStrictCast(new ExecutionFieldRead(null, "Population", typeof(string)), "Int32", typeof(int?));
        var aggregatePlan = CreateAggregatePlan();
        var rootGroup = new ExecutionVariable("rootGroup", typeof(object));
        var groups = new ExecutionVariable("groups", typeof(object));
        var groupsToFinalize = new ExecutionVariable("groupsToFinalize", typeof(object));
        var group = new ExecutionVariable("group", typeof(object));
        var capturedField = new AggregateCapturedField("population", "__population", typeof(int?));
        var plan = new ExecutionPlan(
            "compiled",
            [],
            new ExecutionBlock(
            [
                new ExecutionGetOrAddSingleKeyAggregateGroup(
                    rootGroup,
                    groups,
                    groupsToFinalize,
                    group,
                    cast,
                    "Population::Int32",
                    typeof(int?),
                    null,
                    aggregatePlan),
                new ExecutionAggregateCapturedValueSet(
                    group,
                    "population",
                    cast,
                    typeof(int?),
                    capturedField)
            ]));

        var result = Optimize(plan, enableCrossNodeExpressionCse: true);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[0];
        var getOrAdd = (ExecutionGetOrAddSingleKeyAggregateGroup)result.Plan.Body.Nodes[1];
        var capturedValue = (ExecutionAggregateCapturedValueSet)result.Plan.Body.Nodes[2];
        var getOrAddRead = (ExecutionVariableRead)getOrAdd.Key;
        var capturedRead = (ExecutionVariableRead)capturedValue.Value;

        Assert.AreEqual(cast, let.Value);
        Assert.AreSame(let.Variable, getOrAddRead.Variable);
        Assert.AreSame(let.Variable, capturedRead.Variable);
    }

    [TestMethod]
    public void Optimize_WhenPassThroughPostfixCastRepeats_ShouldLeavePlanUnchanged()
    {
        var cast = CreateStrictCast(new ExecutionFieldRead(null, "Name", typeof(string)), "String", typeof(string));
        var plan = CreatePlan(CreateAppendRow(cast, cast));

        var result = Optimize(plan);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
    }

    [TestMethod]
    public void Optimize_WhenAndExpressionRepeats_ShouldNotHoistWholeShortCircuitExpression()
    {
        var left = new ExecutionBinary(
            BinaryOpKind.GreaterThan,
            new ExecutionLiteral(2, typeof(int)),
            new ExecutionLiteral(1, typeof(int)),
            typeof(bool));
        var expression = new ExecutionBinary(
            BinaryOpKind.And,
            left,
            left,
            typeof(bool));
        var plan = CreatePlan(CreateAppendRow(expression, expression));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[0];

        Assert.AreEqual(left, let.Value);
        Assert.AreNotEqual(expression, let.Value);
    }

    [TestMethod]
    public void Optimize_WhenRepeatedExpressionIsInsideIndexedHelperBody_ShouldHoistInsideHelperBody()
    {
        var expression = new ExecutionBinary(
            BinaryOpKind.Add,
            new ExecutionLiteral(1, typeof(int)),
            new ExecutionLiteral(2, typeof(int)),
            typeof(int));
        var body = new ExecutionBlock([CreateAppendRow(expression, expression)]);
        var plan = CreatePlan(new ExecutionForEachIndexed(
            new ExecutionVariable("item", typeof(object)),
            new ExecutionVariable("index", typeof(int)),
            new ExecutionVariable("rows", typeof(object)),
            ExecutionRowAccessMode.Direct,
            body));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var loop = (ExecutionForEachIndexed)result.Plan.Body.Nodes[0];
        var let = (ExecutionLet)loop.Body.Nodes[0];
        var append = (ExecutionAppendRow)loop.Body.Nodes[1];
        var first = (ExecutionVariableRead)append.Values[0].Value;
        var second = (ExecutionVariableRead)append.Values[1].Value;

        Assert.AreEqual(expression, let.Value);
        Assert.AreSame(let.Variable, first.Variable);
        Assert.AreSame(let.Variable, second.Variable);
        Assert.Contains("including 1 helper-body node(s)", result.Reason);
    }

    [TestMethod]
    public void Optimize_WhenExpressionRepeatsAcrossHelperBoundary_ShouldNotHoistAcrossBoundary()
    {
        var expression = new ExecutionBinary(
            BinaryOpKind.Add,
            new ExecutionLiteral(1, typeof(int)),
            new ExecutionLiteral(2, typeof(int)),
            typeof(int));
        var body = new ExecutionBlock([CreateAppendRow(expression, new ExecutionLiteral(3, typeof(int)))]);
        var plan = new ExecutionPlan(
            "compiled",
            [],
            new ExecutionBlock(
            [
                CreateAppendRow(expression, new ExecutionLiteral(4, typeof(int))),
                new ExecutionForEachIndexed(
                    new ExecutionVariable("item", typeof(object)),
                    new ExecutionVariable("index", typeof(int)),
                    new ExecutionVariable("rows", typeof(object)),
                    ExecutionRowAccessMode.Direct,
                    body)
            ]));

        var result = Optimize(plan);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
    }

    [TestMethod]
    public void Optimize_WhenFilterConditionAndAppendValueShareExpression_ShouldHoistBeforeIf()
    {
        var fieldRead = new ExecutionFieldRead("p", "Age", typeof(int));
        var condition = new ExecutionBinary(
            BinaryOpKind.GreaterThan,
            fieldRead,
            new ExecutionLiteral(18, typeof(int)),
            typeof(bool));
        var append = CreateAppendRow(fieldRead, new ExecutionLiteral(0, typeof(int)));
        var plan = CreatePlan(new ExecutionIf(condition, new ExecutionBlock([append])));

        var result = Optimize(plan, enableCrossNodeExpressionCse: true);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[0];
        var branch = (ExecutionIf)result.Plan.Body.Nodes[1];
        var rewrittenCondition = (ExecutionBinary)branch.Condition;
        var appendRow = (ExecutionAppendRow)branch.Body.Nodes[0];
        var conditionRead = (ExecutionVariableRead)rewrittenCondition.Left;
        var appendRead = (ExecutionVariableRead)appendRow.Values[0].Value;

        Assert.AreEqual(fieldRead, let.Value);
        Assert.AreSame(let.Variable, conditionRead.Variable);
        Assert.AreSame(let.Variable, appendRead.Variable);
    }

    [TestMethod]
    public void Optimize_WhenAggregateBlockRepeatsExpression_ShouldHoistAtBlockStart()
    {
        var fieldRead = new ExecutionFieldRead("p", "Name", typeof(string));
        var aggregatePlan = CreateAggregatePlan();
        var rootGroup = new ExecutionVariable("rootGroup", typeof(object));
        var groups = new ExecutionVariable("groups", typeof(object));
        var groupsToFinalize = new ExecutionVariable("groupsToFinalize", typeof(object));
        var group = new ExecutionVariable("group", typeof(object));
        var capturedField = new AggregateCapturedField("name", "__name", typeof(string));
        var plan = new ExecutionPlan(
            "compiled",
            [],
            new ExecutionBlock(
            [
                new ExecutionGetOrAddSingleKeyAggregateGroup(
                    rootGroup,
                    groups,
                    groupsToFinalize,
                    group,
                    fieldRead,
                    "p.Name",
                    typeof(string),
                    null,
                    aggregatePlan),
                new ExecutionAggregateCapturedValueSet(
                    group,
                    "name",
                    fieldRead,
                    typeof(string),
                    capturedField)
            ]));

        var result = Optimize(plan, enableCrossNodeExpressionCse: true);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[0];
        var getOrAdd = (ExecutionGetOrAddSingleKeyAggregateGroup)result.Plan.Body.Nodes[1];
        var capturedValue = (ExecutionAggregateCapturedValueSet)result.Plan.Body.Nodes[2];
        var getOrAddRead = (ExecutionVariableRead)getOrAdd.Key;
        var capturedRead = (ExecutionVariableRead)capturedValue.Value;

        Assert.AreEqual(fieldRead, let.Value);
        Assert.AreSame(let.Variable, getOrAddRead.Variable);
        Assert.AreSame(let.Variable, capturedRead.Variable);
    }

    [TestMethod]
    public void Optimize_WhenAggregateBlockHasExpandoAdapter_ShouldHoistAfterAdapter()
    {
        var adapter = new ExecutionVariable("ko3iko", typeof(object));
        var resolver = new ExecutionVariable("ko3ikoResolver", typeof(object));
        var fieldRead = new ExecutionFieldRead("ko3iko", "Category", typeof(string));
        var aggregatePlan = CreateAggregatePlan();
        var rootGroup = new ExecutionVariable("rootGroup", typeof(object));
        var groups = new ExecutionVariable("groups", typeof(object));
        var groupsToFinalize = new ExecutionVariable("groupsToFinalize", typeof(object));
        var group = new ExecutionVariable("group", typeof(object));
        var capturedField = new AggregateCapturedField("category", "__category", typeof(string));
        var expandoShape = new ExpandoAdapterShape(
            "ko3iko",
            "ko3ikoDynamicRow0",
            typeof(object),
            [new FieldBinding("Category", "Category", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Category"))]);
        var plan = new ExecutionPlan(
            "compiled",
            [],
            new ExecutionBlock(
            [
                new ExecutionAdaptExpando(adapter, resolver, expandoShape),
                new ExecutionGetOrAddSingleKeyAggregateGroup(
                    rootGroup,
                    groups,
                    groupsToFinalize,
                    group,
                    fieldRead,
                    "Category",
                    typeof(string),
                    null,
                    aggregatePlan),
                new ExecutionAggregateCapturedValueSet(
                    group,
                    "category",
                    fieldRead,
                    typeof(string),
                    capturedField)
            ]));

        var result = Optimize(plan, enableCrossNodeExpressionCse: true);

        Assert.IsTrue(result.IsChanged);
        Assert.IsInstanceOfType<ExecutionAdaptExpando>(result.Plan.Body.Nodes[0]);
        var let = (ExecutionLet)result.Plan.Body.Nodes[1];
        var getOrAdd = (ExecutionGetOrAddSingleKeyAggregateGroup)result.Plan.Body.Nodes[2];
        var capturedValue = (ExecutionAggregateCapturedValueSet)result.Plan.Body.Nodes[3];
        var getOrAddRead = (ExecutionVariableRead)getOrAdd.Key;
        var capturedRead = (ExecutionVariableRead)capturedValue.Value;

        Assert.AreEqual(fieldRead, let.Value);
        Assert.AreSame(let.Variable, getOrAddRead.Variable);
        Assert.AreSame(let.Variable, capturedRead.Variable);
    }

    [TestMethod]
    public void Optimize_WhenNoGroupAggregateRepeatsExpression_ShouldHoistBeforeEnsureGroup()
    {
        var fieldRead = new ExecutionFieldRead("p", "Population", typeof(decimal));
        var aggregatePlan = CreateAggregatePlan();
        var rootGroup = new ExecutionVariable("rootGroup", typeof(object));
        var currentGroup = new ExecutionVariable("group", typeof(object));
        var groups = new ExecutionVariable("groups", typeof(object));
        var capturedField = new AggregateCapturedField("population", "__population", typeof(decimal));
        var plan = new ExecutionPlan(
            "compiled",
            [],
            new ExecutionBlock(
            [
                new ExecutionEnsureAggregateGroup(
                    rootGroup,
                    currentGroup,
                    groups,
                    aggregatePlan),
                new ExecutionAggregateCapturedValueSet(
                    currentGroup,
                    "population",
                    fieldRead,
                    typeof(decimal),
                    capturedField),
                new ExecutionAggregateCapturedValueSet(
                    currentGroup,
                    "population",
                    fieldRead,
                    typeof(decimal),
                    capturedField)
            ]));

        var result = Optimize(plan, enableCrossNodeExpressionCse: true);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[0];
        var ensure = (ExecutionEnsureAggregateGroup)result.Plan.Body.Nodes[1];
        var firstCaptured = (ExecutionAggregateCapturedValueSet)result.Plan.Body.Nodes[2];
        var secondCaptured = (ExecutionAggregateCapturedValueSet)result.Plan.Body.Nodes[3];
        var firstRead = (ExecutionVariableRead)firstCaptured.Value;
        var secondRead = (ExecutionVariableRead)secondCaptured.Value;

        Assert.AreEqual(currentGroup, ensure.CurrentGroup);
        Assert.AreEqual(fieldRead, let.Value);
        Assert.AreSame(let.Variable, firstRead.Variable);
        Assert.AreSame(let.Variable, secondRead.Variable);
    }

    [TestMethod]
    public void Optimize_WhenParallelAggregateRepeatsExpression_ShouldHoistInAggregateBody()
    {
        var fieldRead = new ExecutionFieldRead("p", "Name", typeof(string));
        var aggregatePlan = CreateAggregatePlan();
        var rootGroup = new ExecutionVariable("rootGroup", typeof(object));
        var groups = new ExecutionVariable("groups", typeof(object));
        var groupsToFinalize = new ExecutionVariable("groupsToFinalize", typeof(object));
        var group = new ExecutionVariable("group", typeof(object));
        var source = new ExecutionVariable("p", typeof(object));
        var sourceRows = new ExecutionVariable("pRows", typeof(object));
        var capturedField = new AggregateCapturedField("name", "__name", typeof(string));
        var sequentialLoop = new ExecutionForEach(
            source,
            new ExecutionVariableRead(sourceRows),
            new ExecutionBlock(
            [
                new ExecutionGetOrAddSingleKeyAggregateGroup(
                    rootGroup,
                    groups,
                    groupsToFinalize,
                    group,
                    fieldRead,
                    "p.Name",
                    typeof(string),
                    null,
                    aggregatePlan),
                new ExecutionAggregateCapturedValueSet(
                    group,
                    "name",
                    fieldRead,
                    typeof(string),
                    capturedField)
            ]));
        var parallelBody = new ExecutionBlock(
        [
            new ExecutionAggregateCapturedValueSet(
                group,
                "name",
                fieldRead,
                typeof(string),
                capturedField),
            new ExecutionAggregateCapturedValueSet(
                group,
                "name",
                fieldRead,
                typeof(string),
                capturedField)
        ]);
        var loop = new ExecutionParallelSingleKeyAggregateLoop(
            source,
            new ExecutionVariableRead(sourceRows),
            fieldRead,
            "p.Name",
            typeof(string),
            rootGroup,
            groupsToFinalize,
            group,
            parallelBody,
            aggregatePlan.LeafShape,
            4096,
            512,
            128,
            24);
        var plan = CreatePlan(loop);

        var result = Optimize(plan, enableCrossNodeExpressionCse: true);

        Assert.IsTrue(result.IsChanged);
        var rewrittenLoop = (ExecutionParallelSingleKeyAggregateLoop)result.Plan.Body.Nodes[0];
        var aggregateLet = (ExecutionLet)rewrittenLoop.AggregateBody.Nodes[0];
        var firstAggregateSet = (ExecutionAggregateCapturedValueSet)rewrittenLoop.AggregateBody.Nodes[1];
        var secondAggregateSet = (ExecutionAggregateCapturedValueSet)rewrittenLoop.AggregateBody.Nodes[2];
        var firstAggregateRead = (ExecutionVariableRead)firstAggregateSet.Value;
        var secondAggregateRead = (ExecutionVariableRead)secondAggregateSet.Value;

        Assert.AreEqual(fieldRead, aggregateLet.Value);
        Assert.AreSame(aggregateLet.Variable, firstAggregateRead.Variable);
        Assert.AreSame(aggregateLet.Variable, secondAggregateRead.Variable);
    }

    [TestMethod]
    public void Optimize_WhenHashAndProbeKeysRepeatExpression_ShouldInsertLetsBeforeIndexNodes()
    {
        var expression = CreateRepeatedFieldExpression("p", "Age");
        var row = new ExecutionVariable("row", typeof(object));
        var hash = new ExecutionVariable("hash", typeof(object));
        var matches = new ExecutionVariable("matches", typeof(object));
        var keySet = new ExecutionVariable("keySet", typeof(object));
        var plan = new ExecutionPlan(
            "compiled",
            [],
            new ExecutionBlock(
            [
                new ExecutionHashAdd(hash, expression, row, typeof(int), typeof(object)),
                new ExecutionHashProbe(hash, matches, expression, typeof(int), typeof(object), ExecutionBlock.Empty),
                new ExecutionKeySetAdd(keySet, expression, typeof(int)),
                new ExecutionKeySetProbe(keySet, expression, typeof(int), ExecutionBlock.Empty)
            ]));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        AssertHashKeyHoist<ExecutionHashAdd>(result.Plan.Body.Nodes[0], result.Plan.Body.Nodes[1]);
        AssertHashKeyHoist<ExecutionHashProbe>(result.Plan.Body.Nodes[2], result.Plan.Body.Nodes[3]);
        AssertHashKeyHoist<ExecutionKeySetAdd>(result.Plan.Body.Nodes[4], result.Plan.Body.Nodes[5]);
        AssertHashKeyHoist<ExecutionKeySetProbe>(result.Plan.Body.Nodes[6], result.Plan.Body.Nodes[7]);
    }

    [TestMethod]
    public void Optimize_WhenIfConditionRepeatsExpression_ShouldHoistPredicateExpressionBeforeBranch()
    {
        var expression = CreateRepeatedFieldExpression("p", "Age");
        var plan = CreatePlan(new ExecutionIf(expression, ExecutionBlock.Empty));

        var result = Optimize(plan, enableCrossNodeExpressionCse: true);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[0];
        var branch = (ExecutionIf)result.Plan.Body.Nodes[1];
        var condition = (ExecutionBinary)branch.Condition;
        var left = (ExecutionVariableRead)condition.Left;
        var right = (ExecutionVariableRead)condition.Right;

        Assert.IsInstanceOfType<ExecutionFieldRead>(let.Value);
        Assert.AreSame(let.Variable, left.Variable);
        Assert.AreSame(let.Variable, right.Variable);
    }

    [TestMethod]
    public void Optimize_WhenWindowHelperExpressionsAreIndependent_ShouldHoistBeforeWindowNode()
    {
        var expression = CreateRepeatedLiteralExpression();
        var row = new ExecutionVariable("row", typeof(object));
        var window = new ExecutionComputeOffsetWindow(
            new ExecutionVariable("windowRows", typeof(object)),
            row,
            ExecutionRowAccessMode.Direct,
            expression,
            [new ExecutionWindowOrderKey(expression, false)],
            expression,
            new ExecutionLiteral(1, typeof(int)),
            new ExecutionLiteral(0, typeof(int)),
            ExecutionOffsetWindowFunction.Lag,
            new ExecutionVariable("windowResults", typeof(object)));
        var plan = CreatePlan(window);

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[0];
        var optimizedWindow = (ExecutionComputeOffsetWindow)result.Plan.Body.Nodes[1];
        var partitionKey = (ExecutionVariableRead)optimizedWindow.PartitionKey!;
        var orderKey = (ExecutionVariableRead)optimizedWindow.OrderKeys[0].Expression;
        var value = (ExecutionVariableRead)optimizedWindow.Value;

        Assert.AreEqual(expression, let.Value);
        Assert.AreSame(let.Variable, partitionKey.Variable);
        Assert.AreSame(let.Variable, orderKey.Variable);
        Assert.AreSame(let.Variable, value.Variable);
    }

    [TestMethod]
    public void Optimize_WhenUnsupportedScopesContainRepeatedExpressions_ShouldReportSkippedCseFamilies()
    {
        var expression = CreateRepeatedFieldExpression("p", "Age");
        var row = new ExecutionVariable("row", typeof(object));
        var windowBuffer = new ExecutionVariable("windowRows", typeof(object));
        var windowResults = new ExecutionVariable("windowResults", typeof(object));
        var plan = new ExecutionPlan(
            "compiled",
            [],
            new ExecutionBlock(
            [
                new ExecutionComputeOffsetWindow(
                    windowBuffer,
                    row,
                    ExecutionRowAccessMode.Direct,
                    expression,
                    [new ExecutionWindowOrderKey(expression, false)],
                    expression,
                    new ExecutionLiteral(1, typeof(int)),
                    new ExecutionLiteral(0, typeof(int)),
                    ExecutionOffsetWindowFunction.Lag,
                    windowResults),
                new ExecutionForEachIndexed(
                    row,
                    new ExecutionVariable("index", typeof(int)),
                    new ExecutionVariable("rows", typeof(object)),
                    ExecutionRowAccessMode.Direct,
                    new ExecutionBlock(
                    [
                        new ExecutionLet(new ExecutionVariable("first", typeof(int)), expression),
                        new ExecutionLet(new ExecutionVariable("second", typeof(int)), expression)
                    ]))
            ]));

        var result = Optimize(plan, enableCrossNodeExpressionCse: true);

        Assert.IsFalse(result.IsChanged);
        Assert.Contains("Skipped CSE opportunities remain in unsupported scopes", result.Reason);
        Assert.Contains("window helper bodies=2", result.Reason);
        Assert.Contains("generated helper bodies=2", result.Reason);
    }

    [NonDeterministic]
    public static int RandomValue()
    {
        return 4;
    }

    public static int Identity(int value)
    {
        return value;
    }

    public static object Box(int value)
    {
        return value;
    }

    public sealed class InstanceLibrary : Plugins.LibraryBase
    {
        public int Normalize(int value)
        {
            return value;
        }
    }

    private static OptimizationResult<ExecutionPlan> Optimize(
        ExecutionPlan plan,
        bool? enableExpressionCse = null,
        bool enableCrossNodeExpressionCse = false,
        bool enableStabilityAwareReuse = false)
    {
        var options = new OptimizationOptions
        {
            ExpressionCseEnabled = enableExpressionCse ?? true,
            CrossNodeExpressionCseEnabled = enableCrossNodeExpressionCse,
            StabilityAwareScalarReuseEnabled = enableStabilityAwareReuse
        };

        return new ExpressionCseHoistingPass().Optimize(
            plan,
            new OptimizationContext(
                OptimizationStage.ExecutionIrOptimization,
                trace: null,
                options,
                OptimizationContextState.Empty));
    }

    private static ExecutionPlan CreatePlan(params ExecutionNode[] nodes)
    {
        return new ExecutionPlan("compiled", [], new ExecutionBlock(nodes));
    }

    private static ExecutionAppendRow CreateAppendRow(
        ExecutionExpression first,
        ExecutionExpression second)
    {
        return new ExecutionAppendRow(
            new ExecutionVariable("result", typeof(object)),
            CreateRowShape(),
            [
                new ExecutionRowValue("First", first),
                new ExecutionRowValue("Second", second)
            ]);
    }

    private static GeneratedRowShape CreateRowShape()
    {
        return new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("First", "First", 0, typeof(object), FieldNullability.Unknown, new GeneratedFieldAccess("First")),
                new FieldBinding("Second", "Second", 1, typeof(object), FieldNullability.Unknown, new GeneratedFieldAccess("Second"))
            ]);
    }

    private static ExecutionExpression CreateRepeatedFieldExpression(string alias, string fieldName)
    {
        var field = new ExecutionFieldRead(alias, fieldName, typeof(int));
        return new ExecutionBinary(BinaryOpKind.Add, field, field, typeof(int));
    }

    private static ExecutionExpression CreateRepeatedLiteralExpression()
    {
        return new ExecutionBinary(
            BinaryOpKind.Add,
            new ExecutionLiteral(1, typeof(int)),
            new ExecutionLiteral(2, typeof(int)),
            typeof(int));
    }

    private static ExecutionStrictCast CreateStrictCast(
        ExecutionExpression expression,
        string targetTypeName,
        Type returnType)
    {
        return new ExecutionStrictCast(expression, targetTypeName, returnType);
    }

    private static void AssertHashKeyHoist<TNode>(ExecutionNode letNode, ExecutionNode indexedNode)
        where TNode : ExecutionNode
    {
        var let = (ExecutionLet)letNode;
        var key = indexedNode switch
        {
            ExecutionHashAdd hashAdd => hashAdd.Key,
            ExecutionHashProbe hashProbe => hashProbe.Key,
            ExecutionKeySetAdd keySetAdd => keySetAdd.Key,
            ExecutionKeySetProbe keySetProbe => keySetProbe.Key,
            _ => throw new AssertFailedException($"Unexpected node type {indexedNode.GetType().Name}.")
        };
        var expression = (ExecutionBinary)key;
        var left = (ExecutionVariableRead)expression.Left;
        var right = (ExecutionVariableRead)expression.Right;

        Assert.IsInstanceOfType<TNode>(indexedNode);
        Assert.IsInstanceOfType<ExecutionFieldRead>(let.Value);
        Assert.AreSame(let.Variable, left.Variable);
        Assert.AreSame(let.Variable, right.Variable);
    }

    private static AggregateGroupPlan CreateAggregatePlan()
    {
        var shape = new AggregateGroupShape("Group0", [], [], []);
        return new AggregateGroupPlan(shape, [new AggregateGroupLevelPlan(0, shape)]);
    }
}
