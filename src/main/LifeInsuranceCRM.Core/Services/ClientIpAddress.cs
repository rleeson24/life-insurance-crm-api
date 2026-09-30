using System.Net;
using Microsoft.AspNetCore.Http;

namespace LifeInsuranceCRM.Core.Services;

public static class ClientIpAddress
{
    public static string? Resolve(HttpContext? httpContext, bool trustForwardedFor)
    {
        if (httpContext is null)
        {
            return null;
        }

        if (trustForwardedFor
            && TryGetForwardedClient(httpContext.Request.Headers["X-Forwarded-For"].ToString(), out var forwarded))
        {
            return forwarded;
        }

        return Format(httpContext.Connection.RemoteIpAddress);
    }

    internal static bool TryGetForwardedClient(string? header, out string ip)
    {
        ip = string.Empty;
        if (string.IsNullOrWhiteSpace(header))
        {
            return false;
        }

        var parts = header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var index = parts.Length - 1; index >= 0; index--)
        {
            if (TryParse(parts[index], out ip))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool TryParse(string token, out string ip)
    {
        ip = string.Empty;
        var value = token.Trim().Trim('"');
        if (value.Length == 0)
        {
            return false;
        }

        if (value[0] == '[')
        {
            var end = value.IndexOf(']');
            if (end <= 1)
            {
                return false;
            }

            value = value[1..end];
        }
        else
        {
            var colon = value.LastIndexOf(':');
            if (colon > 0 && value.IndexOf(':') == colon)
            {
                value = value[..colon];
            }
        }

        if (!IPAddress.TryParse(value, out var address))
        {
            return false;
        }

        ip = Format(address)!;
        return true;
    }

    private static string? Format(IPAddress? address)
    {
        if (address is null)
        {
            return null;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return address.ToString();
    }
}
