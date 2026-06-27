using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.Diagnostics;

[TestClass]
public sealed class OperatorProfileRecorderTests
{
    [TestMethod]
    public void QueryProfileRecorder_WhenOperatorsAreRegistered_OrdersSnapshotsByCatalog()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);
        recorder.RegisterOperators(
        [
            new ExecutionPlanOperatorDescriptor("op2", "SortTable [x]", "SortTable", ExecutionPlanOperatorRowCountStrategy.TableTransform),
            new ExecutionPlanOperatorDescriptor("op1", "SourceScan [x]", "SourceScan", ExecutionPlanOperatorRowCountStrategy.SourceBoundary)
        ]);

        using (recorder.BeginOperator("op1", "SourceScan"))
        {
            clock.Advance(TimeSpan.FromMilliseconds(5));
            recorder.AddOperatorInputRows("op1", 2);
            recorder.AddOperatorOutputRows("op1", 1);
        }

        var operators = recorder.CreateSnapshot().Operators;

        CollectionAssert.AreEqual(new[] { "op2", "op1" }, operators.Select(static operation => operation.Id).ToArray());
        Assert.IsFalse(operators[0].HasActualStats);
        Assert.IsTrue(operators[1].HasActualStats);
        Assert.AreEqual(2, operators[1].InputRows);
        Assert.AreEqual(1, operators[1].OutputRows);
        Assert.AreEqual(TimeSpan.FromMilliseconds(5), operators[1].ElapsedTime);
    }

    [TestMethod]
    public void QueryProfileRecorder_WhenScopesAreNested_RecordsEachElapsedIndependently()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);

        using (recorder.BeginOperator("op1", "Outer"))
        {
            clock.Advance(TimeSpan.FromMilliseconds(2));
            using (recorder.BeginOperator("op2", "Inner"))
            {
                clock.Advance(TimeSpan.FromMilliseconds(3));
            }

            clock.Advance(TimeSpan.FromMilliseconds(5));
        }

        var operators = recorder.CreateSnapshot().Operators.ToDictionary(static operation => operation.Id);

        Assert.AreEqual(TimeSpan.FromMilliseconds(10), operators["op1"].ElapsedTime);
        Assert.AreEqual(TimeSpan.FromMilliseconds(3), operators["op2"].ElapsedTime);
    }

    [TestMethod]
    public void QueryProfileRecorder_WhenOperatorFails_RecordsException()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);
        var exception = new InvalidOperationException("broken operator");

        using (var scope = recorder.BeginOperator("op1", "ProjectTable"))
        {
            clock.Advance(TimeSpan.FromMilliseconds(4));
            scope.RecordException(exception);
        }

        var operation = recorder.CreateSnapshot().Operators.Single();

        Assert.AreEqual(1, operation.ExceptionCount);
        Assert.AreEqual(typeof(InvalidOperationException).FullName, operation.ExceptionType);
        Assert.AreEqual("broken operator", operation.ExceptionMessage);
        Assert.AreEqual(TimeSpan.FromMilliseconds(4), operation.ElapsedTime);
    }

    [TestMethod]
    public void QueryProfileRecorder_WhenScopeRecordsRows_FlushesRowsOnDispose()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);

        using (var scope = recorder.BeginOperator("op1", "ForEach"))
        {
            scope.AddInputRows(3);
            scope.AddOutputRows(2);
            clock.Advance(TimeSpan.FromMilliseconds(4));
        }

        var operation = recorder.CreateSnapshot().Operators.Single();

        Assert.AreEqual(3, operation.InputRows);
        Assert.AreEqual(2, operation.OutputRows);
        Assert.AreEqual(TimeSpan.FromMilliseconds(4), operation.ElapsedTime);
    }

    [TestMethod]
    public void QueryProfileRecorder_WhenStringRowApisAreUsed_RecordsRows()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);

        using (recorder.BeginOperator("op1", "AppendRow"))
        {
            recorder.AddOperatorInputRows("op1", 5);
            recorder.AddOperatorOutputRows("op1", 4);
        }

        var operation = recorder.CreateSnapshot().Operators.Single();

        Assert.AreEqual(5, operation.InputRows);
        Assert.AreEqual(4, operation.OutputRows);
    }

    [TestMethod]
    public void QueryProfileRecorder_WhenScopeAndStringRowsAreUsed_AddsBoth()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);

        using (var scope = recorder.BeginOperator("op1", "AppendRow"))
        {
            scope.AddInputRows(2);
            scope.AddOutputRows(3);
            recorder.AddOperatorInputRows("op1", 5);
            recorder.AddOperatorOutputRows("op1", 7);
        }

        var operation = recorder.CreateSnapshot().Operators.Single();

        Assert.AreEqual(7, operation.InputRows);
        Assert.AreEqual(10, operation.OutputRows);
    }

    [TestMethod]
    public void QueryProfileRecorder_WhenHandleApisAreUsed_MatchesStringApis()
    {
        var stringClock = new FakeProfileClock();
        var stringRecorder = new QueryProfileRecorder(stringClock);
        using (var scope = stringRecorder.BeginOperator("op1", "AppendRow"))
        {
            scope.AddInputRows(2);
            scope.AddOutputRows(3);
            stringRecorder.AddOperatorInputRows("op1", 5);
            stringRecorder.AddOperatorOutputRows("op1", 7);
            stringClock.Advance(TimeSpan.FromMilliseconds(4));
        }

        var handleClock = new FakeProfileClock();
        var handleRecorder = new QueryProfileRecorder(handleClock);
        var handle = handleRecorder.GetOperatorHandle("op1", "AppendRow");
        using (var scope = handleRecorder.BeginOperator(handle))
        {
            scope.AddInputRows(2);
            scope.AddOutputRows(3);
            handleRecorder.AddOperatorInputRows(handle, 5);
            handleRecorder.AddOperatorOutputRows(handle, 7);
            handleClock.Advance(TimeSpan.FromMilliseconds(4));
        }

        var stringOperation = stringRecorder.CreateSnapshot().Operators.Single();
        var handleOperation = handleRecorder.CreateSnapshot().Operators.Single();

        Assert.IsTrue(handle.IsEnabled);
        Assert.AreEqual(stringOperation.Id, handleOperation.Id);
        Assert.AreEqual(stringOperation.Name, handleOperation.Name);
        Assert.AreEqual(stringOperation.InputRows, handleOperation.InputRows);
        Assert.AreEqual(stringOperation.OutputRows, handleOperation.OutputRows);
        Assert.AreEqual(stringOperation.ElapsedTime, handleOperation.ElapsedTime);
    }

    [TestMethod]
    public void QueryProfileRecorder_WhenValueScopeApisAreUsed_MatchesReferenceScopeApis()
    {
        var referenceClock = new FakeProfileClock();
        var referenceRecorder = new QueryProfileRecorder(referenceClock);
        var referenceHandle = referenceRecorder.GetOperatorHandle("op1", "AppendRow");
        using (var scope = referenceRecorder.BeginOperator(referenceHandle))
        {
            scope.AddInputRows(2);
            scope.AddOutputRows(3);
            referenceClock.Advance(TimeSpan.FromMilliseconds(4));
        }

        var valueClock = new FakeProfileClock();
        var valueRecorder = new QueryProfileRecorder(valueClock);
        var valueHandle = valueRecorder.GetOperatorHandle("op1", "AppendRow");
        using (var scope = valueRecorder.BeginOperatorValue(valueHandle))
        {
            scope.AddInputRows(2);
            scope.AddOutputRows(3);
            valueClock.Advance(TimeSpan.FromMilliseconds(4));
        }

        var referenceOperation = referenceRecorder.CreateSnapshot().Operators.Single();
        var valueOperation = valueRecorder.CreateSnapshot().Operators.Single();

        Assert.IsTrue(valueHandle.IsEnabled);
        Assert.AreEqual(referenceOperation.Id, valueOperation.Id);
        Assert.AreEqual(referenceOperation.Name, valueOperation.Name);
        Assert.AreEqual(referenceOperation.InputRows, valueOperation.InputRows);
        Assert.AreEqual(referenceOperation.OutputRows, valueOperation.OutputRows);
        Assert.AreEqual(referenceOperation.ElapsedTime, valueOperation.ElapsedTime);
    }

    [TestMethod]
    public void QueryProfileRecorder_WhenNoneHandleIsUsed_DoesNotRecordOperator()
    {
        var recorder = new QueryProfileRecorder(new FakeProfileClock());

        Assert.IsFalse(OperatorProfileHandle.None.IsEnabled);

        using (var scope = recorder.BeginOperator(OperatorProfileHandle.None))
        {
            scope.AddInputRows(1);
            recorder.AddOperatorOutputRows(OperatorProfileHandle.None, 1);
        }

        using (var scope = recorder.BeginOperatorValue(OperatorProfileHandle.None))
        {
            Assert.IsFalse(scope.IsEnabled);
            scope.AddInputRows(1);
            scope.AddOutputRows(1);
        }

        Assert.IsEmpty(recorder.CreateSnapshot().Operators);
    }

    [TestMethod]
    public void QueryProfileRecorder_WhenScopeRowsAreNonPositive_IgnoresRows()
    {
        var recorder = new QueryProfileRecorder(new FakeProfileClock());

        using (var scope = recorder.BeginOperator("op1", "AppendRow"))
        {
            scope.AddInputRows(0);
            scope.AddOutputRows(-1);
        }

        var operation = recorder.CreateSnapshot().Operators.Single();

        Assert.AreEqual(0, operation.InputRows);
        Assert.AreEqual(0, operation.OutputRows);
    }

    [TestMethod]
    public void QueryProfileRecorder_WhenOperatorRecordsMultipleExceptions_IncrementsExceptionCount()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);

        using (var scope = recorder.BeginOperator("op1", "HashAdd"))
        {
            scope.RecordException(new InvalidOperationException("first"));
            scope.RecordException(new ArgumentException("second"));
        }

        var operation = recorder.CreateSnapshot().Operators.Single();

        Assert.AreEqual(2, operation.ExceptionCount);
        Assert.AreEqual(typeof(ArgumentException).FullName, operation.ExceptionType);
        Assert.AreEqual("second", operation.ExceptionMessage);
    }

    [TestMethod]
    public void QueryProfileRecorder_WhenActiveExceptionIsRecorded_RecordsScopesAboveStartDepthOnly()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);
        var exception = new InvalidOperationException("helper failed");

        using (recorder.BeginOperator("op1", "Caller"))
        {
            var helperDepth = recorder.GetCurrentOperatorScopeDepth();
            using (recorder.BeginOperator("op2", "Helper"))
            {
                Assert.IsTrue(recorder.RecordActiveOperatorException(exception, helperDepth));
            }
        }

        var operators = recorder.CreateSnapshot().Operators.ToDictionary(static operation => operation.Id);

        Assert.AreEqual(0, operators["op1"].ExceptionCount);
        Assert.AreEqual(1, operators["op2"].ExceptionCount);
        Assert.AreEqual(typeof(InvalidOperationException).FullName, operators["op2"].ExceptionType);
        Assert.AreEqual("helper failed", operators["op2"].ExceptionMessage);
    }

    [TestMethod]
    public void QueryProfileRecorder_WhenActiveScopesAreDisposed_DisposesScopesAboveStartDepthOnly()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);

        using (recorder.BeginOperator("op1", "Outer"))
        {
            var startDepth = recorder.GetCurrentOperatorScopeDepth();
            recorder.BeginOperator("op2", "Inner");
            clock.Advance(TimeSpan.FromMilliseconds(5));

            recorder.DisposeActiveOperatorScopes(startDepth);

            Assert.AreEqual(startDepth, recorder.GetCurrentOperatorScopeDepth());
            clock.Advance(TimeSpan.FromMilliseconds(7));
        }

        var operators = recorder.CreateSnapshot().Operators.ToDictionary(static operation => operation.Id);

        Assert.AreEqual(TimeSpan.FromMilliseconds(12), operators["op1"].ElapsedTime);
        Assert.AreEqual(TimeSpan.FromMilliseconds(5), operators["op2"].ElapsedTime);
    }

    [TestMethod]
    public void QueryProfileRecorder_WhenActiveScopesAreDisposedTwice_DoesNotDoubleCount()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);

        using (recorder.BeginOperator("op1", "Outer"))
        {
            var startDepth = recorder.GetCurrentOperatorScopeDepth();
            var inner = recorder.BeginOperator("op2", "Inner");
            clock.Advance(TimeSpan.FromMilliseconds(5));

            recorder.DisposeActiveOperatorScopes(startDepth);
            recorder.DisposeActiveOperatorScopes(startDepth);
            inner.Dispose();

            clock.Advance(TimeSpan.FromMilliseconds(2));
        }

        var operators = recorder.CreateSnapshot().Operators.ToDictionary(static operation => operation.Id);

        Assert.AreEqual(TimeSpan.FromMilliseconds(7), operators["op1"].ElapsedTime);
        Assert.AreEqual(TimeSpan.FromMilliseconds(5), operators["op2"].ElapsedTime);
    }

    [TestMethod]
    public void QueryProfileRecorder_WhenScopeIsDisposedOutOfOrder_KeepsRemainingFrameUsable()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);

        var outer = recorder.BeginOperator("op1", "Outer");
        clock.Advance(TimeSpan.FromMilliseconds(2));
        var inner = recorder.BeginOperator("op2", "Inner");
        clock.Advance(TimeSpan.FromMilliseconds(3));

        outer.Dispose();
        inner.AddOutputRows(4);
        clock.Advance(TimeSpan.FromMilliseconds(5));
        inner.Dispose();

        var operators = recorder.CreateSnapshot().Operators.ToDictionary(static operation => operation.Id);

        Assert.AreEqual(TimeSpan.FromMilliseconds(5), operators["op1"].ElapsedTime);
        Assert.AreEqual(TimeSpan.FromMilliseconds(8), operators["op2"].ElapsedTime);
        Assert.AreEqual(4, operators["op2"].OutputRows);
    }

    private sealed class FakeProfileClock : IProfileClock
    {
        private long _timestamp;

        public long GetTimestamp() => _timestamp;

        public TimeSpan GetElapsedTime(long startTimestamp, long endTimestamp) =>
            TimeSpan.FromTicks(endTimestamp - startTimestamp);

        public void Advance(TimeSpan elapsed) => _timestamp += elapsed.Ticks;
    }
}
