namespace Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

public static class GeneratedCSharpCodeFormatter
{
    public static string Normalize(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source
            .Replace("? )", "?)", StringComparison.Ordinal)
            .Replace("? >", "?>", StringComparison.Ordinal)
            .Replace("? ,", "?,", StringComparison.Ordinal);
    }
}
