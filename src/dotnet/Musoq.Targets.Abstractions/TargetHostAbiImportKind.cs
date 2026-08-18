namespace Musoq.Targets.Abstractions;

internal enum TargetHostAbiImportKind
{
    SourceAccess = 0,
    PluginInvocation = 1,
    RowShapeTransfer = 2,
    NullTypeCoercion = 3,
    Cancellation = 4,
    Diagnostics = 5,
    Profiling = 6,
    QueryRowSourceAccess = 7
}
