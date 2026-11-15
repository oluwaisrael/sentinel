using System.Net;
using System.Net.Sockets;

namespace Sentinel.Application.Monitors;

public static class MonitorUrlPolicy
{
    public static bool IsAllowed(Uri uri)
    {
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
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

    public static async Task<IReadOnlyList<IPAddress>> ResolvePublicAddressesAsync(
        string host,
        CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(host, out var literalAddress))
        {
            return IsPublicAddress(literalAddress) ? [literalAddress] : [];
        }

        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        return addresses.Where(IsPublicAddress).Distinct().ToArray();
    }

    public static bool IsPublicAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.None) || address.Equals(IPAddress.IPv6None))
        {
            return false;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            var ipv6Bytes = address.GetAddressBytes();
            var uniqueLocal = (ipv6Bytes[0] & 0xFE) == 0xFC;
            return !address.IsIPv6LinkLocal && !address.IsIPv6SiteLocal && !uniqueLocal;
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

