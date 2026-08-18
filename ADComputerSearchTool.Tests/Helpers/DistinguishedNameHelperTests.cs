using ADComputerSearchTool.Helpers;
using Xunit;

namespace ADComputerSearchTool.Tests.Helpers;

public sealed class DistinguishedNameHelperTests
{
    [Fact]
    public void GetOrganizationalUnitPath_WithValidDn_ReturnsOuPath()
    {
        const string distinguishedName =
            "CN=NB-038,OU=Notebooks,OU=Hamburg,DC=example,DC=local";

        string result =
            DistinguishedNameHelper
                .GetOrganizationalUnitPath(
                    distinguishedName);

        Assert.Equal(
            "Notebooks / Hamburg",
            result);
    }

    [Fact]
    public void GetOrganizationalUnitPath_WithoutOu_ReturnsEmptyString()
    {
        const string distinguishedName =
            "CN=NB-038,CN=Computers,DC=example,DC=local";

        string result =
            DistinguishedNameHelper
                .GetOrganizationalUnitPath(
                    distinguishedName);

        Assert.Equal(
            string.Empty,
            result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void GetOrganizationalUnitPath_WithEmptyDn_ReturnsEmptyString(
        string distinguishedName)
    {
        string result =
            DistinguishedNameHelper
                .GetOrganizationalUnitPath(
                    distinguishedName);

        Assert.Empty(result);
    }

    [Fact]
    public void GetCommonName_WithGroupDn_ReturnsCommonName()
    {
        const string distinguishedName =
            "CN=VPN-Benutzer,OU=Gruppen,DC=example,DC=local";

        string result =
            DistinguishedNameHelper
                .GetCommonName(
                    distinguishedName);

        Assert.Equal(
            "VPN-Benutzer",
            result);
    }

    [Fact]
    public void GetCommonName_WithEscapedComma_ReturnsUnescapedName()
    {
        const string distinguishedName =
            @"CN=Gruppe\, Hamburg,OU=Gruppen,DC=example,DC=local";

        string result =
            DistinguishedNameHelper
                .GetCommonName(
                    distinguishedName);

        Assert.Equal(
            "Gruppe, Hamburg",
            result);
    }

    [Fact]
    public void GetCommonName_WithEscapedPlus_ReturnsUnescapedName()
    {
        const string distinguishedName =
            @"CN=Support\+IT,OU=Gruppen,DC=example,DC=local";

        string result =
            DistinguishedNameHelper
                .GetCommonName(
                    distinguishedName);

        Assert.Equal(
            "Support+IT",
            result);
    }

    [Fact]
    public void GetOrganizationalUnitPath_WithEscapedComma_DoesNotSplitEscapedComma()
    {
        const string distinguishedName =
            @"CN=NB-038,OU=Hamburg\, Nord,OU=Computer,DC=example,DC=local";

        string result =
            DistinguishedNameHelper
                .GetOrganizationalUnitPath(
                    distinguishedName);

        Assert.Equal(
            "Hamburg, Nord / Computer",
            result);
    }

    [Fact]
    public void GetCommonName_WithEmptyValue_ReturnsEmptyString()
    {
        string result =
            DistinguishedNameHelper
                .GetCommonName(
                    string.Empty);

        Assert.Empty(result);
    }
}