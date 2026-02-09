using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Wms.Application.Services.System;
using Wms.Application.DTOs.System.Permissions;
using Wms.Application.DTOs.System.Roles;
using Wms.Domain.Entity.Auth;
using Wms.Infrastructure.Persistence.Context;
using System;
using System.Threading.Tasks;

namespace Wms.Tests.Services.System
{
    public class PermissionServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly PermissionService _permissionService;

        public PermissionServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            // Mock HttpContext với user có uid = 1
            var claims = new[] { new Claim("uid", "1") };
            var identity = new ClaimsIdentity(claims);
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };

            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            _permissionService = new PermissionService(_context, _httpContextAccessorMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Case 32: Quản lý quyền

        [Fact]
        public async Task CreateAsync_ValidData_CreatesPermission()
        {
            // Arrange
            var dto = new CreatePermissionDto
            {
                Code = "SO_APPROVE",
                Description = "Duyệt đơn hàng"
            };

            // Act
            var result = await _permissionService.CreateAsync(dto);

            // Assert
            result.Should().BeGreaterThan(0);
            var permission = await _context.Permissions.FindAsync(result);
            permission.Should().NotBeNull();
            permission!.Code.Should().Be("SO_APPROVE");
            permission.CreatedBy.Should().Be(1);
            permission.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_ValidData_UpdatesPermission()
        {
            // Arrange
            var permission = new Permission
            {
                Code = "OLD_CODE",
                Description = "Old Description",
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow
            };
            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            var dto = new UpdatePermissionDto
            {
                Code = "NEW_CODE",
                Description = "New Description"
            };

            // Act
            await _permissionService.UpdateAsync(permission.Id, dto);

            // Assert
            var updated = await _context.Permissions.FindAsync(permission.Id);
            updated!.Code.Should().Be("NEW_CODE");
            updated.Description.Should().Be("New Description");
            updated.UpdatedBy.Should().Be(1);
            updated.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteAsync_ValidId_SoftDeletesPermission()
        {
            // Arrange
            var permission = new Permission
            {
                Code = "TEST_PERMISSION",
                IsDeleted = false
            };
            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            // Act
            await _permissionService.DeleteAsync(permission.Id);

            // Assert
            var deleted = await _context.Permissions.FindAsync(permission.Id);
            deleted!.IsDeleted.Should().BeTrue();
            deleted.UpdatedBy.Should().Be(1);
            deleted.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllPermissions()
        {
            // Arrange
            _context.Permissions.AddRange(
                new Permission { Code = "PERM1", Description = "Permission 1" },
                new Permission { Code = "PERM2", Description = "Permission 2" }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _permissionService.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(p => p.Code == "PERM1");
        }

        [Fact]
        public async Task CreateAsync_WithoutValidToken_ThrowsException()
        {
            // Arrange
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null);

            var dto = new CreatePermissionDto
            {
                Code = "TEST",
                Description = "Test"
            };

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(
                async () => await _permissionService.CreateAsync(dto)
            );
        }

        #endregion
    }

    public class RoleServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly RoleService _roleService;

        public RoleServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            var claims = new[] { new Claim("uid", "1") };
            var identity = new ClaimsIdentity(claims);
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };

            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            _roleService = new RoleService(_context, _httpContextAccessorMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Case 33: Quản lý vai trò

        [Fact]
        public async Task CreateRoleAsync_ValidData_CreatesRole()
        {
            // Arrange
            var dto = new CreateRoleDto
            {
                RoleName = "Warehouse_Manager"
            };

            // Act
            var result = await _roleService.CreateRoleAsync(dto);

            // Assert
            result.Should().BeGreaterThan(0);
            var role = await _context.Roles.FindAsync(result);
            role.Should().NotBeNull();
            role!.RoleName.Should().Be("Warehouse_Manager");
            role.CreatedBy.Should().Be(1);
        }

        [Fact]
        public async Task UpdateRoleAsync_ValidData_UpdatesRole()
        {
            // Arrange
            var role = new Role
            {
                RoleName = "Old_Role",
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            var dto = new UpdateRoleDto
            {
                RoleName = "Admin_Full"
            };

            // Act
            await _roleService.UpdateRoleAsync(role.Id, dto);

            // Assert
            var updated = await _context.Roles.FindAsync(role.Id);
            updated!.RoleName.Should().Be("Admin_Full");
            updated.UpdatedBy.Should().Be(1);
            updated.UpdatedAt.Should().NotBeNull();
            updated.CreatedAt.Should().Be(role.CreatedAt); // Giữ nguyên CreatedAt
        }

        [Fact]
        public async Task DeleteRoleAsync_ValidId_DeletesRole()
        {
            // Arrange
            var role = new Role { RoleName = "Test_Role" };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            // Act
            await _roleService.DeleteRoleAsync(role.Id);

            // Assert
            var deleted = await _context.Roles.FindAsync(role.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteRoleAsync_InvalidId_ThrowsException()
        {
            // Act
            Func<Task> act = async () => await _roleService.DeleteRoleAsync(9999);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Role not found");
        }

        #endregion

        #region Case 34: Gán quyền cho vai trò

        [Fact]
        public async Task AssignPermissionAsync_NewPermission_CreatesMapping()
        {
            // Arrange
            var dto = new AssignPermissionToRoleDto
            {
                RoleId = 1,
                PermissionId = 10
            };

            // Act
            await _roleService.AssignPermissionAsync(dto);

            // Assert
            var mapping = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == 1 && rp.PermissionId == 10);
            mapping.Should().NotBeNull();
        }

        [Fact]
        public async Task AssignPermissionAsync_ExistingPermission_DoesNotCreateDuplicate()
        {
            // Arrange
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = 1,
                PermissionId = 10
            });
            await _context.SaveChangesAsync();

            var dto = new AssignPermissionToRoleDto
            {
                RoleId = 1,
                PermissionId = 10
            };

            // Act
            await _roleService.AssignPermissionAsync(dto);

            // Assert
            var count = await _context.RolePermissions
                .CountAsync(rp => rp.RoleId == 1 && rp.PermissionId == 10);
            count.Should().Be(1);
        }

        [Fact]
        public async Task RemovePermissionAsync_ExistingPermission_RemovesMapping()
        {
            // Arrange
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = 1,
                PermissionId = 10
            });
            await _context.SaveChangesAsync();

            // Act
            await _roleService.RemovePermissionAsync(1, 10);

            // Assert
            var mapping = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == 1 && rp.PermissionId == 10);
            mapping.Should().BeNull();
        }

        [Fact]
        public async Task RemovePermissionAsync_NonExistentPermission_ThrowsException()
        {
            // Act
            Func<Task> act = async () => await _roleService.RemovePermissionAsync(1, 999);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Permission not assigned to this role");
        }

        #endregion

        #region Case 35: Truy vấn chi tiết

        [Fact]
        public async Task GetRoleAsync_ValidId_ReturnsRoleWithPermissions()
        {
            // Arrange
            var role = new Role
            {
                Id = 1,
                RoleName = "Admin",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1
            };

            var permission1 = new Permission { Id = 1, Code = "PERM1", Description = "Permission 1" };
            var permission2 = new Permission { Id = 2, Code = "PERM2", Description = "Permission 2" };

            _context.Roles.Add(role);
            _context.Permissions.AddRange(permission1, permission2);
            _context.RolePermissions.AddRange(
                new RolePermission { RoleId = 1, PermissionId = 1, Permission = permission1 },
                new RolePermission { RoleId = 1, PermissionId = 2, Permission = permission2 }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _roleService.GetRoleAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.RoleName.Should().Be("Admin");
            result.Permissions.Should().HaveCount(2);
            result.Permissions.Should().Contain(p => p.Code == "PERM1");
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllRolesWithPermissions()
        {
            // Arrange
            var permission = new Permission { Id = 1, Code = "TEST", Description = "Test" };
            var role1 = new Role { Id = 1, RoleName = "Role1" };
            var role2 = new Role { Id = 2, RoleName = "Role2" };

            _context.Permissions.Add(permission);
            _context.Roles.AddRange(role1, role2);
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = 1,
                PermissionId = 1,
                Permission = permission
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _roleService.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
            var roleWithPermission = result.First(r => r.Id == 1);
            roleWithPermission.Permissions.Should().HaveCount(1);
        }

        #endregion
    }
}