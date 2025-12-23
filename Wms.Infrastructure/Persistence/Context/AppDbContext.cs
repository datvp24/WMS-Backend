using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wms.Domain.Entity.Auth;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Entity.Purchase;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Sales;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Entity.Transfer;
using Wms.Domain.Entity.StockTakes;

namespace Wms.Infrastructure.Persistence.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // AUTH
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

        //// MASTER DATA
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Unit> Units => Set<Unit>();
        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Customer> Customers => Set<Customer>();

        // WAREHOUSE
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<Location> Locations => Set<Location>();

        //// INVENTORY
        public DbSet<Inventory> Inventories => Set<Inventory>();
        public DbSet<InventoryHistory> InventoryHistories => Set<InventoryHistory>();


        // PURCHASE
        public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
        public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
        public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
        public DbSet<GoodsReceiptItem> GoodsReceiptItems => Set<GoodsReceiptItem>();

        // SALES
        public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
        public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
        public DbSet<GoodsIssue> GoodsIssues => Set<GoodsIssue>();
        public DbSet<GoodsIssueItem> GoodsIssueItems => Set<GoodsIssueItem>();

        // TRANSFER
        public DbSet<TransferOrder> TransferOrders => Set<TransferOrder>();
        public DbSet<TransferOrderItem> TransferOrderItems => Set<TransferOrderItem>();

        // STOCK TAKE
        public DbSet<StockTake> StockTakes => Set<StockTake>();
        public DbSet<StockTakeItem> StockTakeItems => Set<StockTakeItem>();

        //// SYSTEM
        //public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        //public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Auto-load tất cả configuration trong Infrastructure
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }

}
