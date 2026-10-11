using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Csv;

/// <summary>Constructor signatures advertised for SQL source binding; execution is query scoped.</summary>
public sealed class CsvFileDeclaration
{
    public CsvFileDeclaration(SourceExecutionContext context) => ArgumentNullException.ThrowIfNull(context);
    public CsvFileDeclaration(string path, SourceExecutionContext context) : this(context) => ArgumentNullException.ThrowIfNull(path);
    public CsvFileDeclaration(string path, bool hasHeader, SourceExecutionContext context) : this(path, context) { }
    public CsvFileDeclaration(string path, bool hasHeader, int skipRows, SourceExecutionContext context) : this(path, hasHeader, context) { }
    public CsvFileDeclaration(string path, bool hasHeader, int skipRows, string delimiter, SourceExecutionContext context)
        : this(path, hasHeader, skipRows, context) => ArgumentNullException.ThrowIfNull(delimiter);
}
