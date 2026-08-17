namespace ADComputerSearchTool.Services.Interfaces;

public interface IIpAddressService
{
    Task<string> ResolveIPv4AddressAsync(
        string computerName,
        string dnsHostName,
        CancellationToken cancellationToken = default);
}