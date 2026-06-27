namespace Musoq.Plugins;

/// <summary>
///     Provides typed extra arguments for a plugin window function.
/// </summary>
public interface IWindowFunctionArguments<in T1, in T2>
{
    /// <summary>
    ///     Sets typed extra arguments before partition processing begins.
    /// </summary>
    void SetArguments(T1 arg1, T2 arg2);
}
