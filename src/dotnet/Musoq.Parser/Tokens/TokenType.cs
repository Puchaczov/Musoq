namespace Musoq.Parser.Tokens;

public enum TokenType : short
{
    Word,
    Decimal,
    LeftParenthesis,
    RightParenthesis,
    None,
    EndOfFile,
    Diff,
    And,
    Or,
    Not,
    Where,
    Plus,
    AliasedStar,
    Star,
    FSlash,
    Hyphen,
    Mod,
    Comma,
    WhiteSpace,
    Equality,
    Identifier,
    NumericColumn,
    Function,
    Property,
    VarArg,
    Greater,
    GreaterEqual,
    Less,
    LessEqual,
    Select,
    From,
    Like,
    NotLike,
    RLike,
    NotRLike,
    As,
    Is,
    Null,
    Union,
    UnionAll,
    Except,
    Intersect,
    Dot,
    GroupBy,
    Having,
    Integer,
    HexadecimalInteger,
    BinaryInteger,
    OctalInteger,
    KeyAccess,
    NumericAccess,
    MethodAccess,
    AllColumns,
    Contains,
    Skip,
    Take,
    With,
    InnerJoin,
    OuterJoin,
    CrossApply,
    OuterApply,
    On,
    OrderBy,
    Asc,
    Desc,
    Functions,
    True,
    False,
    In,
    NotIn,
    Table,
    LBracket, // { (left curly brace)
    RBracket, // } (right curly brace)
    Semicolon,
    Couple,
    Case,
    When,
    Then,
    Else,
    End,
    FieldLink,
    Comment,
    Distinct,
    ColumnKeyword,

    // Additional syntax tokens for schema definitions
    LeftSquareBracket, // [
    RightSquareBracket, // ]
    StringLiteral, // 'string literal'

    // Interpretation Schema tokens - Binary schema keywords
    Binary,
    Text,

    // Endianness
    LittleEndian, // le
    BigEndian, // be

    // Primitive types (for schema field types)
    ByteType, // byte
    SByteType, // sbyte
    ShortType, // short
    UShortType, // ushort
    IntType, // int
    UIntType, // uint
    LongType, // long
    ULongType, // ulong
    FloatType, // float
    DoubleType, // double

    // Array and bit types
    BitsType, // bits
    Align, // align
    StringType, // string (for schema context)

    // Encodings
    Utf8,
    Utf16Le,
    Utf16Be,
    Ascii,
    Latin1,
    Ebcdic,

    // Field modifiers
    Trim,
    RTrim,
    LTrim,
    NullTerm,
    Check,
    At,

    // Colon separator for field definitions
    Colon,

    // Text schema keywords (placeholders for future sessions)
    Pattern,
    Literal,
    Until,
    Between,
    Chars,
    Token,
    Rest,
    Whitespace,
    Optional,
    Repeat,
    Switch,
    Nested,
    Escaped,
    Greedy,
    Lazy,
    Lower,
    Upper,
    Capture,

    // Schema inheritance
    Extends,

    // Bitwise operators
    Ampersand, // & (bitwise AND)
    Pipe, // | (bitwise OR)
    Caret, // ^ (bitwise XOR)
    LeftShift, // <<
    RightShift, // >>

    // Fat arrow for switch expressions
    FatArrow, // =>

    // Underscore for default case
    Underscore, // _

    // Question mark for optional quantifier
    QuestionMark, // ?

    // Error token for recovery mode
    Error // Invalid/unrecognized token
}
