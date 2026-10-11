namespace Musoq.Evaluator.IR.Bindings;

internal static class FunctionDescriptionSchema
{
    public static OutputSchema Create() => new(
    [
        new ColumnSchema("Method", typeof(string), 0),
        new ColumnSchema("Description", typeof(string), 1),
        new ColumnSchema("Category", typeof(string), 2),
        new ColumnSchema("Source", typeof(string), 3)
    ]);
}
