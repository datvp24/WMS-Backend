using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOs.MasterData.Units;
using Wms.Application.Interfaces.Services.MasterData;
using Wms.Domain.Entity.MasterData;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Application.Services.MasterData;

public class UnitService : IUnitService
{
    private readonly AppDbContext _db;

    public UnitService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(CreateUnitDto dto)
    {
        if (await _db.Units.AnyAsync(x => x.Code == dto.Code))
            throw new Exception("Code already exists");

        if (await _db.Units.AnyAsync(x => x.Name == dto.Name))
            throw new Exception("Name already exists");

        var unit = new Unit
        {
            Code = dto.Code,
            Name = dto.Name
        };

        _db.Units.Add(unit);
        await _db.SaveChangesAsync();

        return unit.Id;
    }

    public async Task UpdateAsync(int id, UpdateUnitDto dto)
    {
        var unit = await _db.Units.FindAsync(id)
            ?? throw new Exception("Unit not found");

        unit.Name = dto.Name;
        unit.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var unit = await _db.Units.FindAsync(id)
            ?? throw new Exception("Unit not found");

        _db.Units.Remove(unit);
        await _db.SaveChangesAsync();
    }

    public async Task<UnitDto> GetAsync(int id)
    {
        var unit = await _db.Units.FindAsync(id)
            ?? throw new Exception("Unit not found");

        return new UnitDto
        {
            Id = unit.Id,
            Code = unit.Code,
            Name = unit.Name,
            IsActive = unit.IsActive
        };
    }

    public async Task<List<UnitDto>> GetAllAsync()
    {
        return await _db.Units
            .Select(x => new UnitDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive,
                CreateAt = x.CreatedAt
            }).ToListAsync();
    }
}
