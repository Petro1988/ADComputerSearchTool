using ADComputerSearchTool.Models;

namespace ADComputerSearchTool.Services.Interfaces;

public interface IClipboardService
{
    void CopyText(string text);

    void CopyComputerRow(
        ComputerRecord computer);
}