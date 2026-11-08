using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class LikeMatcherCacheSlotTests
{
    [TestMethod]
    public void WorkerLocalSlot_WhenTwoWorkersUseSamePattern_ShouldPrepareOncePerWorker()
    {
        var slot = new LikeMatcherCacheSlot(workerLocal: true);
        using var ready = new Barrier(2);
        Exception? firstFailure = null;
        Exception? secondFailure = null;
        var first = new Thread(() => MatchRepeatedly(slot, ready, ref firstFailure));
        var second = new Thread(() => MatchRepeatedly(slot, ready, ref secondFailure));

        first.Start();
        second.Start();
        first.Join();
        second.Join();

        Assert.IsNull(firstFailure);
        Assert.IsNull(secondFailure);
        Assert.AreEqual(2, slot.MatcherConstructionCount);
        Assert.AreEqual(2, slot.Count);
    }

    [TestMethod]
    public void LikeDynamic_WhenTwoPatternsAlternate_ShouldPrepareEachPatternOnce()
    {
        var cacheSlot = new LikeMatcherCacheSlot();

        for (var repetition = 0; repetition < 16; repetition++)
        {
            Assert.IsTrue(Operators.LikeDynamic("alpha", "a%", cacheSlot));
            Assert.IsTrue(Operators.LikeDynamic("omega", "%ga", cacheSlot));
        }

        Assert.AreEqual(2, cacheSlot.Count);
        Assert.AreEqual(2, cacheSlot.MatcherConstructionCount);
    }

    [TestMethod]
    public void LikeDynamic_WhenSingleAsciiPrefixUsesFastHit_ShouldStillReuseCachedEquivalentPattern()
    {
        var factoryCalls = 0;
        var cacheSlot = new LikeMatcherCacheSlot(
            workerLocal: false,
            pattern =>
            {
                Interlocked.Increment(ref factoryCalls);
                return Operators.PrepareLike(pattern)!;
            });

        for (var repetition = 0; repetition < 32; repetition++)
        {
            var equivalentPattern = new string(['a', '%']);
            Assert.IsTrue(Operators.LikeDynamic("Alpha", equivalentPattern, cacheSlot));
            Assert.IsFalse(Operators.LikeDynamic("bravo", equivalentPattern, cacheSlot));
        }

        Assert.AreEqual(1, cacheSlot.Count);
        Assert.AreEqual(1, cacheSlot.MatcherConstructionCount);
        Assert.AreEqual(1, factoryCalls);
    }

    [TestMethod]
    public void LikeDynamic_WhenThirdPatternArrives_ShouldEvictInFifoOrder()
    {
        var cacheSlot = new LikeMatcherCacheSlot();
        Assert.IsTrue(Operators.LikeDynamic("alpha", "a%", cacheSlot));
        Assert.IsTrue(Operators.LikeDynamic("bravo", "b%", cacheSlot));
        Assert.IsTrue(Operators.LikeDynamic("alpha", "a%", cacheSlot));
        Assert.IsTrue(Operators.LikeDynamic("charlie", "c%", cacheSlot));
        Assert.IsTrue(Operators.LikeDynamic("bravo", "b%", cacheSlot));

        Assert.AreEqual(3, cacheSlot.MatcherConstructionCount);
        Assert.IsTrue(Operators.LikeDynamic("alpha", "a%", cacheSlot));
        Assert.AreEqual(4, cacheSlot.MatcherConstructionCount);
    }

    [TestMethod]
    public void LikeDynamic_WhenPatternsExceedCapacity_ShouldConstructExactlyOncePerMissAndStayBounded()
    {
        var factoryCalls = 0;
        var cacheSlot = new LikeMatcherCacheSlot(
            workerLocal: false,
            pattern =>
            {
                Interlocked.Increment(ref factoryCalls);
                return Operators.PrepareLike(pattern)!;
            });

        for (var index = 0; index < 4096; index++)
        {
            var pattern = $"value-{index}%";
            Assert.IsTrue(Operators.LikeDynamic($"value-{index}-payload", pattern, cacheSlot));
        }

        Assert.AreEqual(2, cacheSlot.Count);
        Assert.AreEqual(4096, cacheSlot.MatcherConstructionCount);
        Assert.AreEqual(4096, factoryCalls);

        Assert.IsTrue(Operators.LikeDynamic("value-4094-payload", "value-4094%", cacheSlot));
        Assert.IsTrue(Operators.LikeDynamic("value-4095-payload", "value-4095%", cacheSlot));
        Assert.AreEqual(4096, factoryCalls);
    }

    [TestMethod]
    public void LikeDynamic_WhenMixedPatternsChurnBeyondCapacity_ShouldMatchFreshHistoricalRegex()
    {
        var cacheSlot = new LikeMatcherCacheSlot();

        for (var index = 0; index < 64; index++)
        {
            var pattern = CreateChurnPattern(index);
            var inputs = new[]
            {
                $"value-{index}",
                $"value-{index}-payload",
                $"prefix-value-{index}",
                $"Ż{index}-payload",
                $@"C:\path\{index}\payload",
                string.Empty
            };

            foreach (var input in inputs)
            {
                Assert.AreEqual(
                    FreshHistoricalLike(input, pattern),
                    Operators.LikeDynamic(input, pattern, cacheSlot),
                    $"Input '{input}', pattern '{pattern}'.");
            }
        }

        Assert.AreEqual(2, cacheSlot.Count);
    }

    [TestMethod]
    public void WorkerLocalSlot_WhenWorkersChurnBeyondCapacity_ShouldConstructOncePerMiss()
    {
        const int workerCount = 4;
        const int patternCount = 512;
        var factoryCalls = 0;
        var cacheSlot = new LikeMatcherCacheSlot(
            workerLocal: true,
            pattern =>
            {
                Interlocked.Increment(ref factoryCalls);
                return Operators.PrepareLike(pattern)!;
            });
        using var ready = new Barrier(workerCount);
        var failures = new Exception?[workerCount];
        var workers = new Thread[workerCount];

        for (var workerIndex = 0; workerIndex < workers.Length; workerIndex++)
        {
            var capturedWorkerIndex = workerIndex;
            workers[workerIndex] = new Thread(() =>
            {
                try
                {
                    ready.SignalAndWait();
                    for (var patternIndex = 0; patternIndex < patternCount; patternIndex++)
                    {
                        var pattern = $"worker-{capturedWorkerIndex}-value-{patternIndex}%";
                        Assert.IsTrue(Operators.LikeDynamic(
                            $"worker-{capturedWorkerIndex}-value-{patternIndex}-payload",
                            pattern,
                            cacheSlot));
                    }
                }
                catch (Exception exception)
                {
                    failures[capturedWorkerIndex] = exception;
                }
            });
        }

        foreach (var worker in workers)
            worker.Start();
        foreach (var worker in workers)
            worker.Join();

        foreach (var failure in failures)
            Assert.IsNull(failure);
        Assert.AreEqual(workerCount * 2, cacheSlot.Count);
        Assert.AreEqual(workerCount * patternCount, cacheSlot.MatcherConstructionCount);
        Assert.AreEqual(workerCount * patternCount, factoryCalls);
    }

    [TestMethod]
    public void LikeDynamic_WhenOperandIsNull_ShouldNotPopulateExecutionLocalCache()
    {
        var cacheSlot = new LikeMatcherCacheSlot();

        Assert.IsFalse(Operators.LikeDynamic(null, "a%", cacheSlot));
        Assert.IsFalse(Operators.LikeDynamic("alpha", null, cacheSlot));

        Assert.AreEqual(0, cacheSlot.Count);
        Assert.AreEqual(0, cacheSlot.MatcherConstructionCount);
    }

    [TestMethod]
    public void LikeDynamic_WhenCultureChanges_ShouldKeepCultureSpecificEntries()
    {
        var cacheSlot = new LikeMatcherCacheSlot();
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.IsFalse(Operators.LikeDynamic("İ", "I", cacheSlot));

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.IsTrue(Operators.LikeDynamic("İ", "I", cacheSlot));

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.IsFalse(Operators.LikeDynamic("İ", "I", cacheSlot));
            Assert.AreEqual(2, cacheSlot.MatcherConstructionCount);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void PrepareLike_WhenAmbientCultureChanges_ShouldRetainPreparationCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var matcher = Operators.PrepareLike("I");

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.IsTrue(Operators.LikePrepared("İ", matcher));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void PreparedAsciiMatchers_WithUnicodeInputs_ShouldMatchHistoricalRegexAcrossCultures()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var cultures = new[] { string.Empty, "en-US", "pl-PL", "tr-TR" };
        var patterns = new[] { "I", "K", "S", "I%", "%I", "%I%", "K%", "%K", "%K%" };
        var inputs = new[] { "İ", "ı", "i", "K", "K", "k", "ſ", "S", "prefix-K-suffix", "İ-tail", "head-İ" };

        try
        {
            foreach (var cultureName in cultures)
            {
                CultureInfo.CurrentCulture = cultureName.Length == 0
                    ? CultureInfo.InvariantCulture
                    : CultureInfo.GetCultureInfo(cultureName);
                foreach (var pattern in patterns)
                {
                    var matcher = Operators.PrepareLike(pattern);
                    foreach (var input in inputs)
                    {
                        Assert.AreEqual(
                            FreshHistoricalLike(input, pattern),
                            Operators.LikePrepared(input, matcher),
                            $"Culture '{cultureName}', input '{input}', pattern '{pattern}'.");
                    }
                }
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    private static void MatchRepeatedly(
        LikeMatcherCacheSlot slot,
        Barrier ready,
        ref Exception? failure)
    {
        try
        {
            ready.SignalAndWait();
            for (var index = 0; index < 100; index++)
                Assert.IsTrue(Operators.LikeDynamic("alpha", "a%", slot));
        }
        catch (Exception exception)
        {
            failure = exception;
        }
    }

    private static bool FreshHistoricalLike(string input, string pattern)
    {
        var escaped = Regex.Escape(pattern);
        var sqlPattern = escaped
            .Replace("_", ".", StringComparison.Ordinal)
            .Replace("%", ".*", StringComparison.Ordinal);
        return Regex.IsMatch(
            input,
            string.Concat(@"\A", sqlPattern, @"\z"),
            RegexOptions.IgnoreCase | RegexOptions.Singleline,
            TimeSpan.FromSeconds(1));
    }

    private static string CreateChurnPattern(int index) => (index % 8) switch
    {
        0 => $"value-{index}",
        1 => $"value-{index}%",
        2 => $"%{index}",
        3 => $"%value-{index}%",
        4 => $"v_lue-{index}",
        5 => $"value%{index}",
        6 => $"Ż{index}%",
        _ => $@"C:\path\{index}\%"
    };
}
