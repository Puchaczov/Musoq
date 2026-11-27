using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator;

internal sealed class TextResultTypeNames
{
    internal const string NamespacePrefix = InterpretationCSharpNames.Namespace + ".";
    internal const string CapturePrefix = "CaptureResult_";
    internal const string SwitchPrefix = "TextSwitch_";

    private readonly Dictionary<TextFieldDefinitionNode, string> _names = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<string, TextFieldDefinitionNode> _fields = new(StringComparer.Ordinal);
    private readonly string _owner;

    internal TextResultTypeNames(string owner, IReadOnlyList<TextFieldDefinitionNode> fields)
    {
        _owner = InterpretationCSharpNames.Qualified(owner);
        var used = fields.Select(field => field.Name).Append(owner).ToHashSet(StringComparer.Ordinal);
        foreach (var field in InterpretationTextFieldTypeResolver.EnumerateDefinitions(fields))
        {
            if (
                (field.CaptureGroups.Length == 0 && field.FieldType != TextFieldType.Switch)
                || _names.ContainsKey(field)
            )
                continue;

            var name = (field.FieldType == TextFieldType.Switch ? SwitchPrefix : CapturePrefix) + field.Name;
            while (used.Contains(name) || field.CaptureGroups.Contains(name, StringComparer.Ordinal))
                name += "_";
            used.Add(name);
            _names.Add(field, name);
            _fields.Add(name, field);
        }
    }

    internal string Name(TextFieldDefinitionNode field) => _names[field];

    internal string Qualified(TextFieldDefinitionNode field) => $"{_owner}.{Name(field)}";

    internal TextFieldDefinitionNode? Find(string name) => _fields.GetValueOrDefault(name);

    internal static int NestedTypeOffset(string? name, string prefix)
    {
        if (name == null || !name.StartsWith(NamespacePrefix, StringComparison.Ordinal))
            return -1;
        var separator = name.IndexOf('.', NamespacePrefix.Length);
        if (separator < 0 || !name.AsSpan(separator + 1).StartsWith(prefix, StringComparison.Ordinal))
            return -1;
        return separator + 1;
    }
}
