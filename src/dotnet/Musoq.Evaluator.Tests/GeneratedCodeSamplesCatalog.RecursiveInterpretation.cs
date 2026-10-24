namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateRecursiveInterpretationSamples() =>
    [
        BinaryInterpretationSample("I06_RecursivePartialInterpretation",
            "binary SampleBlock { ValueCount: byte, Values: byte[ValueCount] } binary SampleBatch { Blocks: SampleBlock[2] } " +
            "with recursive offs (Bytes, Off, Used) as (select f.Content, 0, 0 from #test.files() f union all " +
            "select o.Bytes, o.Off + o.Used, p.BytesConsumed from offs o " +
            "cross apply PartialInterpret<SampleBatch>(FromBase64(ToBase64(o.Bytes, o.Off + o.Used, Length(o.Bytes) - o.Off - o.Used))) p " +
            "where o.Off + o.Used < Length(o.Bytes)) select o.Off, o.Used from offs o") with
        {
            CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false)
        },
        BinaryInterpretationSample("I07_RecursiveInterpretationChain",
            "binary SampleWindow { Bytes: byte[4] } binary SampleAdvance { Used: int le } " +
            "with recursive offs (Bytes, Off, Used) as (select f.Content, 0, 0 from #test.files() f union all " +
            "select o.Bytes, o.Off + o.Used, p.Used from offs o " +
            "cross apply InterpretAt<SampleWindow>(o.Bytes, o.Off + o.Used) h cross apply Interpret<SampleAdvance>(h.Bytes) p " +
            "where o.Off + o.Used < Length(o.Bytes)) select o.Off, o.Used from offs o") with
        {
            CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false)
        }
    ];
}
