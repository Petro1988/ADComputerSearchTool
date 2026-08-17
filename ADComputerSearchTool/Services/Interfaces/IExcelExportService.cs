using ADComputerSearchTool.Models;

namespace ADComputerSearchTool.Services.Interfaces;

public interface IExcelExportService
{
    void Export(
        string filePath,
        IEnumerable<ComputerRecord> computers);
}