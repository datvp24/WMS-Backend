using Wms.Application.DTOs.MasterData.Customers;

namespace Wms.Application.Interfaces.Services.MasterData;

public interface ICustomerService
{
    Task<int> CreateAsync(CreateCustomerDto dto);
    Task UpdateAsync(int id, UpdateCustomerDto dto);
    Task DeleteAsync(int id);
    Task<CustomerDto> GetAsync(int id);
    Task<List<CustomerDto>> GetAllAsync();
}
