using ADComputerSearchTool.Services.Interfaces;
using System.Net;
using System.Net.Sockets;

namespace ADComputerSearchTool.Services;

public sealed class DnsIpAddressService : IIpAddressService
{
    public async Task<string> ResolveIPv4AddressAsync(
        string computerName,
        string dnsHostName,
        CancellationToken cancellationToken = default)
    {
        string hostName =
            GetHostName(
                computerName,
                dnsHostName);

        if (string.IsNullOrWhiteSpace(hostName))
        {
            return string.Empty;
        }

        try
        {
            IPAddress[] addresses =
                await Dns.GetHostAddressesAsync(
                    hostName,
                    cancellationToken);

            IPAddress? ipv4Address =
                addresses.FirstOrDefault(address =>
                    address.AddressFamily ==
                    AddressFamily.InterNetwork);

            return ipv4Address?.ToString() ??
                   string.Empty;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SocketException)
        {
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetHostName(
        string computerName,
        string dnsHostName)
    {
        if (!string.IsNullOrWhiteSpace(dnsHostName))
        {
            return dnsHostName.Trim();
        }

        return computerName?.Trim() ??
               string.Empty;
    }
}