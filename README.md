Warehouse Management System (WMS) - Backend API
Dự án Backend cung cấp hệ thống RESTful API cho quản lý kho hàng (WMS). Được phát triển trên nền tảng ASP.NET Core, hệ thống tập trung vào hiệu suất, tính bảo mật cao thông qua phân quyền chi tiết và kiến trúc bền vững.

Công nghệ & Kỹ thuật
Core: .NET 8, ASP.NET Core Web API.

Database: MySQL, Entity Framework Core (Code First).

Security: JWT Authentication (Access/Refresh Token), Role-based & Permission-based Authorization.

DevOps: Docker, Docker Compose.

Tools: Swagger UI, AutoMapper, BCrypt.Net.

Kiến trúc Hệ thống (Clean Architecture)
Dự án tuân thủ nghiêm ngặt nguyên tắc tách biệt mối quan tâm (Separation of Concerns):

Domain: Định nghĩa Entities, Enums và các quy tắc nghiệp vụ cốt lõi.

Application: Xử lý logic nghiệp vụ, DTOs, Mapping và Interfaces.

Infrastructure: Triển khai truy cập dữ liệu (Persistence), cấu hình EF Core và các dịch vụ bên thứ ba (JWT, Mail...).

API: Cổng giao tiếp chính, xử lý Middleware, Filters và Controllers.

Các Module Nghiệp vụ
Hệ thống bao gồm 26 bảng cơ sở dữ liệu, chia thành 7 module chính:

Auth (6 bảng): Quản lý định danh, phân quyền động (Permissions) và tài khoản.

Master Data (6 bảng): Quản lý thông tin gốc về Sản phẩm, Nhà cung cấp, Khách hàng.

Warehouse & Inventory (4 bảng): Quản lý kho, sơ đồ vị trí (Location) và theo dõi tồn kho thực tế.

Purchase & Sales (8 bảng): Quy trình Nhập kho (PO) và Xuất kho (SO).

Transfer (2 bảng): Điều chuyển hàng hóa nội bộ giữa các kho.

Hướng dẫn Triển khai nhanh (Docker)
Đây là phương thức khuyến nghị để chạy toàn bộ hệ thống (API & Database) chỉ với một câu lệnh:

Yêu cầu: Đã cài đặt Docker & Docker Compose.

Khởi chạy:

Bash

docker-compose up -d --build
Truy cập: * Swagger UI: http://localhost:5000/swagger

Dữ liệu mẫu (Seed Data): Hệ thống tự động khởi tạo tài khoản Admin (admin@wms.com / admin123) và dữ liệu sản phẩm mẫu ngay khi khởi động.

Tính năng nổi bật
Migration & Seeding tự động: Tự động tạo cấu trúc bảng và đổ dữ liệu mẫu ngay trong Docker container.

Phân quyền chi tiết: Kiểm soát truy cập đến từng Endpoint dựa trên danh sách Permission được cấu hình trong DB.

Quản lý tồn kho nâng cao: Hỗ trợ trạng thái tồn kho (On-hand, Locked, In-transit).

Phát triển bởi: [Vo Phat Dat/Seventyfour]

Mục tiêu: Cung cấp giải pháp quản lý kho mã nguồn mở, dễ mở rộng và tích hợp.
