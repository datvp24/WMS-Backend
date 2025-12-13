using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOs.MasterData.Brands;
using Wms.Application.Interfaces.Services.MasterData;
using Wms.Domain.Entity.MasterData;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Application.Services.MasterData;

public class BrandService : IBrandService
{
    private readonly AppDbContext _db;

    public BrandService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(CreateBrandDto dto)
    {
        string finalCode = dto.Code;

        // Tự sinh code nếu FE không nhập
        if (string.IsNullOrWhiteSpace(dto.Code))
            finalCode = await GenerateCodeAsync();

        // Kiểm tra Code trùng
        if (await _db.Brands.AnyAsync(x => x.Code == finalCode))
            throw new Exception("Code already exists");

        // Kiểm tra Name trùng
        if (await _db.Brands.AnyAsync(x => x.Name == dto.Name))
            throw new Exception("Name already exists");

        var brand = new Brand
        {
            Code = finalCode,
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Brands.Add(brand);
        await _db.SaveChangesAsync();

        return brand.Id;
    }

    private async Task<string> GenerateCodeAsync()
    {
        int count = await _db.Brands.CountAsync();
        return $"BR{(count + 1).ToString("D3")}";
    }
    public async Task UpdateAsync(int id, UpdateBrandDto dto)
    {
        var brand = await _db.Brands.FindAsync(id)
            ?? throw new Exception("Brand not found");

        // Check Code duplicate (excluding current)
        if (await _db.Brands.AnyAsync(x => x.Code == dto.Code && x.Id != id))
            throw new Exception("Code already exists");

        // Check Name duplicate (excluding current)
        if (await _db.Brands.AnyAsync(x => x.Name == dto.Name && x.Id != id))
            throw new Exception("Name already exists");

        brand.Code = dto.Code;
        brand.Name = dto.Name;
        brand.Description = dto.Description;
        brand.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
    }


    public async Task DeleteAsync(int id)
    {
        var brand = await _db.Brands.FindAsync(id)
            ?? throw new Exception("Brand not found");

        _db.Brands.Remove(brand);
        await _db.SaveChangesAsync();
    }

    public async Task<BrandDto> GetAsync(int id)
    {
        var brand = await _db.Brands.FindAsync(id)
            ?? throw new Exception("Brand not found");

        return new BrandDto
        {
            Id = brand.Id,
            Code = brand.Code,
            Name = brand.Name,
            IsActive = brand.IsActive
        };
    }

    public async Task<List<BrandDto>> GetAllAsync()
    {
        return await _db.Brands
            .Select(x => new BrandDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }
}
