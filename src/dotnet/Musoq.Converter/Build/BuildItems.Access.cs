using System.Collections.Generic;

namespace Musoq.Converter.Build;

public partial class BuildItems
{
    private BuildArtifactStore Artifacts => new(this);

    private T GetRequired<T>(string key) => Artifacts.GetRequired(new BuildArtifactSlot<T>(key));

    private bool TryGetArtifact<T>(string key, out T value) => Artifacts.TryGet(new BuildArtifactSlot<T>(key), out value);

    private T? GetOptional<T>(string key)
        where T : class
        => Artifacts.GetOptional(new BuildArtifactSlot<T>(key));

    private void SetRequired<T>(string key, T value)
        where T : notnull
        => Artifacts.SetRequired(new BuildArtifactSlot<T>(key), value);

    private void SetOptional<T>(string key, T? value)
        where T : class
        => Artifacts.SetOptional(new BuildArtifactSlot<T>(key), value);

    private bool ContainsArtifact<T>(string key) => Artifacts.Contains(new BuildArtifactSlot<T>(key));

    private bool GetFlag(string key, bool defaultWhenMissing) => Artifacts.GetFlag(new BuildArtifactSlot<bool>(key), defaultWhenMissing);

    private void SetFlag(string key, bool value) => Artifacts.SetFlag(new BuildArtifactSlot<bool>(key), value);

    private T GetValueOrDefault<T>(string key, T defaultWhenMissing)
        where T : struct
        => Artifacts.GetValueOrDefault(new BuildArtifactSlot<T>(key), defaultWhenMissing);

    private IReadOnlyList<T> GetListOrEmpty<T>(string key) => Artifacts.GetListOrEmpty(new BuildArtifactSlot<IReadOnlyList<T>>(key));
}
