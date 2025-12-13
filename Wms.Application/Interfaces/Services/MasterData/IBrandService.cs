using Wms.Application.DTOs.MasterData.Brands;

namespace Wms.Application.Interfaces.Services.MasterData;

public interface IBrandService
{
    Task<int> CreateAsync(CreateBrandDto dto);
    Task UpdateAsync(int id, UpdateBrandDto dto);
    Task DeleteAsync(int id);
    Task<BrandDto> GetAsync(int id);
    Task<List<BrandDto>> GetAllAsync();
}
