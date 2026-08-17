using ADComputerSearchTool.Exceptions;
using ADComputerSearchTool.Helpers;
using ADComputerSearchTool.Models;
using ADComputerSearchTool.Services.Interfaces;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

using LdapSearchScope =
    System.DirectoryServices.Protocols.SearchScope;

namespace ADComputerSearchTool.Services;

public sealed class ActiveDirectoryService :
    IActiveDirectoryService
{
    private const int AccountDisabledFlag = 2;

    private static readonly string[] ComputerAttributes =
    [
        "name",
        "description",
        "distinguishedName",
        "userAccountControl",
        "memberOf",
        "lastLogonTimestamp",
        "operatingSystem",
        "dNSHostName"
    ];

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
            using LdapConnection connection =
                CreateConnection();

            string baseDistinguishedName =
                GetDefaultNamingContext(connection);

            string ldapFilter =
                LdapFilterHelper.BuildComputerFilter(
                    criteria);

            SearchRequest request =
                new SearchRequest(
                    baseDistinguishedName,
                    ldapFilter,
                    LdapSearchScope.Subtree,
                    ComputerAttributes);

            request.SizeLimit = 5000;
            request.TimeLimit = TimeSpan.FromMinutes(2);

            SearchResponse response =
                (SearchResponse)
                connection.SendRequest(request);

            List<ComputerRecord> computers = [];

            foreach (SearchResultEntry entry
                     in response.Entries)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                ComputerRecord computer =
                    await CreateComputerRecordAsync(
                        entry,
                        cancellationToken);

                computers.Add(computer);
            }

            return ApplyLocalFilters(
                    computers,
                    criteria)
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
        catch (Exception exception)
        {
            throw new ActiveDirectoryQueryException(
                "Die Computerinformationen konnten nicht " +
                "aus dem Active Directory gelesen werden.",
                exception);
        }
    }

    private async Task<ComputerRecord>
        CreateComputerRecordAsync(
            SearchResultEntry entry,
            CancellationToken cancellationToken)
    {
        string computerName =
            GetAttribute(entry, "name");

        string dnsHostName =
            GetAttribute(entry, "dNSHostName");

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
            (userAccountControl &
             AccountDisabledFlag) == 0;

        string[] groups =
            GetMultipleAttributes(
                    entry,
                    "memberOf")
                .Select(
                    DistinguishedNameHelper
                        .GetCommonName)
                .Where(group =>
                    !string.IsNullOrWhiteSpace(group))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group)
                .ToArray();

        string ipAddress =
            await _ipAddressService
                .ResolveIPv4AddressAsync(
                    computerName,
                    dnsHostName,
                    cancellationToken);

        return new ComputerRecord
        {
            Name =
                computerName,

            IpAddress =
                ipAddress,

            IsEnabled =
                isEnabled,

            Description =
                GetAttribute(
                    entry,
                    "description"),

            OrganizationalUnit =
                DistinguishedNameHelper
                    .GetOrganizationalUnitPath(
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
                dnsHostName,

            DistinguishedName =
                distinguishedName
        };
    }

    private static IEnumerable<ComputerRecord>
        ApplyLocalFilters(
            IEnumerable<ComputerRecord> computers,
            ComputerSearchCriteria criteria)
    {
        IEnumerable<ComputerRecord> result =
            computers;

        if (!string.IsNullOrWhiteSpace(
                criteria.OrganizationalUnit))
        {
            result =
                result.Where(computer =>
                    computer
                        .OrganizationalUnit
                        .Contains(
                            criteria.OrganizationalUnit,
                            StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(
                criteria.GroupName))
        {
            result =
                result.Where(computer =>
                    computer
                        .MemberOf
                        .Contains(
                            criteria.GroupName,
                            StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(
                criteria.IpAddress))
        {
            result =
                result.Where(computer =>
                    computer
                        .IpAddress
                        .Contains(
                            criteria.IpAddress,
                            StringComparison.OrdinalIgnoreCase));
        }

        return result;
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
                "Es konnte keine Windows-Domäne ermittelt " +
                "werden. Der Rechner muss Mitglied der Domäne " +
                "sein oder über VPN Zugriff auf die Domäne haben.");
        }

        LdapDirectoryIdentifier identifier =
            new LdapDirectoryIdentifier(
                domainName,
                389,
                fullyQualifiedDnsHostName: false,
                connectionless: false);

        LdapConnection connection =
            new LdapConnection(identifier)
            {
                AuthType =
                    AuthType.Negotiate,

                Credential =
                    CredentialCache
                        .DefaultNetworkCredentials,

                Timeout =
                    TimeSpan.FromSeconds(30)
            };

        connection.SessionOptions.ProtocolVersion = 3;
        connection.SessionOptions.Signing = true;
        connection.SessionOptions.Sealing = true;

        connection.Bind();

        return connection;
    }

    private static string GetDefaultNamingContext(
        LdapConnection connection)
    {
        SearchRequest request =
            new SearchRequest(
                distinguishedName: null,
                ldapFilter: "(objectClass=*)",
                searchScope: LdapSearchScope.Base,
                attributeList:
                    "defaultNamingContext");

        SearchResponse response =
            (SearchResponse)
            connection.SendRequest(request);

        if (response.Entries.Count == 0)
        {
            throw new ActiveDirectoryQueryException(
                "Der LDAP-Server hat keinen " +
                "defaultNamingContext zurückgegeben.");
        }

        string defaultNamingContext =
            GetAttribute(
                response.Entries[0],
                "defaultNamingContext");

        if (string.IsNullOrWhiteSpace(
                defaultNamingContext))
        {
            throw new ActiveDirectoryQueryException(
                "Der LDAP-Basispfad der Domäne konnte " +
                "nicht ermittelt werden.");
        }

        return defaultNamingContext;
    }

    private static string GetAttribute(
        SearchResultEntry entry,
        string attributeName)
    {
        if (!entry.Attributes.Contains(
                attributeName))
        {
            return string.Empty;
        }

        DirectoryAttribute attribute =
            entry.Attributes[attributeName];

        if (attribute.Count == 0)
        {
            return string.Empty;
        }

        object? value = attribute[0];

        if (value is byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        return value?.ToString() ??
               string.Empty;
    }

    private static IEnumerable<string>
        GetMultipleAttributes(
            SearchResultEntry entry,
            string attributeName)
    {
        if (!entry.Attributes.Contains(
                attributeName))
        {
            return Enumerable.Empty<string>();
        }

        DirectoryAttribute attribute =
            entry.Attributes[attributeName];

        return attribute
            .GetValues(typeof(string))
            .Cast<string>();
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
}