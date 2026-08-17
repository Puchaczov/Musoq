using System.Collections.Generic;

namespace Musoq.Converter.Build;

internal static class TargetRenderOptionsFactory
{
    public static TargetRenderOptions Create(bool enableContextualExecution)
    {
        return enableContextualExecution
            ? new TargetRenderOptions(new Dictionary<string, string>
            {
                ["EnableContextualExecution"] = "true"
            })
            : TargetRenderOptions.Empty;
    }
}
