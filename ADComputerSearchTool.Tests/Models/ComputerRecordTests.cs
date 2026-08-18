using ADComputerSearchTool.Models;
using Xunit;

namespace ADComputerSearchTool.Tests.Models;

public sealed class ComputerRecordTests
{
    [Fact]
    public void Status_WhenComputerIsEnabled_ReturnsAktiv()
    {
        ComputerRecord computer =
            new ComputerRecord
            {
                Name =
                    "NB-038",

                IsEnabled =
                    true
            };

        Assert.Equal(
            "Aktiv",
            computer.Status);
    }

    [Fact]
    public void Status_WhenComputerIsDisabled_ReturnsDeaktiviert()
    {
        ComputerRecord computer =
            new ComputerRecord
            {
                Name =
                    "NB-038",

                IsEnabled =
                    false
            };

        Assert.Equal(
            "Deaktiviert",
            computer.Status);
    }

    [Fact]
    public void NewComputerRecord_HasEmptyDefaultStrings()
    {
        ComputerRecord computer =
            new ComputerRecord();

        Assert.Empty(computer.Name);
        Assert.Empty(computer.IpAddress);
        Assert.Empty(computer.Description);
        Assert.Empty(computer.OrganizationalUnit);
        Assert.Empty(computer.MemberOf);
        Assert.Empty(computer.OperatingSystem);
        Assert.Empty(computer.DnsHostName);
        Assert.Empty(computer.DistinguishedName);
        Assert.Null(computer.LastLogon);
    }
}