using ADComputerSearchTool.Helpers;
using ADComputerSearchTool.Models;
using ADComputerSearchTool.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

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
    /// Wählt beim Rechtsklick die angeklickte Zelle und
    /// den dazugehörigen Computer aus.
    /// </summary>
    private void ResultsGrid_PreviewMouseRightButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        DependencyObject? source =
            e.OriginalSource as DependencyObject;

        DataGridCell? clickedCell =
            source.FindParent<DataGridCell>();

        if (clickedCell == null)
        {
            return;
        }

        DataGridRow? clickedRow =
            clickedCell.FindParent<DataGridRow>();

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
    /// Kopiert die vollständige Zeile des Computers.
    /// Der Rechtsklick auf eine einzelne Zelle reicht aus.
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
    /// Kopiert ausschließlich den Wert der mit der
    /// rechten Maustaste angeklickten Zelle.
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
    /// Ermittelt über das Binding der DataGrid-Spalte
    /// den Wert der ausgewählten Zelle.
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
}