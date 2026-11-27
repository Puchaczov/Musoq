using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Musoq.Evaluator;

internal static partial class InterpretationCSharpNames
{
    private static readonly FrozenSet<string> CSharpKeywords = new[]
    {
        "__arglist",
        "__makeref",
        "__reftype",
        "__refvalue",
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "virtual",
        "void",
        "volatile",
        "while",
    }.ToFrozenSet(StringComparer.Ordinal);

    internal const string Namespace = "Musoq.Generated.Interpreters";

    internal static string Identifier(string name) => CSharpKeywords.Contains(name) ? "@" + name : name;

    internal static string Qualified(string name) => $"{Namespace}.{Identifier(name)}";

    // Components are authored identifiers in a schema reference or field path.
    // Preserve generic arguments, numeric subscripts and conditional access separators.
    internal static string Reference(string name) =>
        name.AsSpan().IndexOfAny(".<[".AsSpan()) < 0
            ? Identifier(name)
            : IdentifierParts().Replace(name, static match => Identifier(match.Value));

    [GeneratedRegex("""(?:"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'|(?<![@\w])[_\p{L}]\w*)""", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierParts();
}
