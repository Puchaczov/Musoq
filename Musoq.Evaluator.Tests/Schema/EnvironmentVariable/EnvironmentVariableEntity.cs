namespace Musoq.Evaluator.Tests.Schema.EnvironmentVariable;

public class EnvironmentVariableEntity
{
    public EnvironmentVariableEntity(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; set; }
        
    public string Value { get; set; }
}