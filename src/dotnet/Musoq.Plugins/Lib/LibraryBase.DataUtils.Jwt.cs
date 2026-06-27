using System.Text;
using System.Text.Json;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Decodes a JWT token and returns the payload as JSON string.
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>The decoded payload as JSON string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public string? JwtDecode(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2)
                return null;

            var payload = parts[1];

            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var bytes = Convert.FromBase64String(payload);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Gets the header portion of a JWT token as JSON string.
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>The decoded header as JSON string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public string? JwtGetHeader(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var parts = token.Split('.');
            if (parts.Length < 1)
                return null;

            var header = parts[0];
            header = header.Replace('-', '+').Replace('_', '/');
            switch (header.Length % 4)
            {
                case 2: header += "=="; break;
                case 3: header += "="; break;
            }

            var bytes = Convert.FromBase64String(header);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Gets a specific claim from a JWT token payload.
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <param name="claimName">The name of the claim to retrieve</param>
    /// <returns>The claim value as string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public string? JwtGetClaim(string? token, string? claimName)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(claimName))
            return null;

        var payload = JwtDecode(token);
        if (payload == null)
            return null;

        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty(claimName, out var claim))
                return claim.ValueKind == JsonValueKind.String
                    ? claim.GetString()
                    : claim.GetRawText();
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Checks if a string appears to be a valid JWT token.
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <returns>True if the string appears to be a valid JWT</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public bool? IsJwt(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var parts = value.Split('.');
        if (parts.Length != 3)
            return false;

        return JwtGetHeader(value) != null && JwtDecode(value) != null;
    }
}
