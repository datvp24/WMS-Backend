using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOs.MasterData.Categories;
using Wms.Application.Interfaces.Services.MasterData;
using Wms.Domain.Entity.MasterData;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Application.Services.MasterData;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(CreateCategoryDto dto)
    {
        if (await _db.Categories.AnyAsync(x => x.Code == dto.Code))
            throw new Exception("Code already exists");

        if (await _db.Categories.AnyAsync(x => x.Name == dto.Name))
            throw new Exception("Name already exists");

        var cat = new Category
        {
            Code = dto.Code,
            Name = dto.Name
        };

        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();

        return cat.Id;
    }

    public async Task UpdateAsync(int id, UpdateCategoryDto dto)
    {
        var cat = await _db.Categories.FindAsync(id)
            ?? throw new Exception("Category not found");

        cat.Name = dto.Name;
        cat.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var cat = await _db.Categories.FindAsync(id)
            ?? throw new Exception("Category not found");

        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
    }

    public async Task<CategoryDto> GetAsync(int id)
    {
        var cat = await _db.Categories.FindAsync(id)
            ?? throw new Exception("Category not found");

        return new CategoryDto
        {
            Id = cat.Id,
            Code = cat.Code,
            Name = cat.Name,
            IsActive = cat.IsActive
        };
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        return await _db.Categories
            .Select(x => new CategoryDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive,
                CreateAt = x.CreatedAt
            })
            .ToListAsync();
    }
}
