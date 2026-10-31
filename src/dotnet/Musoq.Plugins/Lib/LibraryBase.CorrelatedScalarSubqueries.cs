using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
#pragma warning disable CS1591

    private const string CorrelatedScalarSubqueryAggregateName = "__CorrelatedScalarSubqueryValue";

    [AggregateFunction(
        typeof(CorrelatedScalarSubqueryAggregateKernel<string>),
        Name = CorrelatedScalarSubqueryAggregateName,
        Inline = false,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Custom,
        Visibility = FunctionVisibility.CompilerOnly)]
    public CorrelatedScalarSubqueryResult<string> __CorrelatedScalarSubqueryValue(string value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<CorrelatedScalarSubqueryResult<string>>();

    [AggregateFunction(
        typeof(CorrelatedScalarSubqueryAggregateKernel<decimal?>),
        Name = CorrelatedScalarSubqueryAggregateName,
        Inline = false,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Custom,
        Visibility = FunctionVisibility.CompilerOnly)]
    public CorrelatedScalarSubqueryResult<decimal?> __CorrelatedScalarSubqueryValue(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<CorrelatedScalarSubqueryResult<decimal?>>();

    [AggregateFunction(
        typeof(CorrelatedScalarSubqueryAggregateKernel<int?>),
        Name = CorrelatedScalarSubqueryAggregateName,
        Inline = false,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Custom,
        Visibility = FunctionVisibility.CompilerOnly)]
    public CorrelatedScalarSubqueryResult<int?> __CorrelatedScalarSubqueryValue(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<CorrelatedScalarSubqueryResult<int?>>();

    [AggregateFunction(
        typeof(CorrelatedScalarSubqueryAggregateKernel<long?>),
        Name = CorrelatedScalarSubqueryAggregateName,
        Inline = false,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Custom,
        Visibility = FunctionVisibility.CompilerOnly)]
    public CorrelatedScalarSubqueryResult<long?> __CorrelatedScalarSubqueryValue(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<CorrelatedScalarSubqueryResult<long?>>();

    [AggregateFunction(
        typeof(CorrelatedScalarSubqueryAggregateKernel<bool?>),
        Name = CorrelatedScalarSubqueryAggregateName,
        Inline = false,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Custom,
        Visibility = FunctionVisibility.CompilerOnly)]
    public CorrelatedScalarSubqueryResult<bool?> __CorrelatedScalarSubqueryValue(bool? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<CorrelatedScalarSubqueryResult<bool?>>();

    [AggregateFunction(
        typeof(CorrelatedScalarSubqueryAggregateKernel<DateTime?>),
        Name = CorrelatedScalarSubqueryAggregateName,
        Inline = false,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Custom,
        Visibility = FunctionVisibility.CompilerOnly)]
    public CorrelatedScalarSubqueryResult<DateTime?> __CorrelatedScalarSubqueryValue(DateTime? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<CorrelatedScalarSubqueryResult<DateTime?>>();

    [AggregateFunction(
        typeof(CorrelatedScalarSubqueryAggregateKernel<object>),
        Name = CorrelatedScalarSubqueryAggregateName,
        Inline = false,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Custom,
        Visibility = FunctionVisibility.CompilerOnly)]
    public CorrelatedScalarSubqueryResult<object> __CorrelatedScalarSubqueryValue(object value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<CorrelatedScalarSubqueryResult<object>>();

    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public string __CorrelatedScalarSubqueryResult(CorrelatedScalarSubqueryResult<string> result)
        => ExtractCorrelatedScalarSubqueryResult(result);

    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public decimal? __CorrelatedScalarSubqueryResult(CorrelatedScalarSubqueryResult<decimal?> result)
        => ExtractCorrelatedScalarSubqueryResult(result);

    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public int? __CorrelatedScalarSubqueryResult(CorrelatedScalarSubqueryResult<int?> result)
        => ExtractCorrelatedScalarSubqueryResult(result);

    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public long? __CorrelatedScalarSubqueryResult(CorrelatedScalarSubqueryResult<long?> result)
        => ExtractCorrelatedScalarSubqueryResult(result);

    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public bool? __CorrelatedScalarSubqueryResult(CorrelatedScalarSubqueryResult<bool?> result)
        => ExtractCorrelatedScalarSubqueryResult(result);

    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public DateTime? __CorrelatedScalarSubqueryResult(CorrelatedScalarSubqueryResult<DateTime?> result)
        => ExtractCorrelatedScalarSubqueryResult(result);

    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public object __CorrelatedScalarSubqueryResult(CorrelatedScalarSubqueryResult<object> result)
        => ExtractCorrelatedScalarSubqueryResult(result);

    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public string __CorrelatedScalarSubqueryResult(CorrelatedScalarSubqueryResult<string>? result)
        => ExtractCorrelatedScalarSubqueryResult(result);

    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public decimal? __CorrelatedScalarSubqueryResult(CorrelatedScalarSubqueryResult<decimal?>? result)
        => ExtractCorrelatedScalarSubqueryResult(result);

    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public int? __CorrelatedScalarSubqueryResult(CorrelatedScalarSubqueryResult<int?>? result)
        => ExtractCorrelatedScalarSubqueryResult(result);

    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public long? __CorrelatedScalarSubqueryResult(CorrelatedScalarSubqueryResult<long?>? result)
        => ExtractCorrelatedScalarSubqueryResult(result);

    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public bool? __CorrelatedScalarSubqueryResult(CorrelatedScalarSubqueryResult<bool?>? result)
        => ExtractCorrelatedScalarSubqueryResult(result);

    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public DateTime? __CorrelatedScalarSubqueryResult(CorrelatedScalarSubqueryResult<DateTime?>? result)
        => ExtractCorrelatedScalarSubqueryResult(result);

    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public object __CorrelatedScalarSubqueryResult(CorrelatedScalarSubqueryResult<object>? result)
        => ExtractCorrelatedScalarSubqueryResult(result);

    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public string __CorrelatedScalarSubqueryResult(string value) => value;
    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public decimal? __CorrelatedScalarSubqueryResult(decimal? value) => value;
    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public int? __CorrelatedScalarSubqueryResult(int? value) => value;
    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public long? __CorrelatedScalarSubqueryResult(long? value) => value;
    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public bool? __CorrelatedScalarSubqueryResult(bool? value) => value;
    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public DateTime? __CorrelatedScalarSubqueryResult(DateTime? value) => value;
    [BindableMethod(Visibility = FunctionVisibility.CompilerOnly)]
    public object __CorrelatedScalarSubqueryResult(object value) => value;

    private static T ExtractCorrelatedScalarSubqueryResult<T>(CorrelatedScalarSubqueryResult<T> result)
    {
        return CorrelatedScalarSubqueryResultExtractor.GetValue<T>(result);
    }

    private static T ExtractCorrelatedScalarSubqueryResult<T>(CorrelatedScalarSubqueryResult<T>? result)
    {
        return CorrelatedScalarSubqueryResultExtractor.GetValue(result);
    }

#pragma warning restore CS1591
}
