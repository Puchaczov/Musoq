namespace Musoq.Converter.Diagnostics;

public sealed record DiagnosticSqlCommand(
    DiagnosticSqlCommandKind Kind,
    string InnerScript);
