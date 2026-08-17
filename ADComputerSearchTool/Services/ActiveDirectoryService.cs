using ADComputerSearchTool.Exceptions;
using ADComputerSearchTool.Models;
using ADComputerSearchTool.Services.Interfaces;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Net.NetworkInformation;

using LdapSearchScope =
    System.DirectoryServices.Protocols.SearchScope;

namespace ADComputerSearchTool.Services;

public sealed class ActiveDirectoryService : IActiveDirectoryService
{
    private const int DisabledAccountFlag = 0x2;
    private const int MaximumResultCount = 5000;

    private static readonly TimeSpan LdapTimeout =
        TimeSpan.FromSeconds(30);

    private static readonly TimeSpan SearchTimeout =
        TimeSpan.FromMinutes(2);

    private readonly IIpAddressService _ipAddressService;

    public ActiveDirectoryService(
        IIpAddressService ipAddressService)
    {
        _ipAddressService =
            ipAddressService ??
            throw new ArgumentNullException(
                nameof(ipAddressService));
    }

    public async Task<IReadOnlyList<ComputerRecord>>
        SearchComputersAsync(
            ComputerSearchCriteria criteria,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        try
        {
            List<AdComputerData> adComputers =
                await Task.Run(
                    () => QueryActiveDirectory(
                        criteria,
                        cancellationToken),
                    cancellationToken);

            List<ComputerRecord> computers =
                new List<ComputerRecord>(
                    adComputers.Count);

            foreach (AdComputerData adComputer in adComputers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string ipAddress =
                    await _ipAddressService
                        .ResolveIPv4AddressAsync(
                            adComputer.Name,
                            adComputer.DnsHostName,
                            cancellationToken);

                ComputerRecord computer =
                    new ComputerRecord
                    {
                        Name =
                            adComputer.Name,

                        IpAddress =
                            ipAddress,

                        IsEnabled =
                            adComputer.IsEnabled,

                        Description =
                            adComputer.Description,

                        OrganizationalUnit =
                            adComputer.OrganizationalUnit,

                        MemberOf =
                            adComputer.MemberOf,

                        LastLogon =
                            adComputer.LastLogon,

                        OperatingSystem =
                            adComputer.OperatingSystem,

                        DnsHostName =
                            adComputer.DnsHostName,

                        DistinguishedName =
                            adComputer.DistinguishedName
                    };

                computers.Add(computer);
            }

            IEnumerable<ComputerRecord> filteredComputers =
                ApplyLocalFilters(
                    computers,
                    criteria);

            return filteredComputers
                .OrderBy(computer => computer.Name)
                .ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ActiveDirectoryQueryException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ActiveDirectoryQueryException(
                "Die Computer konnten nicht aus dem Active Directory gelesen werden.",
                ex);
        }
    }

    private static List<AdComputerData> QueryActiveDirectory(
        ComputerSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using LdapConnection connection =
            CreateConnection();

        string baseDistinguishedName =
            GetDefaultNamingContext(connection);

        string ldapFilter =
            BuildLdapFilter(criteria);

        SearchRequest request =
            CreateComputerSearchRequest(
                baseDistinguishedName,
                ldapFilter);

        SearchResponse response;

        try
        {
            response =
                (SearchResponse)
                connection.SendRequest(request);
        }
        catch (DirectoryOperationException ex)
        {
            throw new ActiveDirectoryQueryException(
                "Die LDAP-Suchanfrage konnte nicht ausgeführt werden.",
                ex);
        }
        catch (LdapException ex)
        {
            throw new ActiveDirectoryQueryException(
                "Die Verbindung zum Active Directory ist fehlgeschlagen.",
                ex);
        }

        List<AdComputerData> results =
            new List<AdComputerData>(
                response.Entries.Count);

        foreach (SearchResultEntry entry in response.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            results.Add(
                CreateAdComputerData(entry));
        }

        return results;
    }

    private static SearchRequest CreateComputerSearchRequest(
        string baseDistinguishedName,
        string ldapFilter)
    {
        SearchRequest request =
            new SearchRequest(
                baseDistinguishedName,
                ldapFilter,
                LdapSearchScope.Subtree,
                "name",
                "description",
                "distinguishedName",
                "userAccountControl",
                "memberOf",
                "lastLogonTimestamp",
                "operatingSystem",
                "dNSHostName");

        request.SizeLimit =
            MaximumResultCount;

        request.TimeLimit =
            SearchTimeout;

        return request;
    }

    private static AdComputerData CreateAdComputerData(
        SearchResultEntry entry)
    {
        string distinguishedName =
            GetAttribute(
                entry,
                "distinguishedName");

        int.TryParse(
            GetAttribute(
                entry,
                "userAccountControl"),
            out int userAccountControl);

        bool isEnabled =
            (userAccountControl & DisabledAccountFlag) == 0;

        string[] groups =
            GetMultipleAttributes(
                entry,
                "memberOf")
            .Select(GetCommonName)
            .Where(group =>
                !string.IsNullOrWhiteSpace(group))
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group)
            .ToArray();

        return new AdComputerData
        {
            Name =
                GetAttribute(
                    entry,
                    "name"),

            IsEnabled =
                isEnabled,

            Description =
                GetAttribute(
                    entry,
                    "description"),

            OrganizationalUnit =
                GetOrganizationalUnitPath(
                    distinguishedName),

            MemberOf =
                string.Join(
                    "; ",
                    groups),

            LastLogon =
                ConvertFileTime(
                    GetAttribute(
                        entry,
                        "lastLogonTimestamp")),

            OperatingSystem =
                GetAttribute(
                    entry,
                    "operatingSystem"),

            DnsHostName =
                GetAttribute(
                    entry,
                    "dNSHostName"),

            DistinguishedName =
                distinguishedName
        };
    }

    private static IEnumerable<ComputerRecord> ApplyLocalFilters(
        IEnumerable<ComputerRecord> computers,
        ComputerSearchCriteria criteria)
    {
        IEnumerable<ComputerRecord> filteredComputers =
            computers;

        if (!string.IsNullOrWhiteSpace(
                criteria.OrganizationalUnit))
        {
            filteredComputers =
                filteredComputers.Where(computer =>
                    computer.OrganizationalUnit.Contains(
                        criteria.OrganizationalUnit,
                        StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(
                criteria.GroupName))
        {
            filteredComputers =
                filteredComputers.Where(computer =>
                    computer.MemberOf.Contains(
                        criteria.GroupName,
                        StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(
                criteria.IpAddress))
        {
            filteredComputers =
                filteredComputers.Where(computer =>
                    computer.IpAddress.Contains(
                        criteria.IpAddress,
                        StringComparison.OrdinalIgnoreCase));
        }

        return filteredComputers;
    }

    private static LdapConnection CreateConnection()
    {
        string domainName =
            IPGlobalProperties
                .GetIPGlobalProperties()
                .DomainName;

        if (string.IsNullOrWhiteSpace(domainName))
        {
            throw new ActiveDirectoryQueryException(
                "Es konnte keine Windows-Domäne ermittelt werden. " +
                "Der Rechner muss Mitglied der Domäne sein oder über VPN Zugriff haben.");
        }

        LdapDirectoryIdentifier identifier =
            new LdapDirectoryIdentifier(
                domainName,
                389,
                false,
                false);

        LdapConnection connection =
            new LdapConnection(identifier)
            {
                AuthType =
                    AuthType.Negotiate,

                Credential =
                    CredentialCache.DefaultNetworkCredentials,

                Timeout =
                    LdapTimeout
            };

        connection.SessionOptions.ProtocolVersion = 3;
        connection.SessionOptions.Signing = true;
        connection.SessionOptions.Sealing = true;

        try
        {
            connection.Bind();
        }
        catch (LdapException ex)
        {
            connection.Dispose();

            throw new ActiveDirectoryQueryException(
                $"Die Verbindung zur Domäne „{domainName}“ konnte nicht hergestellt werden.",
                ex);
        }

        return connection;
    }

    private static string GetDefaultNamingContext(
        LdapConnection connection)
    {
        SearchRequest request =
            new SearchRequest(
                null,
                "(objectClass=*)",
                LdapSearchScope.Base,
                "defaultNamingContext");

        SearchResponse response =
            (SearchResponse)
            connection.SendRequest(request);

        if (response.Entries.Count == 0)
        {
            throw new ActiveDirectoryQueryException(
                "Der Domänencontroller hat keinen defaultNamingContext zurückgegeben.");
        }

        string defaultNamingContext =
            GetAttribute(
                response.Entries[0],
                "defaultNamingContext");

        if (string.IsNullOrWhiteSpace(
                defaultNamingContext))
        {
            throw new ActiveDirectoryQueryException(
                "Der LDAP-Basispfad der Domäne konnte nicht ermittelt werden.");
        }

        return defaultNamingContext;
    }

    private static string BuildLdapFilter(
        ComputerSearchCriteria criteria)
    {
        List<string> filters =
            new List<string>
            {
                "(objectCategory=computer)"
            };

        if (!string.IsNullOrWhiteSpace(
                criteria.ComputerName))
        {
            filters.Add(
                $"(name=*{EscapeLdapValue(criteria.ComputerName)}*)");
        }

        if (!string.IsNullOrWhiteSpace(
                criteria.Description))
        {
            filters.Add(
                $"(description=*{EscapeLdapValue(criteria.Description)}*)");
        }

        if (criteria.Status ==
            ComputerStatusFilter.Enabled)
        {
            filters.Add(
                "(!(userAccountControl:" +
                "1.2.840.113556.1.4.803:=2))");
        }
        else if (criteria.Status ==
                 ComputerStatusFilter.Disabled)
        {
            filters.Add(
                "(userAccountControl:" +
                "1.2.840.113556.1.4.803:=2)");
        }

        return $"(&{string.Concat(filters)})";
    }

    private static string GetAttribute(
        SearchResultEntry entry,
        string attributeName)
    {
        if (!entry.Attributes.Contains(attributeName))
        {
            return string.Empty;
        }

        DirectoryAttribute attribute =
            entry.Attributes[attributeName];

        if (attribute.Count == 0)
        {
            return string.Empty;
        }

        object? value =
            attribute[0];

        if (value is byte[] bytes)
        {
            return System.Text.Encoding.UTF8
                .GetString(bytes);
        }

        return value?.ToString() ??
               string.Empty;
    }

    private static IEnumerable<string> GetMultipleAttributes(
        SearchResultEntry entry,
        string attributeName)
    {
        if (!entry.Attributes.Contains(attributeName))
        {
            return Enumerable.Empty<string>();
        }

        DirectoryAttribute attribute =
            entry.Attributes[attributeName];

        return attribute
            .GetValues(typeof(string))
            .Cast<string>();
    }

    private static string GetOrganizationalUnitPath(
        string distinguishedName)
    {
        if (string.IsNullOrWhiteSpace(
                distinguishedName))
        {
            return string.Empty;
        }

        IEnumerable<string> organizationalUnits =
            SplitDistinguishedName(distinguishedName)
                .Where(part =>
                    part.StartsWith(
                        "OU=",
                        StringComparison.OrdinalIgnoreCase))
                .Select(part =>
                    UnescapeDistinguishedNameValue(
                        part.Substring(3)));

        return string.Join(
            " / ",
            organizationalUnits);
    }

    private static string GetCommonName(
        string distinguishedName)
    {
        if (string.IsNullOrWhiteSpace(
                distinguishedName))
        {
            return string.Empty;
        }

        string firstPart =
            SplitDistinguishedName(distinguishedName)
                .FirstOrDefault() ??
            distinguishedName;

        if (firstPart.StartsWith(
                "CN=",
                StringComparison.OrdinalIgnoreCase))
        {
            firstPart =
                firstPart.Substring(3);
        }

        return UnescapeDistinguishedNameValue(
            firstPart);
    }

    private static IEnumerable<string> SplitDistinguishedName(
        string distinguishedName)
    {
        List<string> parts =
            new List<string>();

        System.Text.StringBuilder current =
            new System.Text.StringBuilder();

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

    private static string UnescapeDistinguishedNameValue(
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

    private static DateTime? ConvertFileTime(
        string value)
    {
        if (!long.TryParse(
                value,
                out long fileTime) ||
            fileTime <= 0)
        {
            return null;
        }

        try
        {
            return DateTime
                .FromFileTimeUtc(fileTime)
                .ToLocalTime();
        }
        catch
        {
            return null;
        }
    }

    private static string EscapeLdapValue(
        string value)
    {
        return value
            .Replace(@"\", @"\5c")
            .Replace("*", @"\2a")
            .Replace("(", @"\28")
            .Replace(")", @"\29")
            .Replace("\0", @"\00");
    }

    private sealed class AdComputerData
    {
        public string Name { get; init; } =
            string.Empty;

        public bool IsEnabled { get; init; }

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
}