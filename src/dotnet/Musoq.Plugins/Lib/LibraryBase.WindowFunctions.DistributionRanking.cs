using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Creates a window function that reports a row's relative rank within its partition.
    /// </summary>
    [WindowFunction(Name = "PercentRank")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, double> WindowPercentRank()
    {
        return new PercentRankWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that reports the fraction of partition rows at or before the current peer group.
    /// </summary>
    [WindowFunction(Name = "CumeDist")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, double> WindowCumeDist()
    {
        return new CumeDistWindowFunction();
    }
}
