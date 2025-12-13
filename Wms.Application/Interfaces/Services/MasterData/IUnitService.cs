using Wms.Application.DTOs.MasterData.Units;

namespace Wms.Application.Interfaces.Services.MasterData;

public interface IUnitService
{
    Task<int> CreateAsync(CreateUnitDto dto);
    Task UpdateAsync(int id, UpdateUnitDto dto);
    Task DeleteAsync(int id);
    Task<UnitDto> GetAsync(int id);
    Task<List<UnitDto>> GetAllAsync();
}
