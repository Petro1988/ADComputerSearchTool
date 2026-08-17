using ADComputerSearchTool.Models;

namespace ADComputerSearchTool.Services.Interfaces;

public interface IActiveDirectoryService
{
    Task<IReadOnlyList<ComputerRecord>> SearchComputersAsync(
        ComputerSearchCriteria criteria,
        CancellationToken cancellationToken = default);
}