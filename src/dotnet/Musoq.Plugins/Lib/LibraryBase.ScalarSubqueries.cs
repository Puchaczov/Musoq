using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    private const string ScalarSubqueryAggregateName = "__ScalarSubqueryValue";
    [AggregateFunction(
        typeof(ScalarSubqueryAggregateKernel<string>),
        Name = ScalarSubqueryAggregateName,
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null,
        Visibility = FunctionVisibility.CompilerOnly)]
    public string __ScalarSubqueryValue(string value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<string>();
    [AggregateFunction(
        typeof(ScalarSubqueryAggregateKernel<decimal?>),
        Name = ScalarSubqueryAggregateName,
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null,
        Visibility = FunctionVisibility.CompilerOnly)]
    public decimal? __ScalarSubqueryValue(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();
    [AggregateFunction(
        typeof(ScalarSubqueryAggregateKernel<int?>),
        Name = ScalarSubqueryAggregateName,
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null,
        Visibility = FunctionVisibility.CompilerOnly)]
    public int? __ScalarSubqueryValue(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<int?>();
    [AggregateFunction(
        typeof(ScalarSubqueryAggregateKernel<long?>),
        Name = ScalarSubqueryAggregateName,
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null,
        Visibility = FunctionVisibility.CompilerOnly)]
    public long? __ScalarSubqueryValue(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long?>();
    [AggregateFunction(
        typeof(ScalarSubqueryAggregateKernel<bool?>),
        Name = ScalarSubqueryAggregateName,
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null,
        Visibility = FunctionVisibility.CompilerOnly)]
    public bool? __ScalarSubqueryValue(bool? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<bool?>();
    [AggregateFunction(
        typeof(ScalarSubqueryAggregateKernel<DateTime?>),
        Name = ScalarSubqueryAggregateName,
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null,
        Visibility = FunctionVisibility.CompilerOnly)]
    public DateTime? __ScalarSubqueryValue(DateTime? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<DateTime?>();
    [AggregateFunction(
        typeof(ScalarSubqueryAggregateKernel<object>),
        Name = ScalarSubqueryAggregateName,
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null,
        Visibility = FunctionVisibility.CompilerOnly)]
    public object __ScalarSubqueryValue(object value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<object>();
}
