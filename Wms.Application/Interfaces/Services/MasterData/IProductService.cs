using Wms.Application.DTOs.MasterData.Products;

namespace Wms.Application.Interfaces.Services.MasterData;

public interface IProductService
{
    Task<int> CreateAsync(CreateProductDto dto);
    Task UpdateAsync(int id, UpdateProductDto dto);
    Task DeleteAsync(int id);
    Task<ProductDto> GetAsync(int id);
    Task<List<ProductDto>> GetAllBySupplierAsync(int dto);
    Task<List<ProductDto>> GetAllByType(ProductTypeDto dto);

    Task<List<ProductDto>> GetAllAsync();
    Task<List<ProductDto>> FilterAsync(ProductFilterDto filter);
}
