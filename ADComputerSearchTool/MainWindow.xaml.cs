using ADComputerSearchTool.Models;
using ADComputerSearchTool.ViewModels;
using System;
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

    public MainWindow(
        MainWindowViewModel viewModel)
    {
        InitializeComponent();

        _viewModel =
            viewModel ??
            throw new ArgumentNullException(
                nameof(viewModel));

        DataContext =
            _viewModel;
    }

    /// <summary>
    /// Speichert beim Rechtsklick die angeklickte Tabellenzelle
    /// und den zugehörigen Computer.
    ///
    /// Dadurch reicht der Rechtsklick auf eine einzelne Zelle aus,
    /// um anschließend die vollständige Zeile zu kopieren.
    /// </summary>
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

        DataGridCellInfo selectedCell =
            new DataGridCellInfo(
                computer,
                clickedCell.Column);

        ResultsGrid.CurrentCell =
            selectedCell;

        ResultsGrid.UnselectAllCells();

        ResultsGrid.SelectedCells.Add(
            selectedCell);

        ResultsGrid.SelectedItem =
            computer;

        _viewModel.SelectedComputer =
            computer;

        clickedCell.Focus();
    }

    /// <summary>
    /// Kopiert die vollständigen Informationen des Computers,
    /// zu dem die angeklickte Zelle gehört.
    /// </summary>
    private void CopyRowMenuItem_Click(
        object sender,
        RoutedEventArgs e)
    {
        ComputerRecord? computer =
            ResultsGrid.CurrentCell.Item as ComputerRecord ??
            _viewModel.SelectedComputer;

        if (computer == null)
        {
            MessageBox.Show(
                "Bitte mit der rechten Maustaste auf eine Tabellenzelle klicken.",
                "Zeile kopieren",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        if (!_viewModel.CopyRowCommand.CanExecute(computer))
        {
            return;
        }

        _viewModel.CopyRowCommand.Execute(
            computer);
    }

    /// <summary>
    /// Kopiert nur den Inhalt der mit der rechten
    /// Maustaste angeklickten Tabellenzelle.
    /// </summary>
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

    /// <summary>
    /// Liest anhand des DataGrid-Bindings den Wert
    /// der aktuell ausgewählten Zelle aus.
    /// </summary>
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

    /// <summary>
    /// Sucht ausgehend von einem angeklickten WPF-Element
    /// nach einem übergeordneten Element des angegebenen Typs.
    /// </summary>
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
                    VisualTreeHelper.GetParent(
                        current);
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