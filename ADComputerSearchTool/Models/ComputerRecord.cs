using System;

namespace ADComputerSearchTool.Models;

public sealed class ComputerRecord
{
    public string Name { get; init; } =
        string.Empty;

    public string IpAddress { get; init; } =
        string.Empty;

    public bool IsEnabled { get; init; }

    public string Status =>
        IsEnabled
            ? "Aktiv"
            : "Deaktiviert";

    public string Description { get; init; } =
        string.Empty;

    public string OrganizationalUnit { get; init; } =
        string.Empty;

    public string MemberOf { get; init; } =
        string.Empty;

    public DateTime? LastLogon { get; init; }

    public string OperatingSystem { get; init; } =
        string.Empty;

    public string DnsHostName { get; init; } =
        string.Empty;

    public string DistinguishedName { get; init; } =
        string.Empty;
}