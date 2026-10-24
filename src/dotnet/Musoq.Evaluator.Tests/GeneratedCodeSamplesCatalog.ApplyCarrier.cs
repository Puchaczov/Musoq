namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample CreateApplyCarrierSample() => new()
    {
        Name = "I03_ChainedInterpretationCarrier",
        FileName = "I03_ChainedInterpretationCarrier.cs",
        Query = "binary SampleReading { Score: int le } select batch.Label, reading.Score from #fixture.batches('demo', false) batch " +
            "cross apply #fixture.samples(batch.Key) sample cross apply Interpret<SampleReading>(sample.ReadPayload()) reading",
        Category = "Interpretation",
        Format = GeneratedCodeSampleFormat.QueryHeaderAndGeneratedCode,
        CreateSchemaProvider = static () => new ApplyCarrierSchemaProvider()
    };
}
