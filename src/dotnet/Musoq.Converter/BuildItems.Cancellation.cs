using System.Threading;

namespace Musoq.Converter.Build;

public partial class BuildItems
{
    /// <summary>
    /// Gets or sets the cooperative cancellation token for the current compilation.
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
}
