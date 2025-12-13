using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOs.MasterData.Customers;
using Wms.Application.Interfaces.Services.MasterData;
using Wms.Domain.Entity.MasterData;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Application.Services.MasterData;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;

    public CustomerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(CreateCustomerDto dto)
    {
        if (await _db.Customers.AnyAsync(x => x.Code == dto.Code))
            throw new Exception("Code already exists");

        if (await _db.Customers.AnyAsync(x => x.Name == dto.Name))
            throw new Exception("Name already exists");

        var customer = new Customer
        {
            Code = dto.Code,
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        return customer.Id;
    }

    public async Task UpdateAsync(int id, UpdateCustomerDto dto)
    {
        var customer = await _db.Customers.FindAsync(id)
            ?? throw new Exception("Customer not found");

        customer.Name = dto.Name;
        customer.Email = dto.Email;
        customer.Phone = dto.Phone;
        customer.Address = dto.Address;
        customer.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await _db.Customers.FindAsync(id)
            ?? throw new Exception("Customer not found");

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();
    }

    public async Task<CustomerDto> GetAsync(int id)
    {
        var customer = await _db.Customers.FindAsync(id)
            ?? throw new Exception("Customer not found");

        return new CustomerDto
        {
            Id = customer.Id,
            Code = customer.Code,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone,
            Address = customer.Address,
            IsActive = customer.IsActive
        };
    }

    public async Task<List<CustomerDto>> GetAllAsync()
    {
        return await _db.Customers
            .Select(x => new CustomerDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Email = x.Email,
                Phone = x.Phone,
                Address = x.Address,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }
}
