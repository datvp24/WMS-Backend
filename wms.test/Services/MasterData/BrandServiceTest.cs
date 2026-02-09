using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wms.Application.Services.MasterData;
using Wms.Application.DTOs.MasterData.Brands;
using Wms.Domain.Entity.MasterData;
using Wms.Infrastructure.Persistence.Context;
using System;
using System.Threading.Tasks;

namespace Wms.Tests.Services.MasterData
{
    public class BrandServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly BrandService _brandService;

        public BrandServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _brandService = new BrandService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Case 14: Tạo thương hiệu

        [Fact]
        public async Task CreateAsync_WithCode_CreatesBrandWithGivenCode()
        {
            // Arrange
            var dto = new CreateBrandDto
            {
                Code = "B01",
                Name = "Apple",
                Description = "Apple Inc.",
                IsActive = true
            };

            // Act
            var result = await _brandService.CreateAsync(dto);

            // Assert
            result.Should().BeGreaterThan(0);
            var brand = await _context.Brands.FindAsync(result);
            brand.Should().NotBeNull();
            brand!.Code.Should().Be("B01");
            brand.Name.Should().Be("Apple");
        }

        [Fact]
        public async Task CreateAsync_WithoutCode_GeneratesCode()
        {
            // Arrange
            var dto = new CreateBrandDto
            {
                Code = "",
                Name = "Samsung",
                IsActive = true
            };

            // Act
            var result = await _brandService.CreateAsync(dto);

            // Assert
            var brand = await _context.Brands.FindAsync(result);
            brand!.Code.Should().MatchRegex(@"BR\d{3}"); // BR001, BR002, etc.
        }

        [Fact]
        public async Task CreateAsync_DuplicateCode_ThrowsException()
        {
            // Arrange
            _context.Brands.Add(new Brand { Code = "B01", Name = "Existing Brand" });
            await _context.SaveChangesAsync();

            var dto = new CreateBrandDto
            {
                Code = "B01",
                Name = "New Brand"
            };

            // Act
            Func<Task> act = async () => await _brandService.CreateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Code already exists");
        }

        [Fact]
        public async Task CreateAsync_DuplicateName_ThrowsException()
        {
            // Arrange
            _context.Brands.Add(new Brand { Code = "B01", Name = "Apple" });
            await _context.SaveChangesAsync();

            var dto = new CreateBrandDto
            {
                Code = "B02",
                Name = "Apple"
            };

            // Act
            Func<Task> act = async () => await _brandService.CreateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Name already exists");
        }

        #endregion

        #region Case 15: Cập nhật thương hiệu

        [Fact]
        public async Task UpdateAsync_ValidData_UpdatesBrand()
        {
            // Arrange
            var brand = new Brand
            {
                Code = "B01",
                Name = "Old Name",
                IsActive = true
            };
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            var dto = new UpdateBrandDto
            {
                Code = "B01-NEW",
                Name = "New Name",
                Description = "Updated",
                IsActive = false
            };

            // Act
            await _brandService.UpdateAsync(brand.Id, dto);

            // Assert
            var updated = await _context.Brands.FindAsync(brand.Id);
            updated!.Code.Should().Be("B01-NEW");
            updated.Name.Should().Be("New Name");
            updated.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_SameCodeAndName_Succeeds()
        {
            // Arrange
            var brand = new Brand
            {
                Code = "B01",
                Name = "Apple"
            };
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            var dto = new UpdateBrandDto
            {
                Code = "B01",
                Name = "Apple",
                IsActive = true
            };

            // Act
            await _brandService.UpdateAsync(brand.Id, dto);

            // Assert
            var updated = await _context.Brands.FindAsync(brand.Id);
            updated!.Code.Should().Be("B01");
            updated.Name.Should().Be("Apple");
        }

        [Fact]
        public async Task UpdateAsync_DuplicateCodeOfOtherBrand_ThrowsException()
        {
            // Arrange
            _context.Brands.AddRange(
                new Brand { Id = 1, Code = "B01", Name = "Brand 1" },
                new Brand { Id = 2, Code = "B02", Name = "Brand 2" }
            );
            await _context.SaveChangesAsync();

            var dto = new UpdateBrandDto
            {
                Code = "B01",
                Name = "Brand 2 Updated"
            };

            // Act
            Func<Task> act = async () => await _brandService.UpdateAsync(2, dto);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Code already exists");
        }

        [Fact]
        public async Task UpdateAsync_DuplicateNameOfOtherBrand_ThrowsException()
        {
            // Arrange
            _context.Brands.AddRange(
                new Brand { Id = 1, Code = "B01", Name = "Apple" },
                new Brand { Id = 2, Code = "B02", Name = "Samsung" }
            );
            await _context.SaveChangesAsync();

            var dto = new UpdateBrandDto
            {
                Code = "B02",
                Name = "Apple"
            };

            // Act
            Func<Task> act = async () => await _brandService.UpdateAsync(2, dto);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Name already exists");
        }

        [Fact]
        public async Task UpdateAsync_InvalidId_ThrowsException()
        {
            // Arrange
            var dto = new UpdateBrandDto
            {
                Code = "B01",
                Name = "Test"
            };

            // Act
            Func<Task> act = async () => await _brandService.UpdateAsync(-1, dto);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Brand not found");
        }

        #endregion

        #region Case 16: Xóa và truy vấn

        [Fact]
        public async Task DeleteAsync_ValidId_DeletesBrand()
        {
            // Arrange
            var brand = new Brand { Code = "B01", Name = "Test Brand" };
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            // Act
            await _brandService.DeleteAsync(brand.Id);

            // Assert
            var deleted = await _context.Brands.FindAsync(brand.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_InvalidId_ThrowsException()
        {
            // Act
            Func<Task> act = async () => await _brandService.DeleteAsync(999);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Brand not found");
        }

        [Fact]
        public async Task GetAsync_ValidId_ReturnsBrandDto()
        {
            // Arrange
            var brand = new Brand
            {
                Code = "B01",
                Name = "Apple",
                IsActive = true
            };
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            // Act
            var result = await _brandService.GetAsync(brand.Id);

            // Assert
            result.Should().NotBeNull();
            result.Code.Should().Be("B01");
            result.Name.Should().Be("Apple");
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllBrands()
        {
            // Arrange
            _context.Brands.AddRange(
                new Brand { Code = "B01", Name = "Brand 1", IsActive = true },
                new Brand { Code = "B02", Name = "Brand 2", IsActive = false },
                new Brand { Code = "B03", Name = "Brand 3", IsActive = true }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _brandService.GetAllAsync();

            // Assert
            result.Should().HaveCount(3);
            result.Should().Contain(b => b.Code == "B01");
            result.Should().Contain(b => b.Code == "B02");
        }

        #endregion
    }
}