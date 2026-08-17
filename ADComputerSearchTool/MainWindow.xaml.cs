using ADComputerSearchTool.Models;
using ADComputerSearchTool.Services;
using ADComputerSearchTool.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ADComputerSearchTool;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ComputerRecord> _results =
        new();

    private readonly IActiveDirectoryService
        _activeDirectoryService;

    private readonly IClipboardService
        _clipboardService;

    private readonly IExcelExportService
        _excelExportService;

    private readonly IFileDialogService
        _fileDialogService;

    public MainWindow()
    {
        InitializeComponent();

        IIpAddressService ipAddressService =
            new DnsIpAddressService();

        _activeDirectoryService =
            new ActiveDirectoryService(
                ipAddressService);

        _clipboardService =
            new ClipboardService();

        _excelExportService =
            new ExcelExportService();

        _fileDialogService =
            new FileDialogService();

        ResultsGrid.ItemsSource =
            _results;
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
            ComputerSearchCriteria criteria =
                CreateSearchCriteria();

            IReadOnlyList<ComputerRecord> computers =
                await _activeDirectoryService
                    .SearchComputersAsync(criteria);

            UpdateResults(computers);

            InfoText.Text =
                "Suche abgeschlossen.";
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

    private ComputerSearchCriteria CreateSearchCriteria()
    {
        return new ComputerSearchCriteria
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
                GetSelectedStatusFilter()
        };
    }

    private ComputerStatusFilter GetSelectedStatusFilter()
    {
        string selectedStatus =
            StatusBox.SelectedItem is ComboBoxItem selectedItem
                ? selectedItem.Content?.ToString() ?? "Alle"
                : "Alle";

        return selectedStatus switch
        {
            "Aktiv" =>
                ComputerStatusFilter.Enabled,

            "Deaktiviert" =>
                ComputerStatusFilter.Disabled,

            _ =>
                ComputerStatusFilter.All
        };
    }

    private void UpdateResults(
        IEnumerable<ComputerRecord> computers)
    {
        _results.Clear();

        foreach (ComputerRecord computer in computers)
        {
            _results.Add(computer);
        }

        ResultsGrid.SelectedItem = null;
        ResultsGrid.UnselectAllCells();

        CountText.Text =
            $"{_results.Count} Treffer";

        ExportButton.IsEnabled =
            _results.Count > 0;
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

        CountText.Text =
            "0 Treffer";

        InfoText.Text =
            "Bereit";

        ExportButton.IsEnabled =
            false;
    }

    private void ResultsGrid_PreviewMouseRightButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        DependencyObject? source =
            e.OriginalSource as DependencyObject;

        DataGridCell? clickedCell =
            FindParent<DataGridCell>(source);

        if (clickedCell == null)
        {
            return;
        }

        DataGridRow? clickedRow =
            FindParent<DataGridRow>(clickedCell);

        if (clickedRow?.Item is not ComputerRecord computer)
        {
            return;
        }

        ResultsGrid.CurrentCell =
            new DataGridCellInfo(
                computer,
                clickedCell.Column);

        ResultsGrid.UnselectAllCells();

        ResultsGrid.SelectedCells.Add(
            new DataGridCellInfo(
                computer,
                clickedCell.Column));

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

    private void CopyRowMenuItem_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ResultsGrid.CurrentCell.Item is not ComputerRecord computer)
        {
            MessageBox.Show(
                "Bitte mit der rechten Maustaste auf eine Tabellenzelle klicken.",
                "Zeile kopieren",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        try
        {
            _clipboardService.CopyComputerRow(
                computer);

            InfoText.Text =
                $"Die vollständige Zeile von {computer.Name} wurde kopiert.";
        }
        catch (Exception ex)
        {
            ShowClipboardError(ex);
        }
    }

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

        try
        {
            _clipboardService.CopyText(
                cellValue);

            InfoText.Text =
                $"{columnHeader} wurde kopiert.";
        }
        catch (Exception ex)
        {
            ShowClipboardError(ex);
        }
    }

    private static string GetCellValue(
        DataGridColumn column,
        ComputerRecord computer)
    {
        if (column is not DataGridBoundColumn boundColumn ||
            boundColumn.Binding is not Binding binding)
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

        return value switch
        {
            null =>
                string.Empty,

            DateTime dateTime =>
                dateTime.ToString(
                    "dd.MM.yyyy HH:mm"),

            _ =>
                value.ToString() ??
                string.Empty
        };
    }

    private void ShowClipboardError(
        Exception exception)
    {
        MessageBox.Show(
            "Die Daten konnten nicht kopiert werden:\n\n" +
            exception.Message,
            "Fehler beim Kopieren",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
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

        string? filePath =
            _fileDialogService
                .SelectExcelSavePath();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            _excelExportService.Export(
                filePath,
                _results);

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