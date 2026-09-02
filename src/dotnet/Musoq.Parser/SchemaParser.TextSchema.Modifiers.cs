using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private void ValidateTextFieldModifiers(IEnumerable<TextFieldDefinitionNode> fields)
    {
        foreach (var field in fields)
        {
            var modifiers = field.Modifiers;
            var hasNested = (modifiers & TextFieldModifier.Nested) != 0;
            var hasEscaped = (modifiers & TextFieldModifier.Escaped) != 0;
            var hasGreedy = (modifiers & TextFieldModifier.Greedy) != 0;
            var hasLazy = (modifiers & TextFieldModifier.Lazy) != 0;

            if (hasNested && field.FieldType != TextFieldType.Between)
                ThrowInvalidModifier(field, "nested", "only applies to between fields");

            if (hasEscaped && field.FieldType != TextFieldType.Between)
                ThrowInvalidModifier(field, "escaped", "only applies to between fields");

            if (hasNested && hasEscaped)
                ThrowInvalidModifier(field, "nested and escaped", "cannot be combined");

            if ((hasGreedy || hasLazy) && field.FieldType is not (TextFieldType.Until or TextFieldType.Pattern))
                ThrowInvalidModifier(field, "greedy/lazy", "only applies to until and pattern fields");

            if (hasGreedy && hasLazy)
                ThrowInvalidModifier(field, "greedy and lazy", "cannot be combined");
        }
    }

    private void ThrowInvalidModifier(TextFieldDefinitionNode field, string modifier, string reason)
    {
        throw new SyntaxException(
            $"Text field '{field.Name}' uses invalid modifier '{modifier}': {reason}.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ4002_InvalidTextSchemaField,
            field.Span);
    }
}
