using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOs.MasterData.Suppliers;
using Wms.Application.Interfaces.Services.MasterData;
using Wms.Domain.Entity.MasterData;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Application.Services.MasterData;

public class SupplierService : ISupplierService
{
    private readonly AppDbContext _db;

    public SupplierService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(CreateSupplierDto dto)
    {
        if (await _db.Suppliers.AnyAsync(x => x.Code == dto.Code))
            throw new Exception("Code already exists");

        if (await _db.Suppliers.AnyAsync(x => x.Name == dto.Name))
            throw new Exception("Name already exists");

        var supplier = new Supplier
        {
            Code = dto.Code,
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address
            
        };

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();

        return supplier.Id;
    }

    public async Task UpdateAsync(int id, UpdateSupplierDto dto)
    {
        var supplier = await _db.Suppliers.FindAsync(id)
            ?? throw new Exception("Supplier not found");

        supplier.Name = dto.Name;
        supplier.Email = dto.Email;
        supplier.Phone = dto.Phone;
        supplier.Address = dto.Address;
        supplier.IsActive = dto.IsActive;
        supplier.Code = dto.Code;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var supplier = await _db.Suppliers.FindAsync(id)
            ?? throw new Exception("Supplier not found");

        _db.Suppliers.Remove(supplier);
        await _db.SaveChangesAsync();
    }

    public async Task<SupplierDto> GetAsync(int id)
    {
        var supplier = await _db.Suppliers.FindAsync(id)
            ?? throw new Exception("Supplier not found");

        return new SupplierDto
        {
            Id = supplier.Id,
            Code = supplier.Code,
            Name = supplier.Name,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address,
            IsActive = supplier.IsActive
        };
    }

    public async Task<List<SupplierDto>> GetAllAsync()
    {
        return await _db.Suppliers
            .Select(x => new SupplierDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Email = x.Email,
                Phone = x.Phone,
                Address = x.Address,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }
}
