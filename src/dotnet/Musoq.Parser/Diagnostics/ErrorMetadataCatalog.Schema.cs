using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static class SchemaErrorMetadataCatalog
{
    public static IEnumerable<ErrorMetadata> Build()
    {
        yield return Entry(
            DiagnosticCode.MQ4001_InvalidBinarySchemaField,
            "A field in the binary schema definition is invalid.",
            ["Verify field type and constraints match the binary schema grammar."],
            "Binary/Text Spec - Binary Schema Fields");

        yield return Entry(
            DiagnosticCode.MQ4002_InvalidTextSchemaField,
            "A field in the text schema definition is invalid.",
            [
                "Verify the text field name, type, and constraints.",
                "Check delimiter, quote, and optional field settings for the text schema."
            ],
            "Binary/Text Spec - Text Schema Fields");

        yield return Entry(
            DiagnosticCode.MQ4003_UndefinedSchemaReference,
            "The interpretation schema referenced by Interpret, Parse, or InterpretAt was not defined.",
            [
                "Define the schema in a DEFINE SCHEMA block before referencing it.",
                "Verify the schema name matches exactly."
            ],
            "Binary/Text Spec - Schema References");

        yield return Entry(
            DiagnosticCode.MQ4004_CircularSchemaReference,
            "Interpretation schemas reference each other in a cycle.",
            [
                "Break the schema reference cycle.",
                "Use a flat field or a separately parsed schema boundary where recursion is not supported."
            ],
            "Binary/Text Spec - Schema References");

        yield return Entry(
            DiagnosticCode.MQ4005_InvalidEndianness,
            "A binary schema field declares an unsupported byte order.",
            [
                "Use a supported endianness value.",
                "Remove the endianness modifier when the field type does not need it."
            ],
            "Binary/Text Spec - Binary Schema Fields");

        yield return Entry(
            DiagnosticCode.MQ4006_InvalidFieldConstraint,
            "A schema field constraint is malformed or incompatible with the field type.",
            [
                "Check the constraint name and value.",
                "Apply constraints only to field types that support them."
            ],
            "Binary/Text Spec - Field Constraints");

        yield return Entry(
            DiagnosticCode.MQ4007_InvalidSchemaFieldType,
            "A schema field uses a type that is not supported by the interpretation schema parser.",
            [
                "Use one of the supported binary or text schema field types.",
                "Replace custom CLR type names with schema-supported primitive type names."
            ],
            "Binary/Text Spec - Schema Field Types");

        yield return Entry(
            DiagnosticCode.MQ4008_DuplicateSchemaField,
            "A schema declares the same field name more than once.",
            [
                "Rename one of the fields.",
                "Remove the duplicate field declaration."
            ],
            "Binary/Text Spec - Schema Definitions");

        yield return Entry(
            DiagnosticCode.MQ4009_InvalidSchemaName,
            "The schema name is missing or uses invalid identifier syntax.",
            [
                "Use a valid schema identifier.",
                "Check for punctuation or whitespace in the schema name."
            ],
            "Binary/Text Spec - Schema Definitions");

        yield return Entry(
            DiagnosticCode.MQ4010_MissingRequiredField,
            "A schema field or schema declaration is missing a required value.",
            [
                "Add the required field property.",
                "Check the schema grammar for required name, type, offset, length, or delimiter values."
            ],
            "Binary/Text Spec - Schema Definitions");

        yield return Entry(
            DiagnosticCode.MQ4011_SwitchSelectorNotPreviousField,
            "A binary switch selector does not reference a field declared before the switch field.",
            [
                "Reference a field that appears earlier in the same schema.",
                "Move the discriminator field above the switch field."
            ],
            "Binary/Text Spec - Switch Payloads");

        yield return Entry(
            DiagnosticCode.MQ4012_DuplicateSwitchBranchAlias,
            "A binary switch declares the same branch alias more than once.",
            [
                "Rename one of the branch aliases.",
                "Each switch branch alias must be unique within the switch."
            ],
            "Binary/Text Spec - Switch Payloads");

        yield return Entry(
            DiagnosticCode.MQ4013_InvalidSwitchCaseLabel,
            "A binary switch case label is not a constant scalar literal.",
            [
                "Use a constant integer, hex, binary, octal, or string literal as the case label.",
                "Use '_' for the optional default branch."
            ],
            "Binary/Text Spec - Switch Payloads");

        yield return Entry(
            DiagnosticCode.MQ4014_InvalidSubstreamModifier,
            "A binary substream is missing a 'raw' or 'as <type>' modifier or declares an invalid mode.",
            [
                "Use 'substream[size] raw' to return the bounded slice as bytes.",
                "Use 'substream[size] as <type>' with an optional 'exact' or 'lax' mode."
            ],
            "Binary/Text Spec - Substreams");

        yield return Entry(
            DiagnosticCode.MQ4015_InvalidSubstreamTarget,
            "A binary substream declares an invalid or missing target type after 'as'.",
            [
                "Provide a schema reference, inline schema, array, repeat-until, string, primitive, or switch target.",
                "Remove the trailing 'as' when no target type is intended and use 'raw' instead."
            ],
            "Binary/Text Spec - Substreams");
    }
}
