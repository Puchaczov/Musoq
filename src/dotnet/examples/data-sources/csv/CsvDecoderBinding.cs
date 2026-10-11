using System.Reflection;

namespace Musoq.Examples.DataSources.Csv;

/// <summary>Closes decoder metadata during source description, before compiled execution.</summary>
internal static class CsvDecoderBinding
{
    private static readonly Type DecoderDefinition = typeof(CsvValueReader)
        .GetNestedType("Decoder`1", BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("CSV decoder metadata is unavailable.");

    internal static void Prepare(Type type)
    {
        var decoder = DecoderDefinition.MakeGenericType(type);
        var field = decoder.GetField("Read", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"CSV decoder metadata is unavailable for '{type}'.");
        _ = field.GetValue(null);
    }
}
