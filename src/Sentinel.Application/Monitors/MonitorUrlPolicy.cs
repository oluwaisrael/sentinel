using System.Net;

namespace Sentinel.Application.Monitors;

public static class MonitorUrlPolicy
{
    public static bool IsAllowed(Uri uri)
    {
        if (uri.Scheme is not (Uri.UriSchemeHttp or Uri.UriSchemeHttps) ||
            uri.IsFile ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("metadata.google.internal", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !IPAddress.TryParse(uri.Host, out var address) || IsPublicAddress(address);
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || address.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(address.MapToIPv4()))
        {
            return false;
        }

        var bytes = address.MapToIPv4().GetAddressBytes();
        return bytes[0] switch
        {
            10 => false,
            172 when bytes[1] is >= 16 and <= 31 => false,
            192 when bytes[1] == 168 => false,
            169 when bytes[1] == 254 => false,
            0 => false,
            _ => true
        };
    }
}

