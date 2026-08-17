using ADComputerSearchTool.Models;
using ADComputerSearchTool.Services;
using ADComputerSearchTool.Services.Interfaces;
using ADComputerSearchTool.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ADComputerSearchTool;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        IIpAddressService ipAddressService =
            new DnsIpAddressService();

        IActiveDirectoryService activeDirectoryService =
            new ActiveDirectoryService(
                ipAddressService);

        IClipboardService clipboardService =
            new ClipboardService();

        IExcelExportService excelExportService =
            new ExcelExportService();

        IFileDialogService fileDialogService =
            new FileDialogService();

        _viewModel =
            new MainWindowViewModel(
                activeDirectoryService,
                clipboardService,
                excelExportService,
                fileDialogService);

        DataContext =
            _viewModel;
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

        _viewModel.SelectedComputer =
            computer;

        clickedCell.Focus();
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

        if (_viewModel.CopyRowCommand.CanExecute(computer))
        {
            _viewModel.CopyRowCommand.Execute(computer);
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
            _viewModel.CopyCellValue(
                cellValue,
                columnHeader);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                "Die Daten konnten nicht kopiert werden:\n\n" +
                exception.Message,
                "Fehler beim Kopieren",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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
}