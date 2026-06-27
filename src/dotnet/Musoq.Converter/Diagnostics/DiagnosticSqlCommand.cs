namespace Musoq.Converter.Diagnostics;

internal sealed record DiagnosticSqlCommand(
    DiagnosticSqlCommandKind Kind,
    string InnerScript);
