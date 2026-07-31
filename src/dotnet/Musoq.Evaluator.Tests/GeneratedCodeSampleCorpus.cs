using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private static class GeneratedCodeSampleCorpus
    {
        private const int DefaultDegreeOfParallelism = 4;
        private const string DegreeOfParallelismEnvironmentVariable = "MUSOQ_EVALUATOR_CORPUS_DEGREE";
        private static readonly Lazy<GeneratedCodeSampleFile[]> All = new(
            Create,
            LazyThreadSafetyMode.ExecutionAndPublication);

        public static GeneratedCodeSampleFile[] ReadAll()
        {
            return All.Value;
        }

        private static GeneratedCodeSampleFile[] Create()
        {
            var startedUtc = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var samples = GeneratedCodeSamplesCatalog.Samples;
            var degreeOfParallelism = ResolveDegreeOfParallelism();
            var generated = new GeneratedCodeSampleFile[samples.Count];

            try
            {
                Parallel.For(
                    0,
                    samples.Count,
                    new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism },
                    index =>
                    {
                        var sample = samples[index];
                        generated[index] = new GeneratedCodeSampleFile(
                            sample.FileName,
                            sample.Category,
                            GeneratedCodeSampleArtifacts.Generate(sample, LoggerResolver));
                    });

                return generated;
            }
            finally
            {
                stopwatch.Stop();
                GeneratedCodeSampleTiming.RecordCorpusSetup(
                    samples.Count,
                    degreeOfParallelism,
                    startedUtc,
                    DateTimeOffset.UtcNow,
                    stopwatch.Elapsed,
                    GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore);
            }
        }

        private static int ResolveDegreeOfParallelism()
        {
            var configured = Environment.GetEnvironmentVariable(DegreeOfParallelismEnvironmentVariable);
            if (int.TryParse(configured, out var degreeOfParallelism) && degreeOfParallelism > 0)
                return Math.Min(degreeOfParallelism, 64);

            return Math.Min(DefaultDegreeOfParallelism, Math.Max(1, Environment.ProcessorCount));
        }
    }
}
