using ADComputerSearchTool.Helpers;
using ADComputerSearchTool.Models;
using ADComputerSearchTool.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ADComputerSearchTool.Views;

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

    private void CopyRowMenuItem_Click(
        object sender,
        RoutedEventArgs e)
    {
        ComputerRecord? computer =
            ResultsGrid.CurrentCell.Item as ComputerRecord ??
            _viewModel.SelectedComputer;

        if (computer == null)
        {
            _viewModel.ShowNoRowSelectedMessage();
            return;
        }

        if (_viewModel.CopyRowCommand.CanExecute(computer))
        {
            _viewModel.CopyRowCommand.Execute(
                computer);
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
            _viewModel.ShowNoCellSelectedMessage();
            return;
        }

        string cellValue =
            GetCellValue(
                currentCell.Column,
                computer);

        string columnHeader =
            currentCell.Column.Header?.ToString() ??
            "Zelle";

        _viewModel.CopyCellValue(
            cellValue,
            columnHeader);
    }

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