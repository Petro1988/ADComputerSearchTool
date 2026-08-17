using System.Text;

namespace ADComputerSearchTool.Helpers;

public static class DistinguishedNameHelper
{
    public static string GetOrganizationalUnitPath(
        string distinguishedName)
    {
        if (string.IsNullOrWhiteSpace(
                distinguishedName))
        {
            return string.Empty;
        }

        IEnumerable<string> organizationalUnits =
            Split(distinguishedName)
                .Where(part =>
                    part.StartsWith(
                        "OU=",
                        StringComparison.OrdinalIgnoreCase))
                .Select(part =>
                    UnescapeValue(part[3..]));

        return string.Join(
            " / ",
            organizationalUnits);
    }

    public static string GetCommonName(
        string distinguishedName)
    {
        if (string.IsNullOrWhiteSpace(
                distinguishedName))
        {
            return string.Empty;
        }

        string firstPart =
            Split(distinguishedName).FirstOrDefault() ??
            distinguishedName;

        if (firstPart.StartsWith(
                "CN=",
                StringComparison.OrdinalIgnoreCase))
        {
            firstPart = firstPart[3..];
        }

        return UnescapeValue(firstPart);
    }

    private static IReadOnlyList<string> Split(
        string distinguishedName)
    {
        List<string> parts = [];
        StringBuilder current = new();

        bool escaped = false;

        foreach (char character in distinguishedName)
        {
            if (escaped)
            {
                current.Append(character);
                escaped = false;
                continue;
            }

            if (character == '\\')
            {
                current.Append(character);
                escaped = true;
                continue;
            }

            if (character == ',')
            {
                parts.Add(
                    current.ToString().Trim());

                current.Clear();
                continue;
            }

            current.Append(character);
        }

        if (current.Length > 0)
        {
            parts.Add(
                current.ToString().Trim());
        }

        return parts;
    }

    private static string UnescapeValue(
        string value)
    {
        return value
            .Replace(@"\,", ",")
            .Replace(@"\+", "+")
            .Replace(@"\=", "=")
            .Replace(@"\#", "#")
            .Replace(@"\;", ";")
            .Replace(@"\\", @"\");
    }
}