using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOs.MasterData.Products;
using Wms.Application.Interfaces.Services.MasterData;
using Wms.Domain.Entity.MasterData;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Application.Services.MasterData;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(CreateProductDto dto)
    {
        if (await _db.Products.AnyAsync(x => x.Code == dto.Code))
            throw new Exception("Code already exists");

        var product = new Product
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            CategoryId = dto.CategoryId,
            Type = dto.Type,
            UnitId = dto.UnitId,
            BrandId = dto.BrandId,
            SupplierId = dto.SupplierId
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product.Id;
    }

    public async Task UpdateAsync(int id, UpdateProductDto dto)
    {
        var product = await _db.Products.FindAsync(id)
            ?? throw new Exception("Product not found");

        product.Name = dto.Name;
        product.Code = dto.Code;
        product.Type = dto.Type;
        product.Description = dto.Description;
        product.CategoryId = dto.CategoryId;
        product.UnitId = dto.UnitId;
        product.BrandId = dto.BrandId;
        product.SupplierId = dto.SupplierId;
        product.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _db.Products.FindAsync(id)
            ?? throw new Exception("Product not found");

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
    }

    public async Task<ProductDto> GetAsync(int id)
    {
        var p = await _db.Products.FindAsync(id)
            ?? throw new Exception("Product not found");

        return new ProductDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Type = p.Type,
            Description = p.Description,
            CategoryId = p.CategoryId,
            UnitId = p.UnitId,
            BrandId = p.BrandId,
            SupplierId = p.SupplierId,
            IsActive = p.IsActive
        };
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        return await _db.Products
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                CategoryId = p.CategoryId,
                Type = p.Type,
                UnitId = p.UnitId,
                BrandId = p.BrandId,
                SupplierId = p.SupplierId,
                IsActive = p.IsActive
            }).ToListAsync();
    }
    public async Task<List<ProductDto>> GetAllBySupplierAsync(int dto)
    {
        // Where trước Select để tối ưu
        var prodList = await _db.Products
            .Where(p => p.SupplierId == dto)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                CategoryId = p.CategoryId,
                UnitId = p.UnitId,
                Type = p.Type,
                BrandId = p.BrandId,
                SupplierId = p.SupplierId,
                IsActive = p.IsActive
            })
            .ToListAsync();

        // Check list rỗng (không phải null)
        if (prodList.Count == 0) // hoặc: !prodList.Any()
        {
            throw new KeyNotFoundException($"Không tìm thấy sản phẩm của nhà cung cấp ID: {dto}");
        }

        return prodList;
    }
    public async Task<List<ProductDto>> GetAllByType(ProductTypeDto dto)
    {
        var prodList = await _db.Products
            .Where(p => p.Type == dto.Type)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                CategoryId = p.CategoryId,
                UnitId = p.UnitId,
                Type = p.Type,
                BrandId = p.BrandId,
                SupplierId = p.SupplierId,
                IsActive = p.IsActive
            })
            .ToListAsync();

        // Check list rỗng (không phải null)
        if (prodList.Count == 0) // hoặc: !prodList.Any()
        {
            throw new KeyNotFoundException($"Không tìm thấy sản phẩm thuộc loại: {dto.Type}");
        }

        return prodList;
    }
    public async Task<List<ProductDto>> FilterAsync(ProductFilterDto f)
    {
        var q = _db.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(f.Keyword))
            q = q.Where(x =>
                x.Name.Contains(f.Keyword) ||
                x.Code.Contains(f.Keyword));

        if (f.CategoryId.HasValue)
            q = q.Where(x => x.CategoryId == f.CategoryId);

        if (f.BrandId.HasValue)
            q = q.Where(x => x.BrandId == f.BrandId);

        if (f.SupplierId.HasValue)
            q = q.Where(x => x.SupplierId == f.SupplierId);

        return await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((f.Page - 1) * f.PageSize)
            .Take(f.PageSize)
            .Select(x => new ProductDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                CategoryId = x.CategoryId,
                UnitId = x.UnitId,
                BrandId = x.BrandId,
                Type = x.Type,
                SupplierId = x.SupplierId,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }
}
