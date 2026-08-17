namespace ADComputerSearchTool.Models;

public sealed class ComputerSearchCriteria
{
    public string ComputerName { get; init; } =
        string.Empty;

    public string OrganizationalUnit { get; init; } =
        string.Empty;

    public string Description { get; init; } =
        string.Empty;

    public string GroupName { get; init; } =
        string.Empty;

    public string IpAddress { get; init; } =
        string.Empty;

    public ComputerStatusFilter Status { get; init; } =
        ComputerStatusFilter.All;
}