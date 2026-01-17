namespace Musoq.Evaluator.Tests.Schema.EnvironmentVariable;

public class EnvironmentVariableEntity(string key, string value)
{
    public string Key { get; set; } = key;

    public string Value { get; set; } = value;
}