namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Specifies the kind of value validation applied to a binary schema field.
/// </summary>
public enum FieldValueValidationKind
{
    /// <summary>
    ///     The field must equal a single expected value (or byte sequence).
    /// </summary>
    Const,

    /// <summary>
    ///     Signature-oriented alias of <see cref="Const" />; the field must equal a
    ///     single expected value (or byte sequence).
    /// </summary>
    Magic,

    /// <summary>
    ///     The field value must be one of an explicit set of allowed values.
    /// </summary>
    OneOf
}
