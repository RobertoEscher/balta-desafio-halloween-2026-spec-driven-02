using System.Net;
using System.Net.Sockets;

namespace Shortener.Api.Services;

public static class PublicInternetConnector
{
    public static async ValueTask<Stream> ConnectAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        IPAddress[] addresses;
        if (IPAddress.TryParse(context.DnsEndPoint.Host, out var address))
        {
            addresses = [address];
        }
        else
        {
            addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
        }

        if (addresses.Length == 0 || addresses.Any(candidate => !IsPublic(candidate)))
        {
            throw new HttpRequestException("O destino não está acessível.");
        }

        Exception? lastError = null;
        foreach (var candidate in addresses)
        {
            var socket = new Socket(candidate.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(candidate, context.DnsEndPoint.Port, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception exception) when (exception is SocketException or OperationCanceledException)
            {
                socket.Dispose();
                if (exception is OperationCanceledException)
                {
                    throw;
                }

                lastError = exception;
            }
        }

        throw new HttpRequestException("O destino não está acessível.", lastError);
    }

    private static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            return IsPublic(address.MapToIPv4());
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            return !address.Equals(IPAddress.IPv6Any)
                && !address.Equals(IPAddress.IPv6Loopback)
                && !address.IsIPv6LinkLocal
                && !address.IsIPv6SiteLocal
                && !address.IsIPv6Multicast
                && (bytes[0] & 0xFE) != 0xFC
                && (bytes[0] & 0xE0) == 0x20;
        }

        var octets = address.GetAddressBytes();
        var first = octets[0];
        var second = octets[1];
        return first != 0
            && first != 10
            && first != 127
            && first < 224
            && !(first == 100 && second is >= 64 and <= 127)
            && !(first == 169 && second == 254)
            && !(first == 172 && second is >= 16 and <= 31)
            && !(first == 192 && second == 168)
            && !(first == 192 && second == 0 && octets[2] == 0)
            && !(first == 192 && second == 0 && octets[2] == 2)
            && !(first == 192 && second == 88 && octets[2] == 99)
            && !(first == 198 && second is 18 or 19)
            && !(first == 198 && second == 51 && octets[2] == 100)
            && !(first == 203 && second == 0 && octets[2] == 113);
    }
}
