namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Specifies the type of text field capture strategy.
/// </summary>
public enum TextFieldType
{
    /// <summary>
    ///     Pattern matching using regex: pattern 'regex'.
    /// </summary>
    Pattern,

    /// <summary>
    ///     Exact literal matching: literal 'text'.
    /// </summary>
    Literal,

    /// <summary>
    ///     Capture until delimiter: until 'delimiter'.
    /// </summary>
    Until,

    /// <summary>
    ///     Capture between delimiters: between 'start' 'end'.
    /// </summary>
    Between,

    /// <summary>
    ///     Fixed character count: chars[n].
    /// </summary>
    Chars,

    /// <summary>
    ///     Whitespace-delimited token: token.
    /// </summary>
    Token,

    /// <summary>
    ///     Capture remaining content: rest.
    /// </summary>
    Rest,

    /// <summary>
    ///     Match and consume whitespace: whitespace.
    /// </summary>
    Whitespace,

    /// <summary>
    ///     Repeat parsing a schema until delimiter: repeat SchemaName until 'delimiter'.
    ///     PrimaryValue contains the schema name to repeat.
    ///     SecondaryValue contains the until delimiter (null means until end).
    /// </summary>
    Repeat,

    /// <summary>
    ///     Switch-based alternative parsing: switch { pattern 'regex' => TypeName, ... }.
    ///     Uses lookahead pattern matching to determine which type to parse.
    ///     The SwitchCases property on TextFieldDefinitionNode contains the case definitions.
    /// </summary>
    Switch
}
