using ADComputerSearchTool.Models;

namespace ADComputerSearchTool.Helpers;

public static class LdapFilterHelper
{
    private const string BitwiseAndMatchingRule =
        "1.2.840.113556.1.4.803";

    private const int AccountDisabledFlag = 2;

    public static string BuildComputerFilter(
        ComputerSearchCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        List<string> filters =
        [
            "(objectCategory=computer)"
        ];

        if (!string.IsNullOrWhiteSpace(
                criteria.ComputerName))
        {
            filters.Add(
                $"(name=*{EscapeValue(criteria.ComputerName)}*)");
        }

        if (!string.IsNullOrWhiteSpace(
                criteria.Description))
        {
            filters.Add(
                $"(description=*{EscapeValue(criteria.Description)}*)");
        }

        switch (criteria.Status)
        {
            case ComputerStatusFilter.Enabled:
                filters.Add(
                    $"(!(userAccountControl:{BitwiseAndMatchingRule}:={AccountDisabledFlag}))");
                break;

            case ComputerStatusFilter.Disabled:
                filters.Add(
                    $"(userAccountControl:{BitwiseAndMatchingRule}:={AccountDisabledFlag})");
                break;
        }

        return $"(&{string.Concat(filters)})";
    }

    private static string EscapeValue(
        string value)
    {
        return value
            .Replace(@"\", @"\5c")
            .Replace("*", @"\2a")
            .Replace("(", @"\28")
            .Replace(")", @"\29")
            .Replace("\0", @"\00");
    }
}