using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Musoq.Evaluator;

/// <summary>
///     Produces the stable identity of options that affect compiled query shape or behavior.
/// </summary>
public static class CompilationOptionsFingerprint
{
    private const string FormatVersion = "compilation-options-v1";

    public static string Compute(CompilationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var builder = new StringBuilder(256);
        Append(builder, nameof(options.ParallelizationMode), (int)options.ParallelizationMode);
        Append(builder, nameof(options.UseHashJoin), options.UseHashJoin);
        Append(builder, nameof(options.UseSortMergeJoin), options.UseSortMergeJoin);
        Append(builder, nameof(options.UseCommonSubexpressionElimination), options.UseCommonSubexpressionElimination);
        Append(builder, nameof(options.UseConstantFolding), options.UseConstantFolding);
        Append(builder, nameof(options.UsePrimitiveTypeValidation), options.UsePrimitiveTypeValidation);
        Append(builder, nameof(options.UseCteParallelization), options.UseCteParallelization);
        Append(builder, nameof(options.UseCteSidecarIndexes), options.UseCteSidecarIndexes);
        Append(builder, nameof(options.InstrumentationMode), (int)options.InstrumentationMode);
        Append(builder, nameof(options.MaxDegreeOfParallelismOverride), options.MaxDegreeOfParallelismOverride);
        Append(builder, nameof(options.ForceTableResultMaterialization), options.ForceTableResultMaterialization);
        Append(builder, nameof(options.RecursiveCteLimits.MaxIterations), options.RecursiveCteLimits.MaxIterations);
        Append(builder, nameof(options.RecursiveCteLimits.MaxRows), options.RecursiveCteLimits.MaxRows);
        Append(builder, nameof(options.RecursiveCteLimits.MaxSnapshotRows), options.RecursiveCteLimits.MaxSnapshotRows);

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        return $"{FormatVersion}:{Convert.ToHexString(SHA256.HashData(bytes))}";
    }

    private static void Append<T>(StringBuilder builder, string name, T value)
        where T : IConvertible
    {
        builder
            .Append(name)
            .Append('=')
            .Append(value.ToString(CultureInfo.InvariantCulture))
            .Append(';');
    }

    private static void Append(StringBuilder builder, string name, int? value)
    {
        builder
            .Append(name)
            .Append('=')
            .Append(value?.ToString(CultureInfo.InvariantCulture) ?? "<null>")
            .Append(';');
    }
}
