using Wms.Application.DTOs.MasterData.Suppliers;

namespace Wms.Application.Interfaces.Services.MasterData;

public interface ISupplierService
{
    Task<int> CreateAsync(CreateSupplierDto dto);
    Task UpdateAsync(int id, UpdateSupplierDto dto);
    Task DeleteAsync(int id);
    Task<SupplierDto> GetAsync(int id);
    Task<List<SupplierDto>> GetAllAsync();
}
