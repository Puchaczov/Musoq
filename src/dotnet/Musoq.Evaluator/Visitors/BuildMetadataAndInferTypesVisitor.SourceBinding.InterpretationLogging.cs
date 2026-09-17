using Microsoft.Extensions.Logging;
using Musoq.Schema;
using AliasedFromNode = Musoq.Parser.Nodes.From.AliasedFromNode;
namespace Musoq.Evaluator.Visitors;
public partial class BuildMetadataAndInferTypesVisitor
{
    private static readonly Action<ILogger, string, string, string, Exception?> InterpretFunctionProcessingLog =
        LoggerMessage.Define<string, string, string>(LogLevel.Debug, new EventId(1001, nameof(LogInterpretFunctionProcessing)),
            "Visit(AliasedFromNode): Processing Interpret function '{Identifier}' with alias '{Alias}' -> _queryAlias='{QueryAlias}'");
    private static readonly Action<ILogger, string, int, string, Exception?> InterpretTableRegistrationLog =
        LoggerMessage.Define<string, int, string>(LogLevel.Debug, new EventId(1002, nameof(LogInterpretTableRegistration)),
            "Visit(AliasedFromNode): Registered TableSymbol '{QueryAlias}' with {ColumnCount} columns in scope '{ScopeName}'");
    private static void LogInterpretFunctionProcessing(ILogger? logger, AliasedFromNode node, string queryAlias) {
        if (logger?.IsEnabled(LogLevel.Debug) == true)
            InterpretFunctionProcessingLog(logger, node.Identifier, node.Alias, queryAlias, null);
    }
    private static void LogInterpretTableRegistration(ILogger? logger, string queryAlias, ISchemaTable? interpretTable, string scopeName) {
        if (logger?.IsEnabled(LogLevel.Debug) != true) return;
        InterpretTableRegistrationLog(logger, queryAlias, interpretTable?.Columns?.Length ?? 0, scopeName, null);
    }
}
