using ADComputerSearchTool.Services.Interfaces;
using Microsoft.Win32;

namespace ADComputerSearchTool.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? SelectExcelSavePath()
    {
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

        return dialog.ShowDialog() == true
            ? dialog.FileName
            : null;
    }
}