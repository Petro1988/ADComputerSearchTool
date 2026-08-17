using ADComputerSearchTool.Models;
using ADComputerSearchTool.Services.Interfaces;
using System.Windows;

namespace ADComputerSearchTool.Services;

public sealed class ClipboardService : IClipboardService
{
    public void CopyText(string text)
    {
        Clipboard.SetText(
            text ?? string.Empty);
    }

    public void CopyComputerRow(
        ComputerRecord computer)
    {
        ArgumentNullException.ThrowIfNull(computer);

        string lastLogon =
            computer.LastLogon?.ToString(
                "dd.MM.yyyy HH:mm") ??
            string.Empty;

        string clipboardText =
            string.Join(
                "\t",
                computer.Name,
                computer.IpAddress,
                computer.Status,
                computer.Description,
                computer.OrganizationalUnit,
                computer.MemberOf,
                lastLogon,
                computer.OperatingSystem,
                computer.DnsHostName);

        Clipboard.SetText(clipboardText);
    }
}