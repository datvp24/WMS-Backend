using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wms.Application.Services.Auth;
using Wms.Application.DTOs.Auth;
using Wms.Application.DTOS.Auth;
using Wms.Application.Interfaces.Services;
using Wms.Domain.Entity.Auth;
using Wms.Infrastructure.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Wms.Tests.Services.Auth
{
    public class AuthServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IPasswordHasher> _hasherMock;
        private readonly Mock<IJwtService> _jwtMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            // Sử dụng InMemory database cho testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _hasherMock = new Mock<IPasswordHasher>();
            _jwtMock = new Mock<IJwtService>();

            _authService = new AuthService(_context, _hasherMock.Object, _jwtMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Case 1: Đăng nhập

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
        {
            // Arrange
            var password = "Password123";
            var hashedPassword = "hashed_password";
            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                PasswordHash = hashedPassword,
                FullName = "Test User",
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _hasherMock.Setup(x => x.Verify(password, hashedPassword)).Returns(true);
            _jwtMock.Setup(x => x.GenerateToken(user))
                .Returns(new AuthResponseDto
                {
                    Token = "valid_token",
                    RefreshToken = "refresh_token",
                    ExpireAt = DateTime.UtcNow.AddHours(4)
                });

            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = password
            };

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().Be("valid_token");
            result.RefreshToken.Should().Be("refresh_token");
        }

        [Fact]
        public async Task LoginAsync_InvalidEmail_ThrowsException()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "password"
            };

            // Act
            Func<Task> act = async () => await _authService.LoginAsync(loginDto);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Invalid email or password");
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ThrowsException()
        {
            // Arrange
            var user = new User
            {
                Email = "test@example.com",
                PasswordHash = "hashed_password",
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _hasherMock.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "wrong_password"
            };

            // Act
            Func<Task> act = async () => await _authService.LoginAsync(loginDto);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Invalid email or password");
        }

        [Fact]
        public async Task LoginAsync_InactiveUser_ThrowsException()
        {
            // Arrange
            var user = new User
            {
                Email = "test@example.com",
                PasswordHash = "hashed_password",
                IsActive = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _hasherMock.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "password"
            };

            // Act
            Func<Task> act = async () => await _authService.LoginAsync(loginDto);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("User is deactivated");
        }

        #endregion

        #region Case 2: Lấy danh sách user

        [Fact]
        public async Task GetAllUsersAsync_ReturnsOnlyActiveUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Id = 1, Email = "user1@test.com", FullName = "User 1", IsActive = true, IsDeleted = false },
                new User { Id = 2, Email = "user2@test.com", FullName = "User 2", IsActive = true, IsDeleted = false },
                new User { Id = 3, Email = "user3@test.com", FullName = "User 3", IsActive = true, IsDeleted = true }
            };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.GetAllUsersAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().NotContain(u => u.Id == 3);
        }

        [Fact]
        public async Task GetAllUsersAsync_ExcludesDeletedUsers()
        {
            // Arrange
            var deletedUser = new User
            {
                Email = "deleted@test.com",
                FullName = "Deleted User",
                IsDeleted = true
            };

            _context.Users.Add(deletedUser);
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.GetAllUsersAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region Case 3: Lấy user theo ID

        [Fact]
        public async Task GetUserByIdAsync_ValidId_ReturnsUser()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                FullName = "Test User",
                IsActive = true,
                IsDeleted = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.GetUserByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Email.Should().Be("test@example.com");
        }

        [Fact]
        public async Task GetUserByIdAsync_InvalidId_ThrowsException()
        {
            // Act
            Func<Task> act = async () => await _authService.GetUserByIdAsync(999);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("User not found");
        }

        [Fact]
        public async Task GetUserByIdAsync_DeletedUser_ThrowsException()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                IsDeleted = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            Func<Task> act = async () => await _authService.GetUserByIdAsync(1);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("User not found");
        }

        #endregion

        #region Case 4: Tạo User (Admin)

        [Fact]
        public async Task CreateUserAsync_ValidData_CreatesUser()
        {
            // Arrange
            var dto = new CreateUserDto
            {
                FullName = "New User",
                Email = "newuser@test.com",
                Password = "Password123"
            };

            _hasherMock.Setup(x => x.Hash(dto.Password)).Returns("hashed_password");
            _jwtMock.Setup(x => x.GetUserId()).Returns(1);

            // Act
            var result = await _authService.CreateUserAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be(dto.Email);
            result.IsActive.Should().BeTrue();

            var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            userInDb.Should().NotBeNull();
            userInDb!.CreatedBy.Should().Be(1);
        }

        [Fact]
        public async Task CreateUserAsync_DuplicateEmail_ThrowsException()
        {
            // Arrange
            var existingUser = new User
            {
                Email = "existing@test.com",
                FullName = "Existing User",
                PasswordHash = "hash"
            };

            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var dto = new CreateUserDto
            {
                Email = "existing@test.com",
                Password = "password"
            };

            // Act
            Func<Task> act = async () => await _authService.CreateUserAsync(dto);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Email already exists");
        }

        [Fact]
        public async Task CreateUserAsync_RecordsCreatedBy()
        {
            // Arrange
            var adminId = 5;
            _jwtMock.Setup(x => x.GetUserId()).Returns(adminId);
            _hasherMock.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed");

            var dto = new CreateUserDto
            {
                Email = "test@test.com",
                FullName = "Test",
                Password = "pass"
            };

            // Act
            await _authService.CreateUserAsync(dto);

            // Assert
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            user!.CreatedBy.Should().Be(adminId);
        }

        #endregion

        #region Case 5: Cập nhật User

        [Fact]
        public async Task UpdateUserAsync_ValidData_UpdatesUser()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "old@test.com",
                FullName = "Old Name",
                PasswordHash = "old_hash",
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _jwtMock.Setup(x => x.GetUserId()).Returns(2);

            var updateDto = new UpdateUserDto
            {
                FullName = "New Name",
                Email = "new@test.com",
                IsActive = false,
                Password = null
            };

            // Act
            await _authService.UpdateUserAsync(1, updateDto);

            // Assert
            var updatedUser = await _context.Users.FindAsync(1);
            updatedUser!.FullName.Should().Be("New Name");
            updatedUser.Email.Should().Be("new@test.com");
            updatedUser.IsActive.Should().BeFalse();
            updatedUser.UpdatedBy.Should().Be(2);
            updatedUser.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateUserAsync_WithPassword_UpdatesPasswordHash()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "test@test.com",
                PasswordHash = "old_hash"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _hasherMock.Setup(x => x.Hash("NewPassword123")).Returns("new_hash");
            _jwtMock.Setup(x => x.GetUserId()).Returns(1);

            var updateDto = new UpdateUserDto
            {
                Password = "NewPassword123",
                Email = "test@test.com",
                FullName = "Test"
            };

            // Act
            await _authService.UpdateUserAsync(1, updateDto);

            // Assert
            var updatedUser = await _context.Users.FindAsync(1);
            updatedUser!.PasswordHash.Should().Be("new_hash");
        }

        [Fact]
        public async Task UpdateUserAsync_EmptyPassword_DoesNotUpdatePasswordHash()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "test@test.com",
                PasswordHash = "old_hash"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _jwtMock.Setup(x => x.GetUserId()).Returns(1);

            var updateDto = new UpdateUserDto
            {
                Password = "",
                Email = "test@test.com",
                FullName = "Test"
            };

            // Act
            await _authService.UpdateUserAsync(1, updateDto);

            // Assert
            var updatedUser = await _context.Users.FindAsync(1);
            updatedUser!.PasswordHash.Should().Be("old_hash");
        }

        [Fact]
        public async Task UpdateUserAsync_InvalidId_ThrowsException()
        {
            // Arrange
            var updateDto = new UpdateUserDto
            {
                Email = "test@test.com",
                FullName = "Test"
            };

            // Act
            Func<Task> act = async () => await _authService.UpdateUserAsync(999, updateDto);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("User not found");
        }

        [Fact]
        public async Task UpdateUserAsync_RecordsUpdatedBy()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "test@test.com",
                FullName = "Test"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var adminId = 10;
            _jwtMock.Setup(x => x.GetUserId()).Returns(adminId);

            var updateDto = new UpdateUserDto
            {
                Email = "test@test.com",
                FullName = "Updated"
            };

            // Act
            await _authService.UpdateUserAsync(1, updateDto);

            // Assert
            var updatedUser = await _context.Users.FindAsync(1);
            updatedUser!.UpdatedBy.Should().Be(adminId);
        }

        #endregion

        #region Case 6: Xóa User

        [Fact]
        public async Task DeleteUserAsync_ValidId_SoftDeletesUser()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "test@test.com",
                IsDeleted = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _jwtMock.Setup(x => x.GetUserId()).Returns(5);

            // Act
            await _authService.DeleteUserAsync(1);

            // Assert
            var deletedUser = await _context.Users.FindAsync(1);
            deletedUser!.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteUserAsync_InvalidId_ThrowsException()
        {
            // Act
            Func<Task> act = async () => await _authService.DeleteUserAsync(999);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("User not found");
        }

        [Fact]
        public async Task DeleteUserAsync_RecordsDeleteInfo()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "test@test.com"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var adminId = 7;
            _jwtMock.Setup(x => x.GetUserId()).Returns(adminId);

            // Act
            await _authService.DeleteUserAsync(1);

            // Assert
            var deletedUser = await _context.Users.FindAsync(1);
            deletedUser!.UpdatedBy.Should().Be(adminId);
            deletedUser.UpdatedAt.Should().NotBeNull();
        }

        #endregion

        #region Case 7: Gán role cho user

        [Fact]
        public async Task AssignRoleAsync_NewRole_CreatesUserRole()
        {
            // Arrange
            var userId = 1;
            var roleId = 5;

            // Act
            await _authService.AssignRoleAsync(userId, roleId);

            // Assert
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            userRole.Should().NotBeNull();
        }

        [Fact]
        public async Task AssignRoleAsync_ExistingRole_DoesNotCreateDuplicate()
        {
            // Arrange
            var userId = 1;
            var roleId = 5;

            _context.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
            await _context.SaveChangesAsync();

            // Act
            await _authService.AssignRoleAsync(userId, roleId);

            // Assert
            var count = await _context.UserRoles
                .CountAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            count.Should().Be(1);
        }

        #endregion

        #region Case 8: Gán permission cho user

        [Fact]
        public async Task AssignPermissionAsync_NewPermission_CreatesUserPermission()
        {
            // Arrange
            var userId = 1;
            var permissionId = 10;

            // Act
            await _authService.AssignPermissionAsync(userId, permissionId);

            // Assert
            var userPermission = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

            userPermission.Should().NotBeNull();
        }

        [Fact]
        public async Task AssignPermissionAsync_ExistingPermission_DoesNotCreateDuplicate()
        {
            // Arrange
            var userId = 1;
            var permissionId = 10;

            _context.UserPermissions.Add(new UserPermission { UserId = userId, PermissionId = permissionId });
            await _context.SaveChangesAsync();

            // Act
            await _authService.AssignPermissionAsync(userId, permissionId);

            // Assert
            var count = await _context.UserPermissions
                .CountAsync(up => up.UserId == userId && up.PermissionId == permissionId);

            count.Should().Be(1);
        }

        #endregion
    }
}