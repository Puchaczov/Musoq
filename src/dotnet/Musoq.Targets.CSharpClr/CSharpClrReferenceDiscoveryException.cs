using System;
using System.IO;
using System.Reflection;

namespace Musoq.Targets.CSharpClr;

internal sealed class CSharpClrReferenceDiscoveryException : Exception
{
    public CSharpClrReferenceDiscoveryException(
        string assemblyIdentity,
        string requirementDetail,
        string reason,
        Exception innerException)
        : base(
            $"Required CLR assembly '{assemblyIdentity}' for execution requirement " +
            $"'{requirementDetail}' could not be referenced: {reason}.",
            innerException)
    {
        AssemblyIdentity = string.IsNullOrWhiteSpace(assemblyIdentity)
            ? "<unknown>"
            : assemblyIdentity;
        RequirementDetail = string.IsNullOrWhiteSpace(requirementDetail)
            ? "<unknown execution requirement>"
            : requirementDetail;
        Reason = string.IsNullOrWhiteSpace(reason) ? "the CLR requirement could not be resolved" : reason;
    }

    public string AssemblyIdentity { get; }

    public string RequirementDetail { get; }

    public string Reason { get; }

    internal static CSharpClrReferenceDiscoveryException ForMetadataReference(
        Assembly assembly,
        string requirementDetail,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(exception);

        return new CSharpClrReferenceDiscoveryException(
            assembly.FullName ?? assembly.GetName().Name ?? "<unknown>",
            requirementDetail,
            GetStableReason(exception),
            exception);
    }

    internal static string GetStableReason(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            FileNotFoundException => "the assembly file could not be found",
            BadImageFormatException => "the assembly file is not a valid CLR metadata image",
            FileLoadException => "the assembly file could not be loaded",
            UnauthorizedAccessException or IOException => "the assembly file could not be read",
            ArgumentException => "the assembly metadata reference could not be created",
            InvalidOperationException or NotSupportedException or TypeLoadException =>
                "the CLR descriptor could not be resolved",
            _ => exception.Message
        };
    }
}
