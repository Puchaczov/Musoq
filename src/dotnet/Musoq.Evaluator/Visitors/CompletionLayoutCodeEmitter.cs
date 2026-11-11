using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Musoq.Evaluator.Visitors;

internal static class CompletionLayoutCodeEmitter
{
    internal static string Create(IEnumerable<string> fields)
    {
        var names = fields.ToArray();
        var name = "__musoqCompletionLayout";
        var used = names.ToHashSet(System.StringComparer.Ordinal);
        while (used.Contains(name))
            name += "_";
        var values = string.Join(", ", names.Select(static field => JsonSerializer.Serialize(field)));
        return $"private static readonly global::Musoq.Schema.Interpreters.ParserCompletionLayout {name} = new(new string[] {{ {values} }});\n" +
            $"global::Musoq.Schema.Interpreters.ParserCompletionLayout global::Musoq.Schema.Interpreters.IParserCompletionLayoutSource.CompletionLayout => {name};\n";
    }
}
