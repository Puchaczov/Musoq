using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Execution;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class MethodTargetReusePassTests
{
    [TestMethod]
    public void Optimize_WhenMethodCallRequiresTarget_ShouldBindTargetAndInsertDeclaration()
    {
        var method = typeof(LibraryBase).GetMethod(nameof(LibraryBase.GetTypeName), [typeof(object)]);
        Assert.IsNotNull(method, "Expected LibraryBase.GetTypeName(object) to exist.");
        var call = new ExecutionMethodCall(
            method,
            [new ExecutionLiteral("value", typeof(object))],
            null,
            typeof(string),
            null);
        var plan = CreatePlan(new ExecutionLet(Var("value", typeof(string)), call));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var createObject = (ExecutionCreateObject)result.Plan.Body.Nodes[0];
        var let = (ExecutionLet)result.Plan.Body.Nodes[1];
        var rewrittenCall = (ExecutionMethodCall)let.Value;

        Assert.AreEqual(typeof(LibraryBase), createObject.Target.Type);
        Assert.AreSame(createObject.Target, rewrittenCall.Target);
        Assert.IsNull(rewrittenCall.Cache);
    }

    [TestMethod]
    public void Optimize_WhenMethodCallCanRenderWithoutTarget_ShouldLeavePlanUnchanged()
    {
        var method = ResolveContainsMethod();
        var call = new ExecutionMethodCall(
            method,
            [
                new ExecutionLiteral("abc", typeof(string)),
                new ExecutionLiteral("b", typeof(string))
            ],
            null,
            typeof(bool),
            null);
        var plan = CreatePlan(new ExecutionLet(Var("contains", typeof(bool)), call));

        var result = Optimize(plan);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
    }

    [TestMethod]
    public void Optimize_WhenCandidateIsPresent_ShouldLowerCandidateThroughProductionPass()
    {
        var method = typeof(LibraryBase).GetMethod(nameof(LibraryBase.GetTypeName), [typeof(object)]);
        Assert.IsNotNull(method, "Expected LibraryBase.GetTypeName(object) to exist.");
        var target = Var("__library", typeof(LibraryBase));
        var scope = new ExecutionMethodTargetScope(
            ExecutionMethodTargetScopeKind.AggregateHelper,
            "aggregate0");
        var call = new ExecutionMethodCall(
            method,
            [new ExecutionLiteral("value", typeof(object))],
            null,
            typeof(string),
            null,
            target);
        var plan = new ExecutionPlan(
            "compiled",
            [],
            new ExecutionBlock(
            [
                new ExecutionMethodTargetDeclarationCandidate(target, scope),
                new ExecutionLet(
                    Var("value", typeof(string)),
                    new ExecutionMethodTargetReuseCandidate(call, scope))
            ]));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var createObject = (ExecutionCreateObject)result.Plan.Body.Nodes[0];
        var let = (ExecutionLet)result.Plan.Body.Nodes[1];
        var rewrittenCall = (ExecutionMethodCall)let.Value;

        Assert.AreSame(target, createObject.Target);
        Assert.AreSame(target, rewrittenCall.Target);
        Assert.Contains("Lowered 1 method target reuse candidate call(s) and 1 declaration candidate(s)", result.Reason);
        Assert.Contains("Bound 0 method call target(s)", result.Reason);
    }

    [TestMethod]
    public void Optimize_WhenUnboundCallIsInsideNestedBlock_ShouldBindInsideNestedBlock()
    {
        var method = typeof(LibraryBase).GetMethod(nameof(LibraryBase.GetTypeName), [typeof(object)]);
        Assert.IsNotNull(method, "Expected LibraryBase.GetTypeName(object) to exist.");
        var call = new ExecutionMethodCall(
            method,
            [new ExecutionLiteral("value", typeof(object))],
            null,
            typeof(string),
            null);
        var plan = CreatePlan(new ExecutionIf(
            new ExecutionLiteral(true, typeof(bool)),
            new ExecutionBlock([new ExecutionLet(Var("value", typeof(string)), call)])));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var branch = (ExecutionIf)result.Plan.Body.Nodes[0];
        var createObject = (ExecutionCreateObject)branch.Body.Nodes[0];
        var let = (ExecutionLet)branch.Body.Nodes[1];
        var rewrittenCall = (ExecutionMethodCall)let.Value;

        Assert.AreEqual(typeof(LibraryBase), createObject.Target.Type);
        Assert.AreSame(createObject.Target, rewrittenCall.Target);
        Assert.Contains("Bound 1 method call target(s)", result.Reason);
    }

    [TestMethod]
    public void Optimize_WhenOuterDeclarationCandidateExists_ShouldBindNestedCandidateToOuterTarget()
    {
        var method = typeof(LibraryBase).GetMethod(nameof(LibraryBase.GetTypeName), [typeof(object)]);
        Assert.IsNotNull(method, "Expected LibraryBase.GetTypeName(object) to exist.");
        var target = Var("__resultLibraryBase0", typeof(LibraryBase));
        var scope = new ExecutionMethodTargetScope(
            ExecutionMethodTargetScopeKind.TablePipeline,
            "result");
        var call = new ExecutionMethodCall(
            method,
            [new ExecutionLiteral("value", typeof(object))],
            null,
            typeof(string),
            null);
        var plan = new ExecutionPlan(
            "compiled",
            [],
            new ExecutionBlock(
            [
                new ExecutionMethodTargetDeclarationCandidate(target, scope),
                new ExecutionIf(
                    new ExecutionLiteral(true, typeof(bool)),
                    new ExecutionBlock(
                    [
                        new ExecutionLet(
                            Var("value", typeof(string)),
                            new ExecutionMethodTargetReuseCandidate(call, scope))
                    ]))
            ]));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var createObject = (ExecutionCreateObject)result.Plan.Body.Nodes[0];
        var branch = (ExecutionIf)result.Plan.Body.Nodes[1];
        var let = (ExecutionLet)branch.Body.Nodes[0];
        var rewrittenCall = (ExecutionMethodCall)let.Value;

        Assert.AreSame(target, createObject.Target);
        Assert.AreSame(target, rewrittenCall.Target);
        Assert.IsFalse(branch.Body.Nodes.OfType<ExecutionCreateObject>().Any());
        Assert.Contains("Bound 1 method call target(s)", result.Reason);
        Assert.Contains("inserted 0 target declaration(s)", result.Reason);
    }

    [TestMethod]
    public void Optimize_WhenAggregateLibraryExists_ShouldBindNestedCandidateToLibraryTarget()
    {
        var method = typeof(LibraryBase).GetMethod(nameof(LibraryBase.GetTypeName), [typeof(object)]);
        Assert.IsNotNull(method, "Expected LibraryBase.GetTypeName(object) to exist.");
        var target = Var("__aggregateLibraryBase0", typeof(LibraryBase));
        var call = new ExecutionMethodCall(
            method,
            [new ExecutionLiteral("value", typeof(object))],
            null,
            typeof(string),
            null);
        var plan = new ExecutionPlan(
            "compiled",
            [],
            new ExecutionBlock(
            [
                new ExecutionCreateAggregateLibrary(target, target.Type),
                new ExecutionIf(
                    new ExecutionLiteral(true, typeof(bool)),
                    new ExecutionBlock(
                    [
                        new ExecutionLet(
                            Var("value", typeof(string)),
                            new ExecutionMethodTargetReuseCandidate(call))
                    ]))
            ]));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var branch = (ExecutionIf)result.Plan.Body.Nodes[1];
        var let = (ExecutionLet)branch.Body.Nodes[0];
        var rewrittenCall = (ExecutionMethodCall)let.Value;

        Assert.AreSame(target, rewrittenCall.Target);
        Assert.IsFalse(ExecutionIrAnalysis.CollectNodes<ExecutionCreateObject>(result.Plan.Body).Any());
    }

    [TestMethod]
    public void Optimize_WhenAggregateLoopNeedsMethodTarget_ShouldDeclareTargetBeforeAggregateSetup()
    {
        var method = typeof(LibraryBase).GetMethod(nameof(LibraryBase.GetTypeName), [typeof(object)]);
        Assert.IsNotNull(method, "Expected LibraryBase.GetTypeName(object) to exist.");
        var rowShape = new GeneratedRowShape(
            "ResultRow0",
            [new FieldBinding("Value", "Value", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Value"))]);
        var groupShape = new AggregateGroupShape("AggregateGroup0", [], [], []);
        var groupPlan = new AggregateGroupPlan(groupShape, [new AggregateGroupLevelPlan(0, groupShape)]);
        var rootGroup = Var("resultRootGroup", typeof(object));
        var groups = Var("resultGroups", typeof(object));
        var groupsToFinalize = Var("resultGroupsToFinalize", typeof(object));
        var group = Var("resultGroup", typeof(object));
        var resultTable = Var("result", typeof(object));
        var item = Var("item", typeof(object));
        var rows = Var("rows", typeof(IEnumerable<object>));
        var plan = new ExecutionPlan(
            "compiled",
            [rowShape, groupShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, rowShape),
                new ExecutionCreateSingleKeyAggregateContext(
                    rootGroup,
                    groups,
                    groupsToFinalize,
                    null,
                    typeof(int),
                    groupPlan),
                new ExecutionForEach(
                    item,
                    new ExecutionVariableRead(rows),
                    new ExecutionBlock(
                    [
                        new ExecutionGetOrAddSingleKeyAggregateGroup(
                            rootGroup,
                            groups,
                            groupsToFinalize,
                            group,
                            new ExecutionLiteral(1, typeof(int)),
                            "Value",
                            typeof(int),
                            null,
                            groupPlan),
                        new ExecutionLet(
                            Var("value", typeof(string)),
                            new ExecutionMethodCall(
                                method,
                                [new ExecutionLiteral("value", typeof(object))],
                                null,
                                typeof(string),
                                null))
                    ]))
            ]));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        Assert.IsInstanceOfType<ExecutionCreateObject>(result.Plan.Body.Nodes[0]);
        Assert.IsInstanceOfType<ExecutionCreateTable>(result.Plan.Body.Nodes[1]);
        Assert.IsInstanceOfType<ExecutionCreateSingleKeyAggregateContext>(result.Plan.Body.Nodes[2]);
        Assert.IsInstanceOfType<ExecutionForEach>(result.Plan.Body.Nodes[3]);
    }

    [TestMethod]
    public void Optimize_WhenParallelFilterProjectNeedsMethodTarget_ShouldDeclareTargetOutsideProjectorBody()
    {
        var method = typeof(LibraryBase).GetMethod(nameof(LibraryBase.GetTypeName), [typeof(object)]);
        Assert.IsNotNull(method, "Expected LibraryBase.GetTypeName(object) to exist.");
        var resultTable = Var("result", typeof(object));
        var source = new ExecutionVariable("p", typeof(object), "EntityRow");
        var rows = new ExecutionVariableRead(Var("rows", typeof(object)));
        var rowShape = new GeneratedRowShape(
            "ResultRow0",
            [new FieldBinding("Value", "Value", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Value"))]);
        var appendRow = new ExecutionAppendRow(
            resultTable,
            rowShape,
            [new ExecutionRowValue(
                "Value",
                new ExecutionMethodCall(
                    method,
                    [new ExecutionLiteral("value", typeof(object))],
                    null,
                    typeof(string),
                    null))],
            ExecutionAppendMode.Direct);
        var sequentialLoop = new ExecutionForEach(
            source,
            rows,
            new ExecutionBlock([appendRow]));
        var plan = CreatePlan(new ExecutionParallelFilterProjectLoop(
            source,
            rows,
            null,
            appendRow,
            sequentialLoop.Body,
            1000,
            2));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var createObject = (ExecutionCreateObject)result.Plan.Body.Nodes[0];
        var parallelLoop = (ExecutionParallelFilterProjectLoop)result.Plan.Body.Nodes[1];
        var projectionAppend = (ExecutionAppendRow)parallelLoop.ProjectionBody.Nodes.Single();
        var projectionCall = (ExecutionMethodCall)projectionAppend.Values.Single().Value;
        var projectorCall = (ExecutionMethodCall)parallelLoop.AppendRow.Values.Single().Value;

        Assert.AreEqual(typeof(LibraryBase), createObject.Target.Type);
        Assert.AreSame(createObject.Target, projectionCall.Target);
        Assert.AreSame(createObject.Target, projectorCall.Target);
        Assert.IsFalse(ExecutionIrAnalysis
            .CollectNodes<ExecutionCreateObject>(parallelLoop.ProjectionBody)
            .Any());
    }

    [TestMethod]
    public void Optimize_WhenWindowHelperTargetsExist_ShouldBindCandidateToHelperTarget()
    {
        var method = typeof(LibraryBase).GetMethod(nameof(LibraryBase.GetTypeName), [typeof(object)]);
        Assert.IsNotNull(method, "Expected LibraryBase.GetTypeName(object) to exist.");
        var target = Var("windowLibraryBase0", typeof(LibraryBase));
        var call = new ExecutionMethodCall(
            method,
            [new ExecutionLiteral("value", typeof(object))],
            null,
            typeof(string),
            null);
        var plan = CreatePlan(new ExecutionComputePluginWindow(
            Var("buffer", typeof(object[])),
            Var("item", typeof(object)),
            ExecutionRowAccessMode.Direct,
            null,
            [],
            new ExecutionMethodTargetReuseCandidate(call),
            [],
            [],
            null,
            method,
            "test",
            Var("results", typeof(object[])),
            MethodTargets: [target]));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var window = (ExecutionComputePluginWindow)result.Plan.Body.Nodes[0];
        var rewrittenCall = (ExecutionMethodCall)window.Value;

        Assert.AreSame(target, rewrittenCall.Target);
        Assert.AreSame(target, window.MethodTargets!.Single());
        Assert.IsFalse(ExecutionIrAnalysis.CollectNodes<ExecutionCreateObject>(result.Plan.Body).Any());
    }

    [TestMethod]
    public void Optimize_WhenDecimalMethodCallReceivesTarget_ShouldAssignCache()
    {
        var method = typeof(DecimalLibrary).GetMethod(nameof(DecimalLibrary.Normalize), [typeof(int)]);
        Assert.IsNotNull(method, "Expected DecimalLibrary.Normalize(int) to exist.");
        var call = new ExecutionMethodCall(
            method,
            [new ExecutionLiteral(7, typeof(int))],
            null,
            typeof(decimal),
            null);
        var plan = CreatePlan(new ExecutionLet(Var("value", typeof(decimal)), call));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[1];
        var rewrittenCall = (ExecutionMethodCall)let.Value;

        Assert.IsNotNull(rewrittenCall.Target);
        Assert.IsNotNull(rewrittenCall.Cache);
        Assert.AreEqual(
            typeof(ConcurrentDictionary<int, decimal>),
            rewrittenCall.Cache.Type);
    }

    [TestMethod]
    public void Optimize_WhenDecimalMethodCallIsRowLocalCseLet_ShouldBindTargetWithoutCache()
    {
        var method = typeof(DecimalLibrary).GetMethod(nameof(DecimalLibrary.Normalize), [typeof(int)]);
        Assert.IsNotNull(method, "Expected DecimalLibrary.Normalize(int) to exist.");
        var call = new ExecutionMethodCall(
            method,
            [new ExecutionLiteral(7, typeof(int))],
            null,
            typeof(decimal),
            null);
        var plan = CreatePlan(new ExecutionLet(
            Var("value", typeof(decimal)),
            call,
            ExecutionLetCacheMode.SuppressMethodCache));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[1];
        var rewrittenCall = (ExecutionMethodCall)let.Value;

        Assert.IsNotNull(rewrittenCall.Target);
        Assert.IsNull(rewrittenCall.Cache);
    }

    [TestMethod]
    public void Optimize_WhenRowLocalCseLetAlreadyHasMethodCache_ShouldRemoveCache()
    {
        var method = typeof(DecimalLibrary).GetMethod(nameof(DecimalLibrary.Normalize), [typeof(int)]);
        Assert.IsNotNull(method, "Expected DecimalLibrary.Normalize(int) to exist.");
        var target = Var("__target", typeof(DecimalLibrary));
        var cache = Var("__cache", typeof(ConcurrentDictionary<int, decimal>));
        var call = new ExecutionMethodCall(
            method,
            [new ExecutionLiteral(7, typeof(int))],
            null,
            typeof(decimal),
            null,
            target,
            cache);
        var plan = CreatePlan(new ExecutionLet(
            Var("value", typeof(decimal)),
            call,
            ExecutionLetCacheMode.SuppressMethodCache));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[0];
        var rewrittenCall = (ExecutionMethodCall)let.Value;

        Assert.AreSame(target, rewrittenCall.Target);
        Assert.IsNull(rewrittenCall.Cache);
    }

    [TestMethod]
    public void Optimize_WhenNonDecimalValueTypeMethodCallIsHoistedLet_ShouldAssignCache()
    {
        var method = typeof(IntLibrary).GetMethod(nameof(IntLibrary.Normalize), [typeof(int)]);
        Assert.IsNotNull(method, "Expected IntLibrary.Normalize(int) to exist.");
        var call = new ExecutionMethodCall(
            method,
            [new ExecutionLiteral(7, typeof(int))],
            null,
            typeof(int),
            null);
        var plan = CreatePlan(new ExecutionLet(Var("value", typeof(int)), call));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[1];
        var rewrittenCall = (ExecutionMethodCall)let.Value;

        Assert.IsNotNull(rewrittenCall.Target);
        Assert.IsNotNull(rewrittenCall.Cache);
        Assert.AreEqual(
            typeof(ConcurrentDictionary<int, int>),
            rewrittenCall.Cache.Type);
    }

    [TestMethod]
    public void Optimize_WhenNonDecimalValueTypeMethodCallIsNotHoistedLet_ShouldNotAssignCache()
    {
        var method = typeof(IntLibrary).GetMethod(nameof(IntLibrary.Normalize), [typeof(int)]);
        Assert.IsNotNull(method, "Expected IntLibrary.Normalize(int) to exist.");
        var call = new ExecutionMethodCall(
            method,
            [new ExecutionLiteral(7, typeof(int))],
            null,
            typeof(int),
            null);
        var plan = CreatePlan(new ExecutionIf(
            call,
            new ExecutionBlock([new ExecutionContinue()])));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var branch = (ExecutionIf)result.Plan.Body.Nodes[1];
        var rewrittenCall = (ExecutionMethodCall)branch.Condition;

        Assert.IsNotNull(rewrittenCall.Target);
        Assert.IsNull(rewrittenCall.Cache);
    }

    [TestMethod]
    public void Optimize_WhenTargetBoundDecimalCandidateHasNoCache_ShouldAssignCacheInPass()
    {
        var method = typeof(DecimalLibrary).GetMethod(nameof(DecimalLibrary.Normalize), [typeof(int)]);
        Assert.IsNotNull(method, "Expected DecimalLibrary.Normalize(int) to exist.");
        var target = Var("__decimalLibrary", typeof(DecimalLibrary));
        var call = new ExecutionMethodCall(
            method,
            [new ExecutionLiteral(7, typeof(int))],
            null,
            typeof(decimal),
            null,
            target);
        var plan = new ExecutionPlan(
            "compiled",
            [],
            new ExecutionBlock(
            [
                new ExecutionMethodTargetDeclarationCandidate(target),
                new ExecutionLet(
                    Var("value", typeof(int)),
                    new ExecutionMethodTargetReuseCandidate(call))
            ]));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[1];
        var rewrittenCall = (ExecutionMethodCall)let.Value;

        Assert.AreSame(target, rewrittenCall.Target);
        Assert.IsNotNull(rewrittenCall.Cache);
        Assert.AreEqual(
            typeof(ConcurrentDictionary<int, decimal>),
            rewrittenCall.Cache.Type);
        Assert.Contains("assigned 1 method cache(s)", result.Reason);
    }

    [TestMethod]
    public void Optimize_WhenDecimalMethodUsesNullableArgument_ShouldNotAssignCache()
    {
        var method = typeof(DecimalLibrary).GetMethod(nameof(DecimalLibrary.NullableArgument), [typeof(int?)]);
        Assert.IsNotNull(method, "Expected DecimalLibrary.NullableArgument(int?) to exist.");
        var call = new ExecutionMethodCall(
            method,
            [new ExecutionLiteral(7, typeof(int?))],
            null,
            typeof(decimal),
            null);
        var plan = CreatePlan(new ExecutionLet(Var("value", typeof(decimal)), call));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[1];
        var rewrittenCall = (ExecutionMethodCall)let.Value;

        Assert.IsNotNull(rewrittenCall.Target);
        Assert.IsNull(rewrittenCall.Cache);
    }

    [TestMethod]
    public void Optimize_WhenReusableCallsAppearAcrossNestedExpressionFamilies_ShouldBindEveryRequiredTarget()
    {
        var stringMethod = typeof(LibraryBase).GetMethod(nameof(LibraryBase.ToUpper), [typeof(string)]);
        Assert.IsNotNull(stringMethod, "Expected LibraryBase.ToUpper(string) to exist.");
        var containsMethod = ResolveContainsMethod();
        var result = Var("result", typeof(object));
        var rowShape = new GeneratedRowShape(
            "ResultRow0",
            [new FieldBinding("Value", "Value", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Value"))]);
        var hash = Var("hash", typeof(object));
        var matches = Var("matches", typeof(object));
        var item = Var("item", typeof(object));
        var plan = new ExecutionPlan(
            "compiled",
            [rowShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(result, rowShape),
                new ExecutionForEach(
                    item,
                    Call(stringMethod, "source"),
                    new ExecutionBlock(
                    [
                        new ExecutionIf(
                            new ExecutionCaseWhen(
                                [new ExecutionCaseWhenBranch(
                                    Call(containsMethod, "abc", "a"),
                                    new ExecutionLiteral(true, typeof(bool)))],
                                new ExecutionLiteral(false, typeof(bool)),
                                typeof(bool)),
                            new ExecutionBlock(
                            [
                                new ExecutionHashProbe(
                                    hash,
                                    matches,
                                    Call(stringMethod, "key"),
                                    typeof(string),
                                    typeof(object),
                                    new ExecutionBlock(
                                    [
                                        new ExecutionAppendRow(
                                            result,
                                            rowShape,
                                            [new ExecutionRowValue(
                                                "Value",
                                                new ExecutionCoalesce(
                                                    [
                                                        Call(stringMethod, "value"),
                                                        new ExecutionLiteral("fallback", typeof(string))
                                                    ],
                                                    typeof(string)))])
                                    ]))
                            ]))
                    ]))
            ]));

        var optimized = Optimize(plan).Plan;

        var unboundReusableCalls = ExecutionIrAnalysis
            .CollectExpressions<ExecutionMethodCall>(optimized.Body)
            .Where(RequiresReusableTarget)
            .Where(static call => call.Target == null)
            .ToArray();

        Assert.IsEmpty(unboundReusableCalls);
    }

    private static OptimizationResult<ExecutionPlan> Optimize(ExecutionPlan plan)
    {
        return new MethodTargetReusePass().Optimize(
            plan,
            new OptimizationContext(OptimizationStage.ExecutionIrOptimization));
    }

    private static ExecutionPlan CreatePlan(ExecutionNode node)
    {
        return new ExecutionPlan("compiled", [], new ExecutionBlock([node]));
    }

    private static ExecutionVariable Var(string name, Type type)
    {
        return new ExecutionVariable(name, type);
    }

    private static ExecutionMethodCall Call(MethodInfo method, params string[] values)
    {
        return new ExecutionMethodCall(
            method,
            values.Select(value => new ExecutionLiteral(value, typeof(string))).ToArray(),
            null,
            method.ReturnType,
            null);
    }

    private static bool RequiresReusableTarget(ExecutionMethodCall call)
    {
        return !ExecutionMethodTargetReuse.CanRenderWithoutTarget(call) &&
               ExecutionMethodTargetReuse.TryGetReusableTargetType(call.Method, out _);
    }

    private static MethodInfo ResolveContainsMethod()
    {
        return typeof(LibraryBase)
            .GetMethods()
            .Single(method =>
                method.Name == nameof(LibraryBase.Contains) &&
                method.GetParameters() is { Length: 2 } parameters &&
                parameters[0].ParameterType == typeof(string) &&
                parameters[1].ParameterType == typeof(string));
    }

    public sealed class DecimalLibrary : LibraryBase
    {
        public decimal Normalize(int value)
        {
            return value;
        }

        public decimal NullableArgument(int? value)
        {
            return value ?? 0;
        }
    }

    public sealed class IntLibrary : LibraryBase
    {
        public int Normalize(int value)
        {
            return value;
        }

        public int NullableArgument(int? value)
        {
            return value ?? 0;
        }
    }
}
