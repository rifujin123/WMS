using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IWorkingCapitalRadarService
{
    Task<WorkingCapitalRadarDto> GetRadarDataAsync(Guid? warehouseId = null, CancellationToken cancellationToken = default);
}
