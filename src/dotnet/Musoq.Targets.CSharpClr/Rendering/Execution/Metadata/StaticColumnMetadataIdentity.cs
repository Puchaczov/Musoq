using System.Text;

namespace Musoq.Targets.CSharpClr;

internal static class StaticColumnMetadataIdentity
{
    public static string CreateKey(ExecutionColumnMetadata metadata)
    {
        var builder = new StringBuilder();
        builder
            .Append(metadata.Kind)
            .Append(':')
            .Append(metadata.Fields.Count);

        foreach (var field in metadata.Fields)
        {
            builder
                .Append(':')
                .Append(field.Index)
                .Append(':');
            AppendPart(builder, field.Name);
            builder.Append(':');
            AppendType(builder, field.Type.RequireClrType());
            if (field.EnumType != null || field.SourceReadType != field.Type)
            {
                builder.Append(":source:");
                AppendType(builder, field.SourceReadType.RequireClrType());
                builder.Append(":enum:");
                AppendPart(builder, field.EnumType?.Fingerprint ?? string.Empty);
            }

            ReadModifierMetadata.AppendKey(builder, field.ReadModifiers);
        }

        return builder.ToString();
    }

    private static void AppendType(StringBuilder builder, Type type)
    {
        AppendPart(builder, type.AssemblyQualifiedName ?? type.FullName ?? type.Name);
    }

    private static void AppendPart(StringBuilder builder, string value)
    {
        builder
            .Append(value.Length)
            .Append(':')
            .Append(value);
    }
}
