using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Diagnostics;

namespace Musoq.Evaluator.Tests.Diagnostics;

[TestClass]
public class QueryProfilingCoreTests
{
    [TestMethod]
    public void ProfiledEnumerable_WhenEmpty_RecordsZeroRowsAndUnknownDiagnosis()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);
        var rows = recorder.ProfileSource("empty", new ClockAdvancingEnumerable<int>([], clock, TimeSpan.FromMilliseconds(3))).ToArray();

        var source = recorder.CreateSnapshot().Sources.Single();

        Assert.AreEqual(0, rows.Length);
        Assert.AreEqual("empty", source.Name);
        Assert.AreEqual(0, source.RowsRead);
        Assert.IsNull(source.FirstRowLatency);
        Assert.IsNull(source.LastRowTime);
        Assert.AreEqual(TimeSpan.FromMilliseconds(3), source.MoveNextWaitTime);
        Assert.AreEqual(TimeSpan.Zero, source.ConsumerGapTime);
        Assert.AreEqual(0, source.ExceptionCount);
        Assert.AreEqual(SourceProfileDiagnosis.Unknown, source.Diagnosis);
    }

    [TestMethod]
    public void ProfiledEnumerable_WhenRows_RecordsRowsFirstLastAndWait()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);
        var enumerable = recorder.ProfileSource(
            "numbers",
            new ClockAdvancingEnumerable<int>(
                [1, 2, 3],
                clock,
                TimeSpan.FromMilliseconds(10),
                TimeSpan.FromMilliseconds(20),
                TimeSpan.FromMilliseconds(30),
                TimeSpan.FromMilliseconds(5)));

        var rows = enumerable.ToArray();
        var source = recorder.CreateSnapshot().Sources.Single();

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, rows);
        Assert.AreEqual(3, source.RowsRead);
        Assert.AreEqual(TimeSpan.FromMilliseconds(10), source.FirstRowLatency);
        Assert.AreEqual(TimeSpan.FromMilliseconds(60), source.LastRowTime);
        Assert.AreEqual(TimeSpan.FromMilliseconds(65), source.MoveNextWaitTime);
        Assert.AreEqual(TimeSpan.Zero, source.ConsumerGapTime);
        Assert.IsFalse(source.IsTimingEstimated);
        Assert.AreEqual(4, source.TimedMoveNextCalls);
        Assert.AreEqual(0, source.UntimedMoveNextCalls);
        Assert.AreEqual(SourceProfileDiagnosis.SourceBound, source.Diagnosis);
    }

    [TestMethod]
    public void ProfiledEnumerable_WhenAdaptiveSourceIsLarge_ReportsEstimatedTimingMetadata()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);
        var sourceRecorder = recorder.CreateAdaptiveSourceRecorder("large");
        var enumerable = new ProfiledEnumerable<int>(
            new ClockAdvancingEnumerable<int>(
                Enumerable.Range(0, 1_000).ToArray(),
                clock,
                Enumerable.Repeat(TimeSpan.FromMilliseconds(1), 1_001).ToArray()),
            sourceRecorder);

        var rows = enumerable.ToArray();
        var source = recorder.CreateSnapshot().Sources.Single();

        Assert.AreEqual(1_000, rows.Length);
        Assert.AreEqual(1_000, source.RowsRead);
        Assert.IsTrue(source.IsTimingEstimated);
        Assert.IsGreaterThan(128, source.TimedMoveNextCalls);
        Assert.IsGreaterThan(0, source.UntimedMoveNextCalls);
        Assert.AreEqual(1_001, source.TimedMoveNextCalls + source.UntimedMoveNextCalls);
        Assert.AreEqual(SourceProfileDiagnosis.SourceBound, source.Diagnosis);
    }

    [TestMethod]
    public void ProfiledEnumerable_Create_WhenSourceHasCheapCount_PreservesNonEnumeratedCountWithoutListIndexing()
    {
        AssertCountPreserved([1, 2, 3]);
        AssertCountPreserved(new List<int> { 1, 2, 3, 4 });
        AssertCountPreserved(new Collection<int> { 1, 2, 3, 4, 5 });
    }

    [TestMethod]
    public void ProfiledEnumerable_WhenAdaptiveConsumerIsSlow_DiagnosesEvaluatorBound()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);
        var sourceRecorder = recorder.CreateAdaptiveSourceRecorder("slow-consumer");
        var enumerable = new ProfiledEnumerable<int>(
            new ClockAdvancingEnumerable<int>(
                Enumerable.Range(0, 200).ToArray(),
                clock,
                Enumerable.Repeat(TimeSpan.Zero, 201).ToArray()),
            sourceRecorder);

        using (var enumerator = enumerable.GetEnumerator())
        {
            while (enumerator.MoveNext())
                clock.Advance(TimeSpan.FromMilliseconds(3));
        }

        var source = recorder.CreateSnapshot().Sources.Single();

        Assert.AreEqual(200, source.RowsRead);
        Assert.IsTrue(source.IsTimingEstimated);
        Assert.IsTrue(source.ConsumerGapTime > source.MoveNextWaitTime);
        Assert.AreEqual(SourceProfileDiagnosis.EvaluatorBound, source.Diagnosis);
    }

    [TestMethod]
    public void ProfiledEnumerable_WhenSourceIsSlow_DiagnosesSourceBound()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);
        var enumerable = recorder.ProfileSource(
            "slow-source",
            new ClockAdvancingEnumerable<int>(
                [1, 2],
                clock,
                TimeSpan.FromMilliseconds(50),
                TimeSpan.FromMilliseconds(50),
                TimeSpan.Zero));

        foreach (var _ in enumerable)
        {
        }

        var source = recorder.CreateSnapshot().Sources.Single();

        Assert.AreEqual(TimeSpan.FromMilliseconds(100), source.MoveNextWaitTime);
        Assert.AreEqual(TimeSpan.Zero, source.ConsumerGapTime);
        Assert.AreEqual(SourceProfileDiagnosis.SourceBound, source.Diagnosis);
    }

    [TestMethod]
    public void ProfiledEnumerable_WhenConsumerIsSlow_RecordsConsumerGapAndDiagnosesEvaluatorBound()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);
        var enumerable = recorder.ProfileSource(
            "slow-consumer",
            new ClockAdvancingEnumerable<int>(
                [1, 2, 3],
                clock,
                TimeSpan.FromMilliseconds(1),
                TimeSpan.FromMilliseconds(1),
                TimeSpan.FromMilliseconds(1),
                TimeSpan.Zero));

        using var enumerator = enumerable.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        clock.Advance(TimeSpan.FromMilliseconds(10));
        Assert.IsTrue(enumerator.MoveNext());
        clock.Advance(TimeSpan.FromMilliseconds(10));
        Assert.IsTrue(enumerator.MoveNext());
        clock.Advance(TimeSpan.FromMilliseconds(10));
        Assert.IsFalse(enumerator.MoveNext());

        var source = recorder.CreateSnapshot().Sources.Single();

        Assert.AreEqual(3, source.RowsRead);
        Assert.AreEqual(TimeSpan.FromMilliseconds(3), source.MoveNextWaitTime);
        Assert.AreEqual(TimeSpan.FromMilliseconds(30), source.ConsumerGapTime);
        Assert.AreEqual(SourceProfileDiagnosis.EvaluatorBound, source.Diagnosis);
    }

    [TestMethod]
    public void ProfiledEnumerable_WhenDisposedEarly_FlushesPartialRowsOnlyOnce()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);
        var enumerable = recorder.ProfileSource(
            "partial",
            new ClockAdvancingEnumerable<int>(
                [1, 2, 3],
                clock,
                TimeSpan.FromMilliseconds(5),
                TimeSpan.FromMilliseconds(7),
                TimeSpan.FromMilliseconds(9)));

        var enumerator = enumerable.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        enumerator.Dispose();
        enumerator.Dispose();

        var source = recorder.CreateSnapshot().Sources.Single();

        Assert.AreEqual(1, source.RowsRead);
        Assert.AreEqual(TimeSpan.FromMilliseconds(5), source.FirstRowLatency);
        Assert.AreEqual(TimeSpan.FromMilliseconds(5), source.LastRowTime);
        Assert.AreEqual(TimeSpan.FromMilliseconds(5), source.MoveNextWaitTime);
        Assert.AreEqual(TimeSpan.Zero, source.ConsumerGapTime);
    }

    [TestMethod]
    public void ProfiledEnumerable_WhenDisposedEarlyInsideOperator_ExcludesSourceWaitOnlyOnce()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);
        var enumerable = recorder.ProfileSource(
            "partial",
            new ClockAdvancingEnumerable<int>(
                [1, 2],
                clock,
                TimeSpan.FromMilliseconds(5),
                TimeSpan.FromMilliseconds(7)));

        using (recorder.BeginOperator("op1", "ForEach"))
        {
            var enumerator = enumerable.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            clock.Advance(TimeSpan.FromMilliseconds(3));
            enumerator.Dispose();
            enumerator.Dispose();
            clock.Advance(TimeSpan.FromMilliseconds(2));
        }

        var snapshot = recorder.CreateSnapshot();
        var source = snapshot.Sources.Single();
        var operation = snapshot.Operators.Single();

        Assert.AreEqual(1, source.RowsRead);
        Assert.AreEqual(TimeSpan.FromMilliseconds(5), source.MoveNextWaitTime);
        Assert.AreEqual(TimeSpan.FromMilliseconds(5), operation.ElapsedTime);
    }

    [TestMethod]
    public void ProfiledEnumerable_WhenInnerThrows_RecordsExceptionAndRethrows()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);
        var enumerable = recorder.ProfileSource<int>(
            "fault",
            new ThrowingEnumerable<int>(
                clock,
                TimeSpan.FromMilliseconds(7),
                new InvalidOperationException("broken source")));

        var exception = Assert.Throws<InvalidOperationException>(() => enumerable.ToArray());
        var source = recorder.CreateSnapshot().Sources.Single();

        Assert.AreEqual("broken source", exception.Message);
        Assert.AreEqual(0, source.RowsRead);
        Assert.AreEqual(TimeSpan.FromMilliseconds(7), source.MoveNextWaitTime);
        Assert.AreEqual(1, source.ExceptionCount);
        Assert.AreEqual(typeof(InvalidOperationException).FullName, source.ExceptionType);
        Assert.AreEqual("broken source", source.ExceptionMessage);
        Assert.AreEqual(SourceProfileDiagnosis.Unknown, source.Diagnosis);
    }

    [TestMethod]
    public void ProfiledEnumerable_WhenGetEnumeratorThrows_RecordsExceptionAndRethrows()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);
        var enumerable = recorder.ProfileSource<int>(
            "fault",
            new ThrowingGetEnumeratorEnumerable<int>(
                clock,
                TimeSpan.FromMilliseconds(5),
                new InvalidOperationException("broken acquisition")));

        var exception = Assert.Throws<InvalidOperationException>(() => enumerable.ToArray());
        var source = recorder.CreateSnapshot().Sources.Single();

        Assert.AreEqual("broken acquisition", exception.Message);
        Assert.AreEqual(0, source.RowsRead);
        Assert.AreEqual(TimeSpan.FromMilliseconds(5), source.MoveNextWaitTime);
        Assert.AreEqual(1, source.ExceptionCount);
        Assert.AreEqual(typeof(InvalidOperationException).FullName, source.ExceptionType);
        Assert.AreEqual("broken acquisition", source.ExceptionMessage);
        Assert.AreEqual(SourceProfileDiagnosis.Unknown, source.Diagnosis);
    }

    [TestMethod]
    public void ProfiledEnumerable_WhenInnerThrowsInsideOperator_ExcludesSourceWaitBeforeRethrow()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);
        var enumerable = recorder.ProfileSource<int>(
            "fault",
            new ThrowingEnumerable<int>(
                clock,
                TimeSpan.FromMilliseconds(7),
                new InvalidOperationException("broken source")));

        using (recorder.BeginOperator("op1", "ForEach"))
        {
            Assert.Throws<InvalidOperationException>(() => enumerable.ToArray());
            clock.Advance(TimeSpan.FromMilliseconds(3));
        }

        var snapshot = recorder.CreateSnapshot();
        var source = snapshot.Sources.Single();
        var operation = snapshot.Operators.Single();

        Assert.AreEqual(TimeSpan.FromMilliseconds(7), source.MoveNextWaitTime);
        Assert.AreEqual(1, source.ExceptionCount);
        Assert.AreEqual(TimeSpan.FromMilliseconds(3), operation.ElapsedTime);
    }

    [TestMethod]
    public void ProfiledEnumerable_WhenConsumedInsideOperator_ExcludesMoveNextWaitFromOperatorElapsed()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);
        var enumerable = recorder.ProfileSource(
            "numbers",
            new ClockAdvancingEnumerable<int>(
                [1, 2],
                clock,
                TimeSpan.FromMilliseconds(10),
                TimeSpan.FromMilliseconds(20),
                TimeSpan.FromMilliseconds(5)));

        using (recorder.BeginOperator("op1", "ForEach"))
        {
            clock.Advance(TimeSpan.FromMilliseconds(2));
            foreach (var _ in enumerable)
                clock.Advance(TimeSpan.FromMilliseconds(3));

            clock.Advance(TimeSpan.FromMilliseconds(4));
        }

        var snapshot = recorder.CreateSnapshot();
        var source = snapshot.Sources.Single();
        var operation = snapshot.Operators.Single();

        Assert.AreEqual(TimeSpan.FromMilliseconds(35), source.MoveNextWaitTime);
        Assert.AreEqual(TimeSpan.FromMilliseconds(12), operation.ElapsedTime);
    }

    [TestMethod]
    public void ProfiledEnumerable_WhenNoOperatorIsActive_StillRecordsSourceWait()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);
        var rows = recorder.ProfileSource(
            "numbers",
            new ClockAdvancingEnumerable<int>(
                [1],
                clock,
                TimeSpan.FromMilliseconds(7),
                TimeSpan.FromMilliseconds(1))).ToArray();

        var snapshot = recorder.CreateSnapshot();
        var source = snapshot.Sources.Single();

        CollectionAssert.AreEqual(new[] { 1 }, rows);
        Assert.AreEqual(TimeSpan.FromMilliseconds(8), source.MoveNextWaitTime);
        Assert.AreEqual(0, snapshot.Operators.Count);
    }

    [TestMethod]
    public void ProfiledEnumerable_WhenSourceWaitOccursOnWorkerThread_DoesNotExcludeParentOperator()
    {
        var clock = new FakeProfileClock();
        var recorder = new QueryProfileRecorder(clock);

        using (recorder.BeginOperator("op1", "Parent"))
        {
            Exception? workerException = null;
            var worker = new Thread(() =>
            {
                try
                {
                    recorder.ProfileSource(
                        "worker",
                        new ClockAdvancingEnumerable<int>(
                            [1],
                            clock,
                            TimeSpan.FromMilliseconds(50),
                            TimeSpan.Zero)).ToArray();
                }
                catch (Exception exception)
                {
                    workerException = exception;
                }
            });

            worker.Start();
            worker.Join();

            if (workerException != null)
                throw workerException;

            clock.Advance(TimeSpan.FromMilliseconds(10));
        }

        var snapshot = recorder.CreateSnapshot();
        var source = snapshot.Sources.Single();
        var operation = snapshot.Operators.Single();

        Assert.AreEqual(TimeSpan.FromMilliseconds(50), source.MoveNextWaitTime);
        Assert.AreEqual(TimeSpan.FromMilliseconds(60), operation.ElapsedTime);
    }

    [TestMethod]
    public void SourceProfileDiagnosisClassifier_ClassifiesFixedSnapshots()
    {
        Assert.AreEqual(
            SourceProfileDiagnosis.SourceBound,
            SourceProfileDiagnosisClassifier.Classify(Snapshot(TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(5))));
        Assert.AreEqual(
            SourceProfileDiagnosis.EvaluatorBound,
            SourceProfileDiagnosisClassifier.Classify(Snapshot(TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(20))));
        Assert.AreEqual(
            SourceProfileDiagnosis.Balanced,
            SourceProfileDiagnosisClassifier.Classify(Snapshot(TimeSpan.FromMilliseconds(11), TimeSpan.FromMilliseconds(10))));
        Assert.AreEqual(
            SourceProfileDiagnosis.Unknown,
            SourceProfileDiagnosisClassifier.Classify(Snapshot(TimeSpan.Zero, TimeSpan.Zero)));
    }

    [TestMethod]
    public void QueryProfileTextPrinter_PrintsStableLabelsAndCounts()
    {
        var snapshot = new QueryProfileSnapshot(
            TimeSpan.FromMilliseconds(42),
            [
                new SourceProfileSnapshot(
                    "numbers",
                    3,
                    TimeSpan.FromMilliseconds(1),
                    TimeSpan.FromMilliseconds(3),
                    TimeSpan.FromMilliseconds(4),
                    TimeSpan.FromMilliseconds(2),
                    0,
                    null,
                    null,
                    SourceProfileDiagnosis.Balanced)
                {
                    IsTimingEstimated = true,
                    TimedMoveNextCalls = 2,
                    UntimedMoveNextCalls = 1
                }
            ],
            [
                new OperatorProfileSnapshot(
                    "op1",
                    "SourceScan",
                    0,
                    3,
                    TimeSpan.FromMilliseconds(5)),
                new OperatorProfileSnapshot(
                    "op2",
                    "SortTable",
                    0,
                    0,
                    TimeSpan.Zero,
                    HasActualStats: false),
                new OperatorProfileSnapshot(
                    "op3",
                    "AppendRow",
                    0,
                    3,
                    TimeSpan.Zero)
                {
                    HasElapsedTime = false
                }
            ]);

        var text = QueryProfileTextPrinter.Print(snapshot);

        StringAssert.Contains(text, "Musoq query profile");
        StringAssert.Contains(text, "Total source rows: 3");
        StringAssert.Contains(text, "Sources:");
        StringAssert.Contains(text, "Source: numbers");
        StringAssert.Contains(text, "Rows read: 3");
        StringAssert.Contains(text, "MoveNext wait (estimated):");
        StringAssert.Contains(text, "Consumer gap (estimated):");
        StringAssert.Contains(text, "Diagnosis: Balanced");
        StringAssert.Contains(text, "Operators:");
        StringAssert.Contains(text, "op1 SourceScan");
        StringAssert.Contains(text, "op2 SortTable: stats unavailable");
        StringAssert.Contains(text, "op3 AppendRow: input rows=0, output rows=3, elapsed=n/a");
    }

    private static SourceProfileSnapshot Snapshot(TimeSpan moveNextWaitTime, TimeSpan consumerGapTime) =>
        new(
            "source",
            10,
            TimeSpan.Zero,
            TimeSpan.Zero,
            moveNextWaitTime,
            consumerGapTime,
            0,
            null,
            null,
            SourceProfileDiagnosis.Unknown);

    private static void AssertCountPreserved(IEnumerable<int> source)
    {
        var expectedRows = source.ToArray();
        var clock = new FakeProfileClock();
        var recorder = new SourceProfileRecorder("counted", clock);
        var profiled = ProfiledEnumerable<int>.Create(source, recorder);

        Assert.IsTrue(profiled.TryGetNonEnumeratedCount(out var count));
        Assert.AreEqual(expectedRows.Length, count);
        Assert.IsTrue(profiled is IReadOnlyCollection<int>);
        Assert.IsFalse(profiled is IReadOnlyList<int>);

        CollectionAssert.AreEqual(expectedRows, profiled.ToArray());
        Assert.AreEqual(expectedRows.Length, recorder.CreateSnapshot().RowsRead);
    }

    private sealed class FakeProfileClock : IProfileClock
    {
        private long _timestamp;

        public long GetTimestamp() => Interlocked.Read(ref _timestamp);

        public TimeSpan GetElapsedTime(long startTimestamp, long endTimestamp) =>
            TimeSpan.FromTicks(endTimestamp - startTimestamp);

        public void Advance(TimeSpan elapsed) => Interlocked.Add(ref _timestamp, elapsed.Ticks);
    }

    private sealed class ClockAdvancingEnumerable<T> : IEnumerable<T>
    {
        private readonly IReadOnlyList<T> _items;
        private readonly FakeProfileClock _clock;
        private readonly IReadOnlyList<TimeSpan> _moveNextDurations;

        public ClockAdvancingEnumerable(
            IReadOnlyList<T> items,
            FakeProfileClock clock,
            params TimeSpan[] moveNextDurations)
        {
            _items = items;
            _clock = clock;
            _moveNextDurations = moveNextDurations;
        }

        public IEnumerator<T> GetEnumerator() =>
            new Enumerator(_items, _clock, _moveNextDurations);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Enumerator : IEnumerator<T>
        {
            private readonly IReadOnlyList<T> _items;
            private readonly FakeProfileClock _clock;
            private readonly IReadOnlyList<TimeSpan> _moveNextDurations;
            private int _moveNextCalls;
            private int _index = -1;

            public Enumerator(
                IReadOnlyList<T> items,
                FakeProfileClock clock,
                IReadOnlyList<TimeSpan> moveNextDurations)
            {
                _items = items;
                _clock = clock;
                _moveNextDurations = moveNextDurations;
            }

            public T Current => _items[_index];

            object? IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_moveNextCalls < _moveNextDurations.Count)
                    _clock.Advance(_moveNextDurations[_moveNextCalls]);

                _moveNextCalls++;
                _index++;

                return _index < _items.Count;
            }

            public void Reset()
            {
                _moveNextCalls = 0;
                _index = -1;
            }

            public void Dispose()
            {
            }
        }
    }

    private sealed class ThrowingEnumerable<T> : IEnumerable<T>
    {
        private readonly FakeProfileClock _clock;
        private readonly TimeSpan _moveNextDuration;
        private readonly Exception _exception;

        public ThrowingEnumerable(FakeProfileClock clock, TimeSpan moveNextDuration, Exception exception)
        {
            _clock = clock;
            _moveNextDuration = moveNextDuration;
            _exception = exception;
        }

        public IEnumerator<T> GetEnumerator() =>
            new Enumerator(_clock, _moveNextDuration, _exception);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Enumerator : IEnumerator<T>
        {
            private readonly FakeProfileClock _clock;
            private readonly TimeSpan _moveNextDuration;
            private readonly Exception _exception;

            public Enumerator(FakeProfileClock clock, TimeSpan moveNextDuration, Exception exception)
            {
                _clock = clock;
                _moveNextDuration = moveNextDuration;
                _exception = exception;
            }

            public T Current => default!;

            object? IEnumerator.Current => Current;

            public bool MoveNext()
            {
                _clock.Advance(_moveNextDuration);
                throw _exception;
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
            }
        }
    }

    private sealed class ThrowingGetEnumeratorEnumerable<T> : IEnumerable<T>
    {
        private readonly FakeProfileClock _clock;
        private readonly TimeSpan _getEnumeratorDuration;
        private readonly Exception _exception;

        public ThrowingGetEnumeratorEnumerable(
            FakeProfileClock clock,
            TimeSpan getEnumeratorDuration,
            Exception exception)
        {
            _clock = clock;
            _getEnumeratorDuration = getEnumeratorDuration;
            _exception = exception;
        }

        public IEnumerator<T> GetEnumerator()
        {
            _clock.Advance(_getEnumeratorDuration);
            throw _exception;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
