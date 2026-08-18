using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>
/// Provides the deterministic identifiers shared by the execution IR and the
/// C# renderer for query-scoped row carriers. The fingerprint is already a
/// validated SHA-256 digest, so a short prefix is sufficient to keep generated
/// identifiers readable while remaining stable for a given logical shape.
/// </summary>
internal static class QueryRowSourceNaming
{
    private const int FingerprintPrefixLength = 12;

    public static string CreateCarrierTypeName(
        string shapeFingerprint,
        SourceQueryRowCarrier carrier)
    {
        return $"QueryRow_{GetFingerprintPrefix(shapeFingerprint)}_{GetCarrierSuffix(carrier)}";
    }

    public static string CreateCarrierTypeName(
        string shapeFingerprint,
        ExecutionQueryRowCarrier carrier)
    {
        return $"QueryRow_{GetFingerprintPrefix(shapeFingerprint)}_{GetCarrierSuffix(carrier)}";
    }

    public static string CreateMaterializerTypeName(
        string shapeFingerprint,
        ExecutionQueryRowCarrier carrier)
    {
        return $"QueryRowMaterializer_{GetFingerprintPrefix(shapeFingerprint)}_{GetCarrierSuffix(carrier)}";
    }

    public static string CreateShapeFieldName(string shapeFingerprint)
    {
        return $"__queryRowShape_{GetFingerprintPrefix(shapeFingerprint)}";
    }

    public static string CreateFieldName(int slot)
    {
        if (slot < 0)
            throw new ArgumentOutOfRangeException(nameof(slot));

        return $"Field{slot}";
    }

    private static string GetFingerprintPrefix(string shapeFingerprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeFingerprint);
        if (shapeFingerprint.Length < FingerprintPrefixLength)
            throw new ArgumentException("A query-row fingerprint is shorter than the required identifier prefix.", nameof(shapeFingerprint));

        return shapeFingerprint[..FingerprintPrefixLength].ToUpperInvariant();
    }

    private static string GetCarrierSuffix(SourceQueryRowCarrier carrier)
    {
        return carrier switch
        {
            SourceQueryRowCarrier.ReadonlyStruct => "S",
            SourceQueryRowCarrier.SealedClass => "C",
            _ => throw new ArgumentOutOfRangeException(nameof(carrier), carrier, "Unknown query-row carrier.")
        };
    }

    private static string GetCarrierSuffix(ExecutionQueryRowCarrier carrier)
    {
        return carrier switch
        {
            ExecutionQueryRowCarrier.ReadonlyStruct => "S",
            ExecutionQueryRowCarrier.SealedClass => "C",
            _ => throw new ArgumentOutOfRangeException(nameof(carrier), carrier, "Unknown query-row carrier.")
        };
    }
}
