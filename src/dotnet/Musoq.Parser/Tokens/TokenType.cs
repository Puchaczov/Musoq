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
    ParameterReference,
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
    Pivot,
    Unpivot,
    Like,
    NotLike,
    RLike,
    NotRLike,
    As,
    Is,
    Null,
    Present,
    Missing,
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
    AsOfJoin,
    SemiJoin,
    AntiJoin,
    CrossJoin,
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
    Exists,
    Any,
    Some,
    All,
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
    DoubleColon,

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
    Substream,
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

    // Null coalescing and question mark for optional quantifier
    NullCoalescing, // ??
    QuestionMark, // ?

    // Star expression modifiers
    Exclude, // exclude (context-sensitive keyword after *)
    Replace, // replace (context-sensitive keyword after *)

    // Window function tokens
    Over, // OVER keyword after function call
    PartitionBy, // PARTITION BY (multi-word keyword)
    Window, // WINDOW clause keyword
    Qualify, // QUALIFY clause keyword (post-window filter)

    // Window frame tokens
    Rows, // ROWS frame type
    Range, // RANGE frame type
    Unbounded, // UNBOUNDED keyword
    Preceding, // PRECEDING keyword
    Following, // FOLLOWING keyword
    CurrentRow, // CURRENT ROW (multi-word keyword)

    // Error token for recovery mode
    Error // Invalid/unrecognized token
}
