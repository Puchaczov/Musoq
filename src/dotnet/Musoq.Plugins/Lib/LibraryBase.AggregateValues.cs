using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [AggregateFunction(
        typeof(AggregateValuesStringKernel),
        Name = nameof(AggregateValues),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Custom)]
    public string AggregateValues(string? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<string>();
    [AggregateFunction(
        typeof(AggregateValuesStringDelimitedKernel),
        Name = nameof(AggregateValues),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Custom)]
    public string AggregateValues(string? value, string delimiter, [AggregateParent] int parent = 0)
        => AggregateDeclaration<string>();
    [AggregateFunction(
        typeof(AggregateValuesCharKernel),
        Name = nameof(AggregateValues),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Custom)]
    public string AggregateValues(char? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<string>();
    [AggregateFunction(
        typeof(AggregateValuesCharDelimitedKernel),
        Name = nameof(AggregateValues),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Custom)]
    public string AggregateValues(char? value, string delimiter, [AggregateParent] int parent = 0)
        => AggregateDeclaration<string>();
}
