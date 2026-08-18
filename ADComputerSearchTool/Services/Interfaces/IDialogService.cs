namespace ADComputerSearchTool.Services.Interfaces;

public interface IDialogService
{
    void ShowInformation(
        string message,
        string title);

    void ShowWarning(
        string message,
        string title);

    void ShowError(
        string message,
        string title);
}