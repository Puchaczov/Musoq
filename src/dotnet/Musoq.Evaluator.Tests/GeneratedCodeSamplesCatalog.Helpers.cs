namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{

    private static GeneratedCodeSample Basic(string name, string category, string query)
    {
        return new GeneratedCodeSample
        {
            Name = name,
            FileName = $"{name}.cs",
            Query = query,
            Category = category,
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateBasicSchemaProvider
        };
    }

    private static GeneratedCodeSample BasicWithOptions(
        string name,
        string category,
        string query,
        CompilationOptions compilationOptions)
    {
        return Basic(name, category, query) with
        {
            CompilationOptions = compilationOptions
        };
    }
}
