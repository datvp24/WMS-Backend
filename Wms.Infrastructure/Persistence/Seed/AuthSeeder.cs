using Wms.Domain.Entity.Auth;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Infrastructure.Seed;

public static class AuthSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // 1. Seed Roles
        if (!db.Roles.Any())
        {
            db.Roles.AddRange(
                new Role { Id = 1, RoleName = "Admin" },
                new Role { Id = 2, RoleName = "Manager" },
                new Role { Id = 3, RoleName = "Staff" }
            );
        }

        // 2. Seed Permissions
        if (!db.Permissions.Any())
        {
            var permissions = new List<Permission>
            {
                // Users
                new Permission { Code = "user.view", Description = "View users" },
                new Permission { Code = "user.create", Description = "Create users" },
                new Permission { Code = "user.update", Description = "Update users" },
                new Permission { Code = "user.delete", Description = "Delete users" },
                new Permission { Code = "user.assign-role", Description = "Assign roles to users" },
                new Permission { Code = "user.assign-permission", Description = "Assign permissions to users" },

                // Brands
                new Permission { Code = "brand.view", Description = "View brands" },
                new Permission { Code = "brand.create", Description = "Create brands" },
                new Permission { Code = "brand.update", Description = "Update brands" },
                new Permission { Code = "brand.delete", Description = "Delete brands" },

                // Categories
                new Permission { Code = "category.view", Description = "View categories" },
                new Permission { Code = "category.create", Description = "Create categories" },
                new Permission { Code = "category.update", Description = "Update categories" },
                new Permission { Code = "category.delete", Description = "Delete categories" },

                // Customers
                new Permission { Code = "customer.view", Description = "View customers" },
                new Permission { Code = "customer.create", Description = "Create customers" },
                new Permission { Code = "customer.update", Description = "Update customers" },
                new Permission { Code = "customer.delete", Description = "Delete customers" },

                // Inventory
                new Permission { Code = "inventory.view", Description = "View inventory" },
                new Permission { Code = "inventory.history", Description = "View inventory history" },
                new Permission { Code = "inventory.adjust", Description = "Adjust inventory" },
                new Permission { Code = "inventory.lock", Description = "Lock inventory" },
                new Permission { Code = "inventory.unlock", Description = "Unlock inventory" },

                // Locations
                new Permission { Code = "location.view", Description = "View locations" },
                new Permission { Code = "location.create", Description = "Create locations" },
                new Permission { Code = "location.update", Description = "Update locations" },
                new Permission { Code = "location.delete", Description = "Delete locations" },

                // Permissions
                new Permission { Code = "permission.view", Description = "View permissions" },
                new Permission { Code = "permission.create", Description = "Create permissions" },
                new Permission { Code = "permission.update", Description = "Update permissions" },
                new Permission { Code = "permission.delete", Description = "Delete permissions" },

                // Products
                new Permission { Code = "product.view", Description = "View products" },
                new Permission { Code = "product.create", Description = "Create products" },
                new Permission { Code = "product.update", Description = "Update products" },
                new Permission { Code = "product.delete", Description = "Delete products" },

                // Purchases
                new Permission { Code = "purchase.po.create", Description = "Create purchase orders" },
                new Permission { Code = "purchase.po.view", Description = "View purchase orders" },
                new Permission { Code = "purchase.po.approve", Description = "Approve purchase orders" },
                new Permission { Code = "purchase.po.reject", Description = "Reject purchase orders" },
                new Permission { Code = "purchase.gr.create", Description = "Create goods receipts" },
                new Permission { Code = "purchase.gr.view", Description = "View goods receipts" },
                new Permission { Code = "purchase.gr.cancel", Description = "Cancel goods receipts" },

                // Roles
                new Permission { Code = "role.view", Description = "View roles" },
                new Permission { Code = "role.create", Description = "Create roles" },
                new Permission { Code = "role.update", Description = "Update roles" },
                new Permission { Code = "role.delete", Description = "Delete roles" },
                new Permission { Code = "role.assign-permission", Description = "Assign permissions to roles" },
                new Permission { Code = "role.remove-permission", Description = "Remove permissions from roles" },

                // Suppliers
                new Permission { Code = "supplier.view", Description = "View suppliers" },
                new Permission { Code = "supplier.create", Description = "Create suppliers" },
                new Permission { Code = "supplier.update", Description = "Update suppliers" },
                new Permission { Code = "supplier.delete", Description = "Delete suppliers" },

                // Units
                new Permission { Code = "unit.view", Description = "View units" },
                new Permission { Code = "unit.create", Description = "Create units" },
                new Permission { Code = "unit.update", Description = "Update units" },
                new Permission { Code = "unit.delete", Description = "Delete units" },

                // Warehouses
                new Permission { Code = "warehouse.view", Description = "View warehouses" },
                new Permission { Code = "warehouse.create", Description = "Create warehouses" },
                new Permission { Code = "warehouse.update", Description = "Update warehouses" },
                new Permission { Code = "warehouse.delete", Description = "Delete warehouses" },
                new Permission { Code = "warehouse.lock", Description = "Lock warehouses" },
                new Permission { Code = "warehouse.unlock", Description = "Unlock warehouses" }
            };

            db.Permissions.AddRange(permissions);
            await db.SaveChangesAsync();

            // 3. Assign all permissions to Admin (roleId = 1)
            var adminRolePermissions = permissions.Select(p => new RolePermission
            {
                RoleId = 1,
                PermissionId = p.Id
            }).ToList();

            db.RolePermissions.AddRange(adminRolePermissions);
        }

        // 4. Seed Admin User
        if (!db.Users.Any())
        {
            var admin = new User
            {
                FullName = "Administrator",
                Email = "admin@wms.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123")
            };

            db.Users.Add(admin);
            await db.SaveChangesAsync();

            // assign admin role
            db.UserRoles.Add(new UserRole
            {
                UserId = admin.Id,
                RoleId = 1
            });
        }

        await db.SaveChangesAsync();
    }
}
