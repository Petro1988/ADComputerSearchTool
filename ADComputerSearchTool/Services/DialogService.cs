using ADComputerSearchTool.Services.Interfaces;
using System.Windows;

namespace ADComputerSearchTool.Services;

public sealed class DialogService : IDialogService
{
    public void ShowInformation(
        string message,
        string title)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    public void ShowWarning(
        string message,
        string title)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    public void ShowError(
        string message,
        string title)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}