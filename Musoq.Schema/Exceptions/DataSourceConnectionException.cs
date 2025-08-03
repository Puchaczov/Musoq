using System;

namespace Musoq.Schema.Exceptions;

/// <summary>
/// Exception thrown when data source connection or initialization fails.
/// Provides specific guidance for different types of connection issues.
/// </summary>
public class DataSourceConnectionException : InvalidOperationException
{
    public string DataSourceName { get; }
    public string ConnectionString { get; }
    public string DataSourceType { get; }

    public DataSourceConnectionException(string dataSourceName, string dataSourceType, string message, string connectionString = null)
        : base(message)
    {
        DataSourceName = dataSourceName ?? string.Empty;
        DataSourceType = dataSourceType ?? string.Empty;
        ConnectionString = connectionString ?? string.Empty;
    }

    public DataSourceConnectionException(string dataSourceName, string dataSourceType, string message, Exception innerException, string connectionString = null)
        : base(message, innerException)
    {
        DataSourceName = dataSourceName ?? string.Empty;
        DataSourceType = dataSourceType ?? string.Empty;
        ConnectionString = connectionString ?? string.Empty;
    }

    public static DataSourceConnectionException ForConnectionFailure(string dataSourceName, string dataSourceType, Exception innerException, string connectionString = null)
    {
        var message = $"Failed to connect to data source '{dataSourceName}' of type '{dataSourceType}'. " +
                     GetConnectionAdvice(dataSourceType) +
                     "\n\nPlease check:\n" +
                     "- Network connectivity\n" +
                     "- Connection parameters\n" +
                     "- Authentication credentials\n" +
                     "- Data source availability";

        return new DataSourceConnectionException(dataSourceName, dataSourceType, message, innerException, connectionString);
    }

    public static DataSourceConnectionException ForInitializationFailure(string dataSourceName, string dataSourceType, Exception innerException)
    {
        var message = $"Failed to initialize data source '{dataSourceName}' of type '{dataSourceType}'. " +
                     "This usually indicates a configuration or compatibility issue. " +
                     "\n\nPlease check:\n" +
                     "- Data source plugin installation\n" +
                     "- Configuration parameters\n" +
                     "- Required dependencies\n" +
                     "- System permissions";

        return new DataSourceConnectionException(dataSourceName, dataSourceType, message, innerException);
    }

    public static DataSourceConnectionException ForInvalidParameters(string dataSourceName, string dataSourceType, string parameterIssue)
    {
        var message = $"Invalid parameters for data source '{dataSourceName}' of type '{dataSourceType}': {parameterIssue}. " +
                     GetParameterAdvice(dataSourceType) +
                     "\n\nPlease refer to the documentation for correct parameter format and values.";

        return new DataSourceConnectionException(dataSourceName, dataSourceType, message);
    }

    public static DataSourceConnectionException ForTimeout(string dataSourceName, string dataSourceType, TimeSpan timeout)
    {
        var message = $"Connection to data source '{dataSourceName}' of type '{dataSourceType}' timed out after {timeout.TotalSeconds} seconds. " +
                     "This may indicate network issues or high load on the data source. " +
                     "\n\nPlease try:\n" +
                     "- Increasing the timeout value\n" +
                     "- Checking network connectivity\n" +
                     "- Verifying data source performance\n" +
                     "- Using more specific queries to reduce load";

        return new DataSourceConnectionException(dataSourceName, dataSourceType, message);
    }

    private static string GetConnectionAdvice(string dataSourceType)
    {
        return dataSourceType?.ToLowerInvariant() switch
        {
            "file" or "os" => "For file-based sources, ensure the file path exists and is accessible.",
            "git" => "For Git repositories, ensure the repository path is valid and accessible.",
            "database" or "sql" => "For database connections, verify the connection string and database availability.",
            "web" or "http" or "api" => "For web-based sources, check URL accessibility and network connectivity.",
            _ => "Please verify the data source configuration and accessibility."
        };
    }

    private static string GetParameterAdvice(string dataSourceType)
    {
        return dataSourceType?.ToLowerInvariant() switch
        {
            "file" or "os" => "File sources typically require a valid file path.",
            "git" => "Git sources require a valid repository path or URL.",
            "database" or "sql" => "Database sources require a valid connection string.",
            "web" or "http" or "api" => "Web sources require a valid URL and may need authentication.",
            _ => "Check the parameter format required for this data source type."
        };
    }
}