# ⚙️ WMS - BACKEND API

Hệ thống quản lý kho hàng (Phần Backend) được xây dựng trên nền tảng .NET Core, cung cấp các RESTful API cho ứng dụng Frontend.

## 🚀 Công nghệ sử dụng
* **Language:** C# (.NET Core 6/7/8)
* **ORM:** Entity Framework Core
* **Database:** MYSQL
* **Auth:** JWT (JSON Web Token) & Refresh Token
* **API Documentation:** Swagger UI

## 📂 Các Module chính
| STT | Module      | Bảng | Mô tả                        |
| --- | ----------- | ---- | ---------------------------- |
| 1   | Auth        | 6    | Login, JWT, Role, Permission |
| 2   | Master Data | 6    | Danh mục sản phẩm, NCC, KH   |
| 3   | Warehouse   | 2    | Kho + vị trí                 |
| 4   | Inventory   | 2    | Tồn + lịch sử tồn            |
| 5   | Purchase    | 4    | Nhập kho                     |
| 6   | Sales       | 4    | Xuất kho                     |
| 7   | Transfer    | 2    | Chuyển kho                   |

## 🏗️ Kiến trúc dự án
Dự án được chia thành các Layer theo tiêu chuẩn:

1. **Domain Layer:** Chứa các Entity (Users, Products, Warehouses...), Value Objects và Interfaces cơ bản. Không phụ thuộc vào bất kỳ Layer nào khác.
2. **Application Layer:** Chứa các Logic nghiệp vụ (Services), DTOs, Mappers và các Interfaces cho Repository. Sử dụng CQRS (nếu có) hoặc Service Pattern.
3. **Infrastructure Layer:** Triển khai các Interfaces từ Layer trên, kết nối MYSQL thông qua EF Core, JWT.
4. **Web API:** Điểm cuối (Endpoints) để Frontend React kết nối. Chỉ chịu trách nhiệm điều hướng và nhận/trả dữ liệu.

## 🛠 Cài đặt & Chạy dự án
1. Clone repository về máy.
2. Cấu hình `ConnectionStrings` trong file `appsettings.json`.
3. dotnet ef migrations add --project Wms.Infrastructure --startup-project Wms.Api
4. dotnet ef database update --project Wms.Infrastructure --startup-project Wms.Api
