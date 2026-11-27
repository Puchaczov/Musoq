using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private sealed record PayloadProperty(string Name, string Type)
    {
        internal string Identifier => EscapeCSharpIdentifier(Name);
    }

    private sealed record InterpreterDeclaration(
        string Name,
        string BaseClass,
        string StatusInterface,
        string TypeParameters = "",
        string Constraints = ""
    )
    {
        internal string TypeName => EscapeCSharpIdentifier(Name) + TypeParameters;
        internal bool UsesBinaryInput => StatusInterface == "IBytesInterpreterStatus";

        internal bool HasRuntimeMember(string name) =>
            name
                is "SchemaName"
                    or "ParsePosition"
                    or "BitOffset"
                    or "WriteJson"
                    or "IsFieldComplete"
                    or "__completedFieldNames"
                    or "AttachPartialFieldNames"
            || name == (UsesBinaryInput ? "InterpretAt" : "ParseAt");
    }

    // Keep the authored property names on the result type. When a name is also used by the
    // interpreter, put the reader in a base class so its state and methods retain their bindings.
    private string OpenInterpreterClass(
        StringBuilder builder,
        InterpreterDeclaration declaration,
        IReadOnlyList<PayloadProperty> properties
    )
    {
        var type = declaration.TypeName;
        var implementation = EscapeCSharpIdentifier(declaration.Name);
        var separate =
            declaration.HasRuntimeMember(declaration.Name)
            || properties.Any(field => declaration.HasRuntimeMember(field.Name) || field.Name == declaration.Name);
        var schemaBase = declaration.BaseClass + "<" + type + ">";
        var interfaces =
            $"{declaration.StatusInterface}<{type}>, IParserCompletionLayoutSource, global::Musoq.Plugins.IJsonPayload";

        builder.AppendLine(
            "[global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]"
        );
        if (separate)
        {
            implementation = AvailableInterpreterClassName("__MusoqReader_" + declaration.Name);
            AppendPayloadClass(builder, declaration, properties, implementation);
            builder.AppendLine(
                $"public abstract class {implementation}{declaration.TypeParameters} : {schemaBase}, {interfaces}{declaration.Constraints}"
            );
        }
        else
            builder.AppendLine($"public sealed class {type} : {schemaBase}, {interfaces}{declaration.Constraints}");

        builder.AppendLine("{");
        if (!separate)
            AppendPayloadProperties(builder, properties);

        builder.Append(CompletionLayoutCodeEmitter.Create(properties.Select(static field => field.Name)));
        var reader = separate ? implementation + declaration.TypeParameters : null;
        builder.Append(PartialPayloadCodeEmitter.Members(type, reader));
        builder.Append(
            PartialPayloadCodeEmitter.JsonMembers(
                properties.Select(static field => (field.Name, field.Identifier)),
                separate ? $"(({type})this)" : "this"
            )
        );
        builder.AppendLine($"    public override string SchemaName => \"{declaration.Name}\";");
        return implementation;
    }

    private void AppendPayloadClass(
        StringBuilder builder,
        InterpreterDeclaration declaration,
        IReadOnlyList<PayloadProperty> properties,
        string implementation
    )
    {
        var hasOwnName = properties.Any(field => field.Name == declaration.Name);
        var carrier = hasOwnName ? AvailableInterpreterClassName("__MusoqFields_" + declaration.Name) : implementation;
        builder.AppendLine(
            $"public sealed class {declaration.TypeName} : {carrier}{declaration.TypeParameters}{declaration.Constraints}"
        );
        builder.AppendLine("{");
        builder.Append(ForwardParentContextConstructors(declaration, EscapeCSharpIdentifier(declaration.Name)));
        if (!hasOwnName)
            AppendPayloadProperties(builder, properties);
        builder.AppendLine("}");

        if (!hasOwnName)
            return;
        builder.AppendLine(
            $"public abstract class {carrier}{declaration.TypeParameters} : {implementation}{declaration.TypeParameters}{declaration.Constraints}"
        );
        builder.AppendLine("{");
        builder.Append(ForwardParentContextConstructors(declaration, carrier));
        AppendPayloadProperties(builder, properties);
        builder.AppendLine("}");
    }

    private string AvailableInterpreterClassName(string name)
    {
        while (_registry.TryGetSchema(name, out _))
            name += "_";
        return name;
    }

    private static void AppendPayloadProperties(StringBuilder builder, IReadOnlyList<PayloadProperty> properties)
    {
        foreach (var field in properties)
            builder.AppendLine($"    public {field.Type} {field.Identifier} {{ get; set; }}");
    }
}
