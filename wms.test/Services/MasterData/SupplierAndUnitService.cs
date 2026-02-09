using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wms.Application.Services.MasterData;
using Wms.Application.DTOs.MasterData.Suppliers;
using Wms.Application.DTOs.MasterData.Units;
using Wms.Domain.Entity.MasterData;
using Wms.Infrastructure.Persistence.Context;
using System;
using System.Threading.Tasks;

namespace Wms.Tests.Services.MasterData
{
    #region SupplierService Tests

    public class SupplierServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly SupplierService _supplierService;

        public SupplierServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _supplierService = new SupplierService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Case 21: Quản lý nhà cung cấp

        [Fact]
        public async Task CreateAsync_ValidData_CreatesSupplierWithContactInfo()
        {
            // Arrange
            var dto = new CreateSupplierDto
            {
                Code = "NCC01",
                Name = "Công ty A",
                Email = "contact@company-a.com",
                Phone = "0123456789",
                Address = "123 Business Street, City"
            };

            // Act
            var result = await _supplierService.CreateAsync(dto);

            // Assert
            result.Should().BeGreaterThan(0);
            var supplier = await _context.Suppliers.FindAsync(result);
            supplier.Should().NotBeNull();
            supplier!.Code.Should().Be("NCC01");
            supplier.Name.Should().Be("Công ty A");
            supplier.Email.Should().Be("contact@company-a.com");
            supplier.Phone.Should().Be("0123456789");
            supplier.Address.Should().Be("123 Business Street, City");
        }

        [Fact]
        public async Task CreateAsync_DuplicateCode_ThrowsException()
        {
            // Arrange
            _context.Suppliers.Add(new Supplier
            {
                Code = "NCC01",
                Name = "Existing Supplier"
            });
            await _context.SaveChangesAsync();

            var dto = new CreateSupplierDto
            {
                Code = "NCC01",
                Name = "New Supplier"
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _supplierService.CreateAsync(dto)
            ).ContinueWith(t => t.Result.Message.Should().Be("Code already exists"));
        }

        [Fact]
        public async Task CreateAsync_DuplicateName_ThrowsException()
        {
            // Arrange
            _context.Suppliers.Add(new Supplier
            {
                Code = "NCC01",
                Name = "Công ty A"
            });
            await _context.SaveChangesAsync();

            var dto = new CreateSupplierDto
            {
                Code = "NCC02",
                Name = "Công ty A"
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _supplierService.CreateAsync(dto)
            ).ContinueWith(t => t.Result.Message.Should().Be("Name already exists"));
        }

        [Fact]
        public async Task UpdateAsync_ValidData_UpdatesSupplier()
        {
            // Arrange
            var supplier = new Supplier
            {
                Code = "NCC01",
                Name = "Old Name",
                Email = "old@test.com",
                Phone = "111",
                Address = "Old Address",
                IsActive = true
            };
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            var dto = new UpdateSupplierDto
            {
                Code = "NCC01-NEW",
                Name = "New Name",
                Email = "new@test.com",
                Phone = "222",
                Address = "New Address",
                IsActive = false
            };

            // Act
            await _supplierService.UpdateAsync(supplier.Id, dto);

            // Assert
            var updated = await _context.Suppliers.FindAsync(supplier.Id);
            updated!.Code.Should().Be("NCC01-NEW");
            updated.Name.Should().Be("New Name");
            updated.Email.Should().Be("new@test.com");
            updated.Phone.Should().Be("222");
            updated.Address.Should().Be("New Address");
            updated.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_ValidId_DeletesSupplier()
        {
            // Arrange
            var supplier = new Supplier
            {
                Code = "NCC01",
                Name = "Test Supplier"
            };
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            // Act
            await _supplierService.DeleteAsync(supplier.Id);

            // Assert
            var deleted = await _context.Suppliers.FindAsync(supplier.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_InvalidId_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _supplierService.DeleteAsync(999)
            ).ContinueWith(t => t.Result.Message.Should().Be("Supplier not found"));
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllSuppliersWithCreatedDate()
        {
            // Arrange
            var now = DateTime.UtcNow;
            _context.Suppliers.AddRange(
                new Supplier
                {
                    Code = "NCC01",
                    Name = "Supplier 1",
                    Email = "s1@test.com",
                    CreatedAt = now.AddDays(-2)
                },
                new Supplier
                {
                    Code = "NCC02",
                    Name = "Supplier 2",
                    Email = "s2@test.com",
                    CreatedAt = now.AddDays(-1)
                }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _supplierService.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(s => s.CreatedAt.Should().NotBe(default(DateTime)));
            result.Should().Contain(s => s.Code == "NCC01");
            result.Should().Contain(s => s.Code == "NCC02");
        }

        [Fact]
        public async Task GetAsync_ValidId_ReturnsSupplierDto()
        {
            // Arrange
            var supplier = new Supplier
            {
                Code = "NCC01",
                Name = "Test Supplier",
                Email = "test@supplier.com",
                Phone = "0123456789",
                Address = "Test Address",
                IsActive = true
            };
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            // Act
            var result = await _supplierService.GetAsync(supplier.Id);

            // Assert
            result.Should().NotBeNull();
            result.Code.Should().Be("NCC01");
            result.Name.Should().Be("Test Supplier");
            result.Email.Should().Be("test@supplier.com");
            result.IsActive.Should().BeTrue();
        }

        #endregion
    }

    #endregion

    #region UnitService Tests

    public class UnitServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly UnitService _unitService;

        public UnitServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _unitService = new UnitService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Case 22: Quản lý đơn vị tính

        [Fact]
        public async Task CreateAsync_ValidData_CreatesUnit()
        {
            // Arrange
            var dto = new CreateUnitDto
            {
                Code = "KG",
                Name = "Kilogram"
            };

            // Act
            var result = await _unitService.CreateAsync(dto);

            // Assert
            result.Should().BeGreaterThan(0);
            var unit = await _context.Units.FindAsync(result);
            unit.Should().NotBeNull();
            unit!.Code.Should().Be("KG");
            unit.Name.Should().Be("Kilogram");
        }

        [Fact]
        public async Task CreateAsync_DuplicateCode_ThrowsException()
        {
            // Arrange
            _context.Units.Add(new Unit
            {
                Code = "KG",
                Name = "Kilogram"
            });
            await _context.SaveChangesAsync();

            var dto = new CreateUnitDto
            {
                Code = "KG",
                Name = "Kilograms"
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _unitService.CreateAsync(dto)
            ).ContinueWith(t => t.Result.Message.Should().Be("Code already exists"));
        }

        [Fact]
        public async Task CreateAsync_DuplicateName_ThrowsException()
        {
            // Arrange
            _context.Units.Add(new Unit
            {
                Code = "KG",
                Name = "Kilogram"
            });
            await _context.SaveChangesAsync();

            var dto = new CreateUnitDto
            {
                Code = "KGM",
                Name = "Kilogram"
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _unitService.CreateAsync(dto)
            ).ContinueWith(t => t.Result.Message.Should().Be("Name already exists"));
        }

        [Fact]
        public async Task UpdateAsync_ValidData_UpdatesNameOnly()
        {
            // Arrange
            var unit = new Unit
            {
                Code = "KG",
                Name = "Old Name",
                IsActive = true
            };
            _context.Units.Add(unit);
            await _context.SaveChangesAsync();

            var dto = new UpdateUnitDto
            {
                Name = "Kilogram",
                IsActive = false
            };

            // Act
            await _unitService.UpdateAsync(unit.Id, dto);

            // Assert
            var updated = await _context.Units.FindAsync(unit.Id);
            updated!.Code.Should().Be("KG"); // Code không đổi
            updated.Name.Should().Be("Kilogram");
            updated.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_InvalidId_ThrowsException()
        {
            // Arrange
            var dto = new UpdateUnitDto
            {
                Name = "Test"
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _unitService.UpdateAsync(999, dto)
            ).ContinueWith(t => t.Result.Message.Should().Be("Unit not found"));
        }

        [Fact]
        public async Task DeleteAsync_ValidId_DeletesUnit()
        {
            // Arrange
            var unit = new Unit
            {
                Code = "KG",
                Name = "Kilogram"
            };
            _context.Units.Add(unit);
            await _context.SaveChangesAsync();

            // Act
            await _unitService.DeleteAsync(unit.Id);

            // Assert
            var deleted = await _context.Units.FindAsync(unit.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_InvalidId_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _unitService.DeleteAsync(999)
            ).ContinueWith(t => t.Result.Message.Should().Be("Unit not found"));
        }

        [Fact]
        public async Task GetAsync_ValidId_ReturnsUnitDto()
        {
            // Arrange
            var unit = new Unit
            {
                Code = "KG",
                Name = "Kilogram",
                IsActive = true
            };
            _context.Units.Add(unit);
            await _context.SaveChangesAsync();

            // Act
            var result = await _unitService.GetAsync(unit.Id);

            // Assert
            result.Should().NotBeNull();
            result.Code.Should().Be("KG");
            result.Name.Should().Be("Kilogram");
            result.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllUnitsWithCreatedDate()
        {
            // Arrange
            var now = DateTime.UtcNow;
            _context.Units.AddRange(
                new Unit
                {
                    Code = "KG",
                    Name = "Kilogram",
                    IsActive = true,
                    CreatedAt = now.AddDays(-1)
                },
                new Unit
                {
                    Code = "L",
                    Name = "Liter",
                    IsActive = false,
                    CreatedAt = now
                }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _unitService.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(u => u.CreateAt.Should().NotBe(default(DateTime)));
            result.Should().Contain(u => u.Code == "KG" && u.IsActive);
            result.Should().Contain(u => u.Code == "L" && !u.IsActive);
        }

        #endregion
    }

    #endregion
}