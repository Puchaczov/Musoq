using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private SyntaxException BinarySchemaDiagnostic(string message, DiagnosticCode code, TextSpan span) =>
        new(message, _lexer.AlreadyResolvedQueryPart, code, span);

    private SyntaxException InvalidBinarySchemaField(string message, TextSpan span) =>
        BinarySchemaDiagnostic(message, DiagnosticCode.MQ4001_InvalidBinarySchemaField, span);

    private SyntaxException InvalidBinarySchemaEndianness(string message, TextSpan span) =>
        BinarySchemaDiagnostic(message, DiagnosticCode.MQ4005_InvalidEndianness, span);
}
