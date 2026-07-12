using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Musoq.Evaluator.IR.Execution;

internal static class ReadModifierMetadata
{
    public static void AppendKey(StringBuilder builder, IReadOnlyDictionary<string, string> readModifiers)
    {
        builder.Append(':').Append(readModifiers.Count);

        foreach (var modifier in Sort(readModifiers))
        {
            AppendKeyPart(builder, modifier.Key);
            builder.Append(':');
            AppendKeyPart(builder, modifier.Value);
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> Sort(IReadOnlyDictionary<string, string> readModifiers)
    {
        return readModifiers.OrderBy(static modifier => modifier.Key, StringComparer.Ordinal);
    }

    private static void AppendKeyPart(StringBuilder builder, string value)
    {
        builder
            .Append(':')
            .Append(value.Length)
            .Append(':')
            .Append(value);
    }
}
