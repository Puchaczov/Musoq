namespace Musoq.Schema.Diagnostics;

/// <summary>Stable, provider-independent reasons for rejecting datasource input.</summary>
public enum DataSourceInputFailureReason
{
    MalformedInput = 0,
    UnterminatedQuotedField = 1,
    MalformedJson = 2
}
