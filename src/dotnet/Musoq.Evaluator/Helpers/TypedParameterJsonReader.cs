using System.Text.Json;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Evaluator.Helpers;

/// <summary>Reads declared scalar JSON values directly into concrete storage.</summary>
public static class TypedParameterJsonReader
{
    public static char ReadChar(JsonElement value, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var text = value.GetString();
        if (text is { Length: 1 }) return text[0];
        throw ScriptParameterBindingException.TypeMismatch(path, typeof(char), typeof(JsonElement));
    }

    public static void RequireRecord(JsonElement value, StructuralParameterCaptureState state, int depth, string path)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        state.ReserveNode(depth);
        if (value.ValueKind != JsonValueKind.Object)
            throw ScriptParameterBindingException.TypeMismatch(path, typeof(JsonElement), typeof(JsonValueKind));
    }

    public static (bool Found, JsonElement Value) FindField(JsonElement value, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        JsonElement field = default;
        var found = false;
        foreach (var property in value.EnumerateObject())
        {
            if (!string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
            if (found) throw ScriptParameterBindingException.TypeMismatch(name, typeof(JsonElement), typeof(JsonElement));
            found = true;
            field = property.Value;
        }
        return (found, field);
    }

    public static void ValidateFields(JsonElement value, string[] names, string path)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        foreach (var property in value.EnumerateObject())
        {
            var known = false;
            foreach (var name in names)
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) { known = true; break; }
            if (!known) throw ScriptParameterBindingException.Unknown(path + "." + property.Name);
        }
    }
}
