using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Musoq.Targets.Execution;

internal static class TargetArtifactPackageManifestSerializer
{
    public static string Serialize(TargetArtifactPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);

        var builder = new StringBuilder();
        Append(builder, "package-format", package.PackageFormatVersion);
        Append(builder, "execution-ir", package.ExecutionIrVersion);
        Append(builder, "execution-semantics", package.SemanticsContract.Version);
        Append(builder, "execution-semantics-fingerprint", package.SemanticsContract.Fingerprint);
        Append(builder, "host-abi", package.HostAbiVersion);
        Append(builder, "target", package.TargetId.Value);
        Append(builder, "artifact-kind", package.ArtifactKind);
        Append(builder, "executable-kind", package.ExecutableArtifactKind);

        foreach (var pair in package.Metadata.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
            Append(builder, $"metadata:{Encode(pair.Key)}", pair.Value);

        foreach (var file in package.SourceFiles.OrderBy(static file => file.Path, StringComparer.Ordinal))
        {
            Append(
                builder,
                $"source:{Encode(file.Path)}",
                $"language={Encode(file.Language)};sha256={Hash(Encoding.UTF8.GetBytes(file.Content))}");
        }

        foreach (var blob in package.BinaryBlobs.OrderBy(static blob => blob.Name, StringComparer.Ordinal))
        {
            Append(
                builder,
                $"blob:{Encode(blob.Name)}",
                $"content-type={Encode(blob.ContentType)};sha256={Hash(blob.Content)}");
        }

        foreach (var entrypoint in package.Entrypoints
                     .OrderBy(static entrypoint => entrypoint.Name, StringComparer.Ordinal))
        {
            Append(
                builder,
                $"entrypoint:{Encode(entrypoint.Name)}",
                $"kind={entrypoint.Kind};symbol={Encode(entrypoint.SymbolName)}");
        }

        foreach (var service in package.RuntimeServices.Fulfillments
                     .OrderBy(static pair => pair.Key))
        {
            Append(builder, $"runtime-service:{service.Key}", service.Value.ToString());
        }

        foreach (var import in package.HostAbiInventory.Imports)
        {
            var attributes = string.Join(
                ",",
                import.Attributes
                    .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                    .Select(static pair => $"{Encode(pair.Key)}={Encode(pair.Value)}"));
            Append(
                builder,
                $"abi:{import.Kind}:{Encode(import.Name)}",
                $"contract={Encode(import.Contract)};version={import.ContractVersion.ToString(CultureInfo.InvariantCulture)};attributes={attributes}");
        }

        return builder.ToString();
    }

    private static void Append(StringBuilder builder, string key, object value)
    {
        builder.Append(key).Append('=').AppendLine(Convert.ToString(value, CultureInfo.InvariantCulture));
    }

    private static string Encode(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private static string Hash(byte[] content) =>
        Convert.ToHexString(SHA256.HashData(content));
}
