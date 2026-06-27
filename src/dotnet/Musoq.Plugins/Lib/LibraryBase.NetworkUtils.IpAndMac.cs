using System.Net;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Checks if an IP address is a private (RFC 1918) address.
    /// </summary>
    /// <param name="ipAddress">The IP address to check</param>
    /// <returns>True if the IP is private</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public bool? IsPrivateIp(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return null;

        if (!IPAddress.TryParse(ipAddress, out var ip))
            return null;

        var bytes = ip.GetAddressBytes();
        if (bytes.Length != 4)
            return false;


        if (bytes[0] == 10)
            return true;


        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            return true;


        if (bytes[0] == 192 && bytes[1] == 168)
            return true;


        if (bytes[0] == 127)
            return true;

        return false;
    }

    /// <summary>
    ///     Converts an IPv4 address to its numeric representation.
    /// </summary>
    /// <param name="ipAddress">The IP address</param>
    /// <returns>The numeric representation</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public long? IpToLong(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return null;

        if (!IPAddress.TryParse(ipAddress, out var ip))
            return null;

        var bytes = ip.GetAddressBytes();
        if (bytes.Length != 4)
            return null;

        return ((long)bytes[0] << 24) | ((long)bytes[1] << 16) | ((long)bytes[2] << 8) | bytes[3];
    }

    /// <summary>
    ///     Converts a numeric IP representation back to dotted notation.
    /// </summary>
    /// <param name="ipNumber">The numeric IP representation</param>
    /// <returns>The IP address in dotted notation</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public string? LongToIp(long? ipNumber)
    {
        if (ipNumber is null or < 0 or > uint.MaxValue)
            return null;

        var num = (uint)ipNumber.Value;
        return $"{(num >> 24) & 0xFF}.{(num >> 16) & 0xFF}.{(num >> 8) & 0xFF}.{num & 0xFF}";
    }

    /// <summary>
    ///     Checks if an IP address is within a CIDR subnet.
    /// </summary>
    /// <param name="ipAddress">The IP address to check</param>
    /// <param name="cidr">The CIDR notation (e.g., "192.168.1.0/24")</param>
    /// <returns>True if the IP is in the subnet</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public bool? IsInSubnet(string? ipAddress, string? cidr)
    {
        if (string.IsNullOrEmpty(ipAddress) || string.IsNullOrEmpty(cidr))
            return null;

        try
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2)
                return null;

            if (!IPAddress.TryParse(ipAddress, out _) ||
                !IPAddress.TryParse(parts[0], out _) ||
                !int.TryParse(parts[1], out var prefixLength))
                return null;

            if (prefixLength is < 0 or > 32)
                return null;

            var ipLong = IpToLong(ipAddress);
            var subnetLong = IpToLong(parts[0]);

            if (!ipLong.HasValue || !subnetLong.HasValue)
                return null;

            var mask = prefixLength == 0 ? 0 : ~((1L << (32 - prefixLength)) - 1);
            return (ipLong.Value & mask) == (subnetLong.Value & mask);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Formats a MAC address with the specified separator.
    /// </summary>
    /// <param name="mac">The MAC address (can be with or without separators)</param>
    /// <param name="separator">The separator to use (default: ":")</param>
    /// <returns>The formatted MAC address</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public string? FormatMac(string? mac, string separator = ":")
    {
        if (string.IsNullOrEmpty(mac))
            return null;

        var clean = FormatMacRegex().Replace(mac, "");
        if (clean.Length != 12)
            return null;

        var parts = new string[6];
        for (var i = 0; i < 6; i++) parts[i] = clean.Substring(i * 2, 2).ToUpperInvariant();

        return string.Join(separator, parts);
    }
}
