namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample CreateInterpretedCteSample() => BinaryInterpretationSample(
        "I04_InterpretedCteShape",
        "binary Inner { X: byte, Y: byte } binary Outer { Inner: Inner } " +
        "with c (Renamed) as (select s.Inner as Inner from #test.files() f " +
        "cross apply Interpret<Outer>(f.Content) s) select c.Renamed.X as X, c.Renamed.Y as Y from c c") with
        {
            CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false)
        };

    private static GeneratedCodeSample CreateInterpretedIndexedCteSample() => BinaryInterpretationSample(
        "I05_InterpretedIndexedCteShape",
        "binary Inner { X: byte, Y: byte } binary Outer { Items: Inner[2] } " +
        "with c as (select s.Items[1] as Item from #test.files() f " +
        "cross apply Interpret<Outer>(f.Content) s) select c.Item.X as X, c.Item.Y as Y from c c") with
        {
            CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false)
        };
}
