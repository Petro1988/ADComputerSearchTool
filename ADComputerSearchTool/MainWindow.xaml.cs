using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using LdapSearchScope =
    System.DirectoryServices.Protocols.SearchScope;

namespace ADComputerSearchTool
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<ComputerRecord> _results =
            new ObservableCollection<ComputerRecord>();

        public MainWindow()
        {
            InitializeComponent();

            ResultsGrid.ItemsSource = _results;
        }

        private async void SearchButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetBusy(
                true,
                "Active Directory wird durchsucht und IP-Adressen werden ermittelt...");

            try
            {
                string selectedStatus = "Alle";

                if (StatusBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    selectedStatus =
                        selectedItem.Content?.ToString() ?? "Alle";
                }

                SearchCriteria criteria = new SearchCriteria
                {
                    ComputerName =
                        NameBox.Text.Trim(),

                    OrganizationalUnit =
                        OuBox.Text.Trim(),

                    Description =
                        DescriptionBox.Text.Trim(),

                    GroupName =
                        GroupBoxFilter.Text.Trim(),

                    IpAddress =
                        IpAddressBox.Text.Trim(),

                    Status =
                        selectedStatus
                };

                List<ComputerRecord> computers =
                    await Task.Run(() =>
                        ActiveDirectoryService.SearchComputers(
                            criteria));

                _results.Clear();

                foreach (ComputerRecord computer in computers)
                {
                    _results.Add(computer);
                }

                ResultsGrid.SelectedItem = null;
                ResultsGrid.UnselectAllCells();

                CountText.Text =
                    $"{_results.Count} Treffer";

                InfoText.Text =
                    "Suche abgeschlossen.";

                ExportButton.IsEnabled =
                    _results.Count > 0;
            }
            catch (Exception ex)
            {
                InfoText.Text =
                    "Fehler bei der AD-Abfrage.";

                MessageBox.Show(
                    ex.Message,
                    "Fehler bei der AD-Abfrage",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void ClearButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NameBox.Clear();
            OuBox.Clear();
            DescriptionBox.Clear();
            GroupBoxFilter.Clear();
            IpAddressBox.Clear();

            StatusBox.SelectedIndex = 0;

            ResultsGrid.SelectedItem = null;
            ResultsGrid.UnselectAllCells();

            _results.Clear();

            CountText.Text = "0 Treffer";
            InfoText.Text = "Bereit";

            ExportButton.IsEnabled = false;
        }

        /*
         * Beim Rechtsklick wird die angeklickte Zelle als
         * aktuelle Zelle gespeichert.
         *
         * Gleichzeitig wird der Datensatz der Zeile gesetzt.
         * Dadurch reicht eine einzelne angeklickte Zelle aus,
         * um anschließend die komplette Zeile zu kopieren.
         */
        private void ResultsGrid_PreviewMouseRightButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            DependencyObject? originalSource =
                e.OriginalSource as DependencyObject;

            DataGridCell? clickedCell =
                FindParent<DataGridCell>(originalSource);

            if (clickedCell == null)
            {
                ResultsGrid.SelectedItem = null;
                ResultsGrid.UnselectAllCells();

                return;
            }

            DataGridRow? clickedRow =
                FindParent<DataGridRow>(clickedCell);

            if (clickedRow?.Item is not ComputerRecord computer)
            {
                ResultsGrid.SelectedItem = null;
                ResultsGrid.UnselectAllCells();

                return;
            }

            /*
             * Die angeklickte Zelle wird als aktuelle Zelle gesetzt.
             * Das wird für "Zelle kopieren" verwendet.
             */
            ResultsGrid.CurrentCell =
                new DataGridCellInfo(
                    computer,
                    clickedCell.Column);

            /*
             * Nur die angeklickte Zelle wird sichtbar markiert.
             */
            ResultsGrid.UnselectAllCells();

            ResultsGrid.SelectedCells.Add(
                new DataGridCellInfo(
                    computer,
                    clickedCell.Column));

            /*
             * Zusätzlich wird der Datensatz der betreffenden
             * Zeile gespeichert. Die ganze Zeile muss dafür
             * nicht sichtbar markiert werden.
             */
            ResultsGrid.SelectedItem =
                computer;

            clickedCell.Focus();
        }

        private static T? FindParent<T>(
            DependencyObject? child)
            where T : DependencyObject
        {
            DependencyObject? current =
                child;

            while (current != null)
            {
                if (current is T requestedParent)
                {
                    return requestedParent;
                }

                if (current is Visual ||
                    current is Visual3D)
                {
                    current =
                        VisualTreeHelper.GetParent(current);
                }
                else if (current is FrameworkContentElement contentElement)
                {
                    current =
                        contentElement.Parent;
                }
                else
                {
                    current = null;
                }
            }

            return null;
        }

        /*
         * Kopiert die vollständige Zeile.
         *
         * Der ComputerRecord wird aus CurrentCell.Item gelesen.
         * Deshalb reicht es aus, nur eine Zelle auszuwählen.
         */
        private void CopyRowMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (ResultsGrid.CurrentCell.Item is not ComputerRecord computer)
            {
                MessageBox.Show(
                    "Bitte zuerst eine Zelle der gewünschten Zeile auswählen.",
                    "Zeile kopieren",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            string lastLogon =
                computer.LastLogon.HasValue
                    ? computer.LastLogon.Value.ToString(
                        "dd.MM.yyyy HH:mm")
                    : string.Empty;

            /*
             * Durch Tabulatoren wird die Zeile beim Einfügen
             * in Excel auf mehrere Spalten verteilt.
             */
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

            CopyTextToClipboard(
                clipboardText,
                $"Die vollständige Zeile von {computer.Name}");
        }

        /*
         * Kopiert nur den Inhalt der mit der rechten
         * Maustaste angeklickten Zelle.
         */
        private void CopyCellMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            DataGridCellInfo currentCell =
                ResultsGrid.CurrentCell;

            if (currentCell.Item is not ComputerRecord computer ||
                currentCell.Column == null)
            {
                MessageBox.Show(
                    "Bitte mit der rechten Maustaste direkt auf eine Tabellenzelle klicken.",
                    "Zelle kopieren",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            string cellValue =
                GetCellValue(
                    currentCell.Column,
                    computer);

            string columnHeader =
                currentCell.Column.Header?.ToString() ??
                "Zelle";

            CopyTextToClipboard(
                cellValue,
                columnHeader);
        }

        private void CopyTextToClipboard(
            string? text,
            string description)
        {
            string value =
                text ?? string.Empty;

            try
            {
                Clipboard.SetText(value);

                InfoText.Text =
                    $"{description} wurde kopiert.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{description} konnte nicht kopiert werden:\n\n" +
                    ex.Message,
                    "Fehler beim Kopieren",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /*
         * Liest anhand des DataGrid-Bindings den Wert
         * der angeklickten Zelle aus dem ComputerRecord.
         */
        private static string GetCellValue(
            DataGridColumn column,
            ComputerRecord computer)
        {
            if (column is not DataGridBoundColumn boundColumn)
            {
                return string.Empty;
            }

            if (boundColumn.Binding is not Binding binding)
            {
                return string.Empty;
            }

            string propertyName =
                binding.Path?.Path ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return string.Empty;
            }

            object? value =
                typeof(ComputerRecord)
                    .GetProperty(propertyName)?
                    .GetValue(computer);

            if (value == null)
            {
                return string.Empty;
            }

            if (value is DateTime dateTime)
            {
                return dateTime.ToString(
                    "dd.MM.yyyy HH:mm");
            }

            return value.ToString() ??
                   string.Empty;
        }

        private void ExportButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_results.Count == 0)
            {
                MessageBox.Show(
                    "Es sind keine Ergebnisse zum Exportieren vorhanden.",
                    "Excel-Export",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            SaveFileDialog dialog =
                new SaveFileDialog
                {
                    Title =
                        "AD-Computer nach Excel exportieren",

                    Filter =
                        "Excel-Arbeitsmappe (*.xlsx)|*.xlsx",

                    DefaultExt =
                        ".xlsx",

                    AddExtension =
                        true,

                    FileName =
                        $"AD-Computer_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx"
                };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                ExportToExcel(
                    dialog.FileName);

                MessageBox.Show(
                    "Der Excel-Export wurde erfolgreich erstellt.",
                    "Excel-Export",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Der Excel-Export ist fehlgeschlagen:\n\n" +
                    ex.Message,
                    "Fehler beim Excel-Export",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ExportToExcel(
            string fileName)
        {
            using XLWorkbook workbook =
                new XLWorkbook();

            IXLWorksheet worksheet =
                workbook.Worksheets.Add(
                    "AD-Computer");

            string[] headers =
            {
                "Computername",
                "IPv4-Adresse",
                "Status",
                "Beschreibung",
                "Organisationseinheit",
                "Mitglied von",
                "Letzte Anmeldung",
                "Betriebssystem",
                "DNS-Hostname",
                "Distinguished Name"
            };

            for (int column = 0;
                 column < headers.Length;
                 column++)
            {
                worksheet
                    .Cell(1, column + 1)
                    .Value = headers[column];
            }

            int row = 2;

            foreach (ComputerRecord computer in _results)
            {
                worksheet.Cell(row, 1).Value =
                    computer.Name;

                worksheet.Cell(row, 2).Value =
                    computer.IpAddress;

                worksheet.Cell(row, 3).Value =
                    computer.Status;

                worksheet.Cell(row, 4).Value =
                    computer.Description;

                worksheet.Cell(row, 5).Value =
                    computer.OrganizationalUnit;

                worksheet.Cell(row, 6).Value =
                    computer.MemberOf;

                if (computer.LastLogon.HasValue)
                {
                    worksheet.Cell(row, 7).Value =
                        computer.LastLogon.Value;

                    worksheet
                        .Cell(row, 7)
                        .Style
                        .DateFormat
                        .Format = "dd.MM.yyyy HH:mm";
                }

                worksheet.Cell(row, 8).Value =
                    computer.OperatingSystem;

                worksheet.Cell(row, 9).Value =
                    computer.DnsHostName;

                worksheet.Cell(row, 10).Value =
                    computer.DistinguishedName;

                row++;
            }

            IXLRange? usedRange =
                worksheet.RangeUsed();

            if (usedRange != null)
            {
                usedRange.CreateTable(
                    "ADComputerTabelle");
            }

            worksheet.SheetView.FreezeRows(1);

            worksheet.Columns().AdjustToContents();

            worksheet.Column(1).Width = 22;
            worksheet.Column(2).Width = 18;
            worksheet.Column(3).Width = 14;
            worksheet.Column(4).Width = 35;
            worksheet.Column(5).Width = 45;
            worksheet.Column(6).Width = 60;
            worksheet.Column(7).Width = 22;
            worksheet.Column(8).Width = 30;
            worksheet.Column(9).Width = 40;
            worksheet.Column(10).Width = 55;

            worksheet
                .Column(4)
                .Style
                .Alignment
                .WrapText = true;

            worksheet
                .Column(5)
                .Style
                .Alignment
                .WrapText = true;

            worksheet
                .Column(6)
                .Style
                .Alignment
                .WrapText = true;

            worksheet
                .Column(10)
                .Style
                .Alignment
                .WrapText = true;

            worksheet
                .Rows()
                .Style
                .Alignment
                .Vertical =
                XLAlignmentVerticalValues.Top;

            workbook.SaveAs(
                fileName);
        }

        private void SetBusy(
            bool busy,
            string? message = null)
        {
            SearchButton.IsEnabled =
                !busy;

            ClearButton.IsEnabled =
                !busy;

            ResultsGrid.IsEnabled =
                !busy;

            ExportButton.IsEnabled =
                !busy &&
                _results.Count > 0;

            BusyBar.Visibility =
                busy
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            if (!string.IsNullOrWhiteSpace(message))
            {
                InfoText.Text =
                    message;
            }
        }
    }

    public class SearchCriteria
    {
        public string ComputerName { get; set; } =
            string.Empty;

        public string OrganizationalUnit { get; set; } =
            string.Empty;

        public string Description { get; set; } =
            string.Empty;

        public string GroupName { get; set; } =
            string.Empty;

        public string IpAddress { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            "Alle";
    }

    public class ComputerRecord
    {
        public string Name { get; set; } =
            string.Empty;

        public string IpAddress { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public string Description { get; set; } =
            string.Empty;

        public string OrganizationalUnit { get; set; } =
            string.Empty;

        public string MemberOf { get; set; } =
            string.Empty;

        public DateTime? LastLogon { get; set; }

        public string OperatingSystem { get; set; } =
            string.Empty;

        public string DnsHostName { get; set; } =
            string.Empty;

        public string DistinguishedName { get; set; } =
            string.Empty;
    }

    public static class ActiveDirectoryService
    {
        public static List<ComputerRecord> SearchComputers(
            SearchCriteria criteria)
        {
            using LdapConnection connection =
                CreateConnection();

            string baseDn =
                GetDefaultNamingContext(
                    connection);

            string ldapFilter =
                BuildLdapFilter(
                    criteria);

            SearchRequest searchRequest =
                new SearchRequest(
                    baseDn,
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

            searchRequest.SizeLimit =
                5000;

            searchRequest.TimeLimit =
                TimeSpan.FromMinutes(2);

            SearchResponse response =
                (SearchResponse)
                connection.SendRequest(
                    searchRequest);

            List<ComputerRecord> computers =
                new List<ComputerRecord>();

            foreach (SearchResultEntry entry
                     in response.Entries)
            {
                string computerName =
                    GetAttribute(
                        entry,
                        "name");

                string dnsHostName =
                    GetAttribute(
                        entry,
                        "dNSHostName");

                string distinguishedName =
                    GetAttribute(
                        entry,
                        "distinguishedName");

                string userAccountControlValue =
                    GetAttribute(
                        entry,
                        "userAccountControl");

                int.TryParse(
                    userAccountControlValue,
                    out int userAccountControl);

                bool enabled =
                    (userAccountControl & 0x2) == 0;

                string[] groups =
                    GetMultipleAttributes(
                        entry,
                        "memberOf")
                    .Select(
                        GetCommonName)
                    .Where(
                        group =>
                            !string.IsNullOrWhiteSpace(
                                group))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .OrderBy(
                        group =>
                            group)
                    .ToArray();

                string ipAddress =
                    GetIPv4Address(
                        computerName,
                        dnsHostName);

                ComputerRecord computer =
                    new ComputerRecord
                    {
                        Name =
                            computerName,

                        IpAddress =
                            ipAddress,

                        Status =
                            enabled
                                ? "Aktiv"
                                : "Deaktiviert",

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
                            dnsHostName,

                        DistinguishedName =
                            distinguishedName
                    };

                computers.Add(
                    computer);
            }

            IEnumerable<ComputerRecord> filteredComputers =
                computers;

            if (!string.IsNullOrWhiteSpace(
                    criteria.OrganizationalUnit))
            {
                filteredComputers =
                    filteredComputers.Where(
                        computer =>
                            computer
                                .OrganizationalUnit
                                .Contains(
                                    criteria.OrganizationalUnit,
                                    StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(
                    criteria.GroupName))
            {
                filteredComputers =
                    filteredComputers.Where(
                        computer =>
                            computer
                                .MemberOf
                                .Contains(
                                    criteria.GroupName,
                                    StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(
                    criteria.IpAddress))
            {
                filteredComputers =
                    filteredComputers.Where(
                        computer =>
                            computer
                                .IpAddress
                                .Contains(
                                    criteria.IpAddress,
                                    StringComparison.OrdinalIgnoreCase));
            }

            return filteredComputers
                .OrderBy(
                    computer =>
                        computer.Name)
                .ToList();
        }

        private static LdapConnection CreateConnection()
        {
            string domainName =
                IPGlobalProperties
                    .GetIPGlobalProperties()
                    .DomainName;

            if (string.IsNullOrWhiteSpace(
                    domainName))
            {
                throw new InvalidOperationException(
                    "Es konnte keine Windows-Domäne ermittelt werden. " +
                    "Der Rechner muss Mitglied der Domäne sein oder " +
                    "über VPN Zugriff auf die Domäne haben.");
            }

            LdapDirectoryIdentifier identifier =
                new LdapDirectoryIdentifier(
                    domainName,
                    389,
                    false,
                    false);

            LdapConnection connection =
                new LdapConnection(
                    identifier)
                {
                    AuthType =
                        AuthType.Negotiate,

                    Credential =
                        CredentialCache.DefaultNetworkCredentials,

                    Timeout =
                        TimeSpan.FromSeconds(30)
                };

            connection
                .SessionOptions
                .ProtocolVersion = 3;

            connection
                .SessionOptions
                .Signing = true;

            connection
                .SessionOptions
                .Sealing = true;

            connection.Bind();

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
                connection.SendRequest(
                    request);

            if (response.Entries.Count == 0)
            {
                throw new InvalidOperationException(
                    "Der LDAP-Server hat keinen Eintrag für " +
                    "defaultNamingContext zurückgegeben.");
            }

            string defaultNamingContext =
                GetAttribute(
                    response.Entries[0],
                    "defaultNamingContext");

            if (string.IsNullOrWhiteSpace(
                    defaultNamingContext))
            {
                throw new InvalidOperationException(
                    "Der LDAP-Basispfad der Domäne konnte " +
                    "nicht ermittelt werden.");
            }

            return defaultNamingContext;
        }

        private static string BuildLdapFilter(
            SearchCriteria criteria)
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

            if (criteria.Status == "Aktiv")
            {
                filters.Add(
                    "(!(userAccountControl:" +
                    "1.2.840.113556.1.4.803:=2))");
            }
            else if (criteria.Status == "Deaktiviert")
            {
                filters.Add(
                    "(userAccountControl:" +
                    "1.2.840.113556.1.4.803:=2)");
            }

            return $"(&{string.Concat(filters)})";
        }

        private static string GetIPv4Address(
            string computerName,
            string dnsHostName)
        {
            string hostName =
                !string.IsNullOrWhiteSpace(
                    dnsHostName)
                    ? dnsHostName
                    : computerName;

            if (string.IsNullOrWhiteSpace(
                    hostName))
            {
                return string.Empty;
            }

            try
            {
                IPAddress? ipv4Address =
                    Dns.GetHostAddresses(
                            hostName)
                        .FirstOrDefault(
                            address =>
                                address.AddressFamily ==
                                AddressFamily.InterNetwork);

                return ipv4Address?.ToString() ??
                       string.Empty;
            }
            catch
            {
                return string.Empty;
            }
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

            object? value =
                attribute[0];

            if (value is byte[] bytes)
            {
                return System.Text.Encoding.UTF8
                    .GetString(
                        bytes);
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
                .GetValues(
                    typeof(string))
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
                SplitDistinguishedName(
                        distinguishedName)
                    .Where(
                        part =>
                            part.StartsWith(
                                "OU=",
                                StringComparison.OrdinalIgnoreCase))
                    .Select(
                        part =>
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
                SplitDistinguishedName(
                        distinguishedName)
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

        private static IEnumerable<string>
            SplitDistinguishedName(
                string distinguishedName)
        {
            List<string> parts =
                new List<string>();

            System.Text.StringBuilder current =
                new System.Text.StringBuilder();

            bool escaped = false;

            foreach (char character
                     in distinguishedName)
            {
                if (escaped)
                {
                    current.Append(
                        character);

                    escaped = false;
                    continue;
                }

                if (character == '\\')
                {
                    current.Append(
                        character);

                    escaped = true;
                    continue;
                }

                if (character == ',')
                {
                    parts.Add(
                        current
                            .ToString()
                            .Trim());

                    current.Clear();

                    continue;
                }

                current.Append(
                    character);
            }

            if (current.Length > 0)
            {
                parts.Add(
                    current
                        .ToString()
                        .Trim());
            }

            return parts;
        }

        private static string
            UnescapeDistinguishedNameValue(
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
                    out long fileTime))
            {
                return null;
            }

            if (fileTime <= 0)
            {
                return null;
            }

            try
            {
                return DateTime
                    .FromFileTimeUtc(
                        fileTime)
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
    }
}