using Microsoft.CodeAnalysis.CSharp;
using Musoq.Targets.CSharpClr;

namespace Musoq.Converter.Build;

public partial class BuildItems
{
    internal RenderedQueryArtifact RenderingArtifact
    {
        get
        {
            if (TryGetArtifact<RenderedQueryArtifact>(BuildItemKeys.RenderingArtifact, out var artifact))
                return artifact;

            if (TryGetArtifact<CSharpCompilation>(BuildItemKeys.Compilation, out var compilation) &&
                TryGetArtifact<string>(BuildItemKeys.AccessToClassPath, out var accessToClassPath))
            {
                artifact = CSharpClrArtifactCompatibility.CreateRenderedArtifact(compilation, accessToClassPath);
                SetRequired(BuildItemKeys.RenderingArtifact, artifact);
                return artifact;
            }

            return GetRequired<RenderedQueryArtifact>(BuildItemKeys.RenderingArtifact);
        }
        set => SetRenderingArtifact(value);
    }

    public CSharpCompilation Compilation
    {
        get => GetRequired<CSharpCompilation>(BuildItemKeys.Compilation);
        set
        {
            SetRequired(BuildItemKeys.Compilation, value);
            TryRefreshCSharpRenderingArtifact();
        }
    }

    public string AccessToClassPath
    {
        get => GetRequired<string>(BuildItemKeys.AccessToClassPath);
        set
        {
            SetRequired(BuildItemKeys.AccessToClassPath, value);
            TryRefreshCSharpRenderingArtifact();
        }
    }

    private void SetRenderingArtifact(RenderedQueryArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        SetRequired(BuildItemKeys.RenderingArtifact, artifact);

        if (CSharpClrArtifactCompatibility.TryGetRenderedArtifact(artifact, out var csharpArtifact))
        {
            SetRequired(BuildItemKeys.Compilation, csharpArtifact.Compilation);
            SetRequired(BuildItemKeys.AccessToClassPath, csharpArtifact.AccessToClassPath);
            SetRequired(
                BuildItemKeys.QueryMethodRenderMetadata,
                CSharpClrArtifactCompatibility.GetQueryMethodRenderMetadata(artifact));
        }
    }

    private void TryRefreshCSharpRenderingArtifact()
    {
        if (TryGetArtifact<CSharpCompilation>(BuildItemKeys.Compilation, out var compilation) &&
            TryGetArtifact<string>(BuildItemKeys.AccessToClassPath, out var accessToClassPath))
        {
            SetRequired(
                BuildItemKeys.RenderingArtifact,
                CSharpClrArtifactCompatibility.CreateRenderedArtifact(compilation, accessToClassPath));
        }
    }
}
