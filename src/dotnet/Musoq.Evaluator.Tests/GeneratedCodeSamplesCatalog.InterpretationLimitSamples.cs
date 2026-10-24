namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateInterpretationLimitSamples() =>
    [
        BinaryInterpretationSample("I01_ConfiguredBinaryRepeatLimit",
            "binary Item { X: byte } binary Packet { Records: Item repeat until eof } " +
            "select Count(r.X) as N from #test.files() f cross apply Interpret<Packet>(f.Content) p cross apply p.Records r") with
        {
            CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false).WithInterpretationLimits(new(20_000))
        },
        TextInterpretationSample("I02_ConfiguredTextRepeatLimit",
            "text Item { X: chars[1] } text Packet { Records: repeat Item until end } " +
            "select Count(r.X) as N from #test.lines() f cross apply Parse<Packet>(f.Line) p cross apply p.Records r") with
        {
            CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false).WithInterpretationLimits(new(20_000))
        }
    ];
}
