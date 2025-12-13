// File: Extensions/ServiceCollectionExtensions.cs

using Microsoft.Extensions.DependencyInjection;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Interfaces.Services.MasterData;
using Wms.Application.Interfaces.Services.Purchase;
using Wms.Application.Interfaces.Services.System;
using Wms.Application.Interfaces.Services.Warehouse;
using Wms.Application.Interfaces.Services.Sales;
using Wms.Application.Services.Inventorys;
using Wms.Application.Services.MasterData;
using Wms.Application.Services.Purchase;
using Wms.Application.Services.System;
using Wms.Application.Services.Warehouses;
//using Wms.Application.Services.Sales;


namespace Wms.Api.Extensions // Đảm bảo đúng namespace của API project
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // SYSTEM MANAGEMENT4
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IPermissionService, PermissionService>();

            // MASTER DATA
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IUnitService, UnitService>();
            services.AddScoped<IBrandService, BrandService>();
            services.AddScoped<ISupplierService, SupplierService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IProductService, ProductService>();

            // WAREHOUSE
            services.AddScoped<IWarehouseService, WarehouseService>();

            // INVENTORY
            services.AddScoped<IInventoryService, InventoryService>();

            // PURCHASE
            services.AddScoped<IPurchaseService, PurchaseService>();
            //SALES
            services.AddScoped<IPurchaseService, PurchaseService>();


            return services;
        }
    }
}