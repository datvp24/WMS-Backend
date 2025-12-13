using Wms.Application.DTOs.MasterData.Categories;

namespace Wms.Application.Interfaces.Services.MasterData;

public interface ICategoryService
{
    Task<int> CreateAsync(CreateCategoryDto dto);
    Task UpdateAsync(int id, UpdateCategoryDto dto);
    Task DeleteAsync(int id);
    Task<CategoryDto> GetAsync(int id);
    Task<List<CategoryDto>> GetAllAsync();
}
