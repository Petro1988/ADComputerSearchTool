using ADComputerSearchTool.Helpers;
using ADComputerSearchTool.Models;
using Xunit;

namespace ADComputerSearchTool.Tests.Helpers;

public sealed class LdapFilterHelperTests
{
    [Fact]
    public void BuildComputerFilter_WithEmptyCriteria_ReturnsComputerFilter()
    {
        ComputerSearchCriteria criteria =
            new ComputerSearchCriteria
            {
                Status =
                    ComputerStatusFilter.All
            };

        string result =
            LdapFilterHelper.BuildComputerFilter(
                criteria);

        Assert.Equal(
            "(&(objectCategory=computer))",
            result);
    }

    [Fact]
    public void BuildComputerFilter_WithComputerName_AddsNameFilter()
    {
        ComputerSearchCriteria criteria =
            new ComputerSearchCriteria
            {
                ComputerName =
                    "NB-038",

                Status =
                    ComputerStatusFilter.All
            };

        string result =
            LdapFilterHelper.BuildComputerFilter(
                criteria);

        Assert.Equal(
            "(&(objectCategory=computer)(name=*NB-038*))",
            result);
    }

    [Fact]
    public void BuildComputerFilter_WithDescription_AddsDescriptionFilter()
    {
        ComputerSearchCriteria criteria =
            new ComputerSearchCriteria
            {
                Description =
                    "Controlling",

                Status =
                    ComputerStatusFilter.All
            };

        string result =
            LdapFilterHelper.BuildComputerFilter(
                criteria);

        Assert.Equal(
            "(&(objectCategory=computer)(description=*Controlling*))",
            result);
    }

    [Fact]
    public void BuildComputerFilter_WithEnabledStatus_AddsEnabledFilter()
    {
        ComputerSearchCriteria criteria =
            new ComputerSearchCriteria
            {
                Status =
                    ComputerStatusFilter.Enabled
            };

        string result =
            LdapFilterHelper.BuildComputerFilter(
                criteria);

        Assert.Equal(
            "(&(objectCategory=computer)" +
            "(!(userAccountControl:1.2.840.113556.1.4.803:=2)))",
            result);
    }

    [Fact]
    public void BuildComputerFilter_WithDisabledStatus_AddsDisabledFilter()
    {
        ComputerSearchCriteria criteria =
            new ComputerSearchCriteria
            {
                Status =
                    ComputerStatusFilter.Disabled
            };

        string result =
            LdapFilterHelper.BuildComputerFilter(
                criteria);

        Assert.Equal(
            "(&(objectCategory=computer)" +
            "(userAccountControl:1.2.840.113556.1.4.803:=2))",
            result);
    }

    [Theory]
    [InlineData(@"NB\Test", @"NB\5cTest")]
    [InlineData("NB*038", @"NB\2a038")]
    [InlineData("NB(038", @"NB\28038")]
    [InlineData("NB)038", @"NB\29038")]
    public void BuildComputerFilter_EscapesSpecialCharacters(
        string input,
        string expectedEscapedValue)
    {
        ComputerSearchCriteria criteria =
            new ComputerSearchCriteria
            {
                ComputerName =
                    input,

                Status =
                    ComputerStatusFilter.All
            };

        string result =
            LdapFilterHelper.BuildComputerFilter(
                criteria);

        Assert.Contains(
            $"(name=*{expectedEscapedValue}*)",
            result);
    }

    [Fact]
    public void BuildComputerFilter_WithSeveralCriteria_CombinesAllFilters()
    {
        ComputerSearchCriteria criteria =
            new ComputerSearchCriteria
            {
                ComputerName =
                    "NB",

                Description =
                    "Notebook",

                Status =
                    ComputerStatusFilter.Enabled
            };

        string result =
            LdapFilterHelper.BuildComputerFilter(
                criteria);

        Assert.Equal(
            "(&(objectCategory=computer)" +
            "(name=*NB*)" +
            "(description=*Notebook*)" +
            "(!(userAccountControl:1.2.840.113556.1.4.803:=2)))",
            result);
    }

    [Fact]
    public void BuildComputerFilter_WithNullCriteria_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () =>
                LdapFilterHelper.BuildComputerFilter(
                    null!));
    }
}