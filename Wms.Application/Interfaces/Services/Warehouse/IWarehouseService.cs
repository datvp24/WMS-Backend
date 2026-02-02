using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wms.Application.DTOS.Warehouse;
using Wms.Domain.Entity.Warehouses;


namespace Wms.Application.Interfaces.Services.Warehouse
{
    public interface IWarehouseService
    {
        Task<WarehouseDto> CreateAsync(WarehouseCreateDto dto);
        Task<WarehouseDto> UpdateAsync(WarehouseUpdateDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<WarehouseDto> GetByIdAsync(Guid id);
        Task<(IEnumerable<WarehouseDto> Items, int Total)> QueryAsync(int page, int pageSize, string q, string sortBy, bool asc);
        Task LockAsync(Guid id, string reason = null);
        Task<List<Wms.Domain.Entity.Warehouses.Warehouse>> GetWarehousesByProduct(WarehousesbyProduct dto);
        Task UnlockAsync(Guid id);


        // Locations
        Task<LocationDto> CreateLocationAsync(LocationCreateDto dto);
        Task<LocationDto> UpdateLocationAsync(LocationUpdateDto dto);
        Task<Guid> GetReceivingLocationId(Guid warehouseId);
        Task<Location> GetIssuedLocationId(Guid warehouseId);

        Task<bool> DeleteLocationAsync(Guid id);
        Task<IEnumerable<LocationDto>> GetLocationsByWarehouseAsync(Guid warehouseId);
        Task<LocationDto> GetLocationByIdAsync(Guid id);
        Task<List<Wms.Domain.Entity.Warehouses.Warehouse>> GetByWarehouseType(WarehousesbyTypeDto dto);
    }
}