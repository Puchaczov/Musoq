using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

namespace Musoq.Evaluator.Tests;

internal static class GeneratedCodeSampleTiming
{
    private static readonly ConcurrentQueue<GeneratedCodeSampleTimingEvent> Events = new();
    private static readonly object FileGate = new();

    public static void RecordGeneration(
        GeneratedCodeSample sample,
        DateTimeOffset startedUtc,
        DateTimeOffset finishedUtc,
        TimeSpan duration)
    {
        var timingEvent = new GeneratedCodeSampleTimingEvent(
            "generation",
            sample.FileName,
            sample.Category.ToString(),
            startedUtc,
            finishedUtc,
            duration.TotalMilliseconds);
        Events.Enqueue(timingEvent);
        WriteEvent(timingEvent);
    }

    public static void RecordCacheHit(GeneratedCodeSample sample)
    {
        var now = DateTimeOffset.UtcNow;
        var timingEvent = new GeneratedCodeSampleTimingEvent(
            "cache-hit",
            sample.FileName,
            sample.Category,
            now,
            now,
            0);
        Events.Enqueue(timingEvent);
        WriteEvent(timingEvent);
    }

    public static void RecordCorpusSetup(
        int sampleCount,
        int degreeOfParallelism,
        DateTimeOffset startedUtc,
        DateTimeOffset finishedUtc,
        TimeSpan duration,
        long allocatedBytes)
    {
        var timingEvent = new GeneratedCodeSampleTimingEvent(
            "corpus-setup",
            string.Empty,
            "generated-sample",
            startedUtc,
            finishedUtc,
            duration.TotalMilliseconds,
            sampleCount,
            degreeOfParallelism,
            allocatedBytes);
        Events.Enqueue(timingEvent);
        WriteEvent(timingEvent);
    }

    private static void WriteEvent(GeneratedCodeSampleTimingEvent timingEvent)
    {

        var outputDirectory = Environment.GetEnvironmentVariable("MUSOQ_EVALUATOR_TIMING_DIRECTORY");
        if (string.IsNullOrWhiteSpace(outputDirectory))
            return;

        Directory.CreateDirectory(outputDirectory);
        var path = Path.Combine(
            outputDirectory,
            $"generated-code-sample-timing-{Environment.ProcessId}.jsonl");

        lock (FileGate)
        {
            File.AppendAllText(
                path,
                JsonSerializer.Serialize(timingEvent) + Environment.NewLine);
        }
    }

    internal sealed record GeneratedCodeSampleTimingEvent(
        string Kind,
        string FileName,
        string Category,
        DateTimeOffset StartedUtc,
        DateTimeOffset FinishedUtc,
        double DurationMilliseconds,
        int? SampleCount = null,
        int? DegreeOfParallelism = null,
        long? AllocatedBytes = null);
}
