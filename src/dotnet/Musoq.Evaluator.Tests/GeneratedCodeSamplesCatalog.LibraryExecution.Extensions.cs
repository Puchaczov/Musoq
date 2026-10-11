using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateLibraryExecutionExtensionSamples() =>
    [
        ExtensionSample("K71_NativeStructGenericArguments",
            "param(items: (Value: int)[]) select StructElements(NativeEcho($items)) as Length from #schema.first()"),
        ExtensionSample("K72_NativeStructGenericResult",
            "param(items: (Value: int)[]) with echoed as (select StructEcho($items) as Echo from #schema.first()) select e.Echo as Echo from echoed e"),
        new GeneratedCodeSample
        {
            Name = "K73_NativeTupleResults", FileName = "K73_NativeTupleResults.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = LibraryExecutionNativeRowQueryTests.NativeTupleQuery, OutputType = typeof((int?, long)),
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider(LibraryExecutionNativeRowQueryTests.Rows())
        },
        ExtensionSample("K74_NativeInlineRecordArgument",
            "select NeedsStruct((Value: Position)) as Value from #schema.first()"),
        ExtensionSample("K75_NativeGenericResultDescription",
            "param(items: (Value: int)[]) desc query (select StructEcho($items) as Echo from #schema.first())")
    ];

    private static GeneratedCodeSample ExtensionSample(string name, string query) => new()
    {
        Name = name, FileName = name + ".cs", Query = query,
        Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
        CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false).WithStabilityAwareScalarReuse(),
        CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<
            LibraryExecutionNativeGenericQueryTests.Input, LibraryExecutionNativeGenericQueryTests.GenericLibrary>([new(1)])
    };
}
