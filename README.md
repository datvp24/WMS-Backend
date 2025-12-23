# 📦 WMS-BACKEND PROJECT

## 🟦 **1. AUTH & PHÂN QUYỀN**
*(6 bảng)*

| Entity |
| :--- |
| Users |
| Roles |
| UserRoles |
| Permissions |
| RolePermissions |
| UserPermissions |

**Nội dung module:**
* Register, Login, JWT, Refresh Token
* CRUD User
* Assign Role
* Assign Permission
* Middleware: HasPermission
* Authorization theo role + permission
* Seed Role/Permission mặc định
* User soft delete / active / deactivate

---

## 🟧 **2. MASTER DATA (DANH MỤC)**
*(6 bảng)*

| Entity |
| :--- |
| Products |
| Categories |
| Units |
| Brands |
| Suppliers |
| Customers |

**Nội dung module:**
* CRUD đầy đủ 6 danh mục
* Query + Paging + Sorting + Filtering
* Check duplicate name/code
* Active/Inactive product
* List products theo category/supplier

---

## 🟩 **3. WAREHOUSE (KHO)**
*(2 bảng)*

| Entity |
| :--- |
| Warehouses |
| Locations |

**Nội dung module:**
* CRUD Warehouse
* CRUD Location
* Validate layout (kệ – tầng – ô) - Ví dụ: A1-01-03
* Lock/Maintenance warehouse
* Không cho xuất/nhập khi kho bị khóa

---

## 🟨 **4. INVENTORY (TỒN KHO)**
*(2 bảng)*

| Entity |
| :--- |
| Inventory |
| InventoryHistory |

**Nội dung module:**
* Xem tồn theo location, warehouse, sản phẩm
* Xem tất cả location chứa 1 sản phẩm
* History nhập / xuất / chuyển / kiểm kê
* LockedStock (khi tạo Sales Order chưa xuất hàng)

---

## 🟫 **5. PURCHASE (NHẬP KHO)**
*(4 bảng)*

| Entity |
| :--- |
| PurchaseOrders |
| PurchaseOrderItems |
| GoodsReceipts |
| GoodsReceiptItems |

**Nội dung module:**
* Tạo đơn mua (PO) và Approve/Reject PO
* Tạo phiếu nhập (GR) theo từng location
* Cập nhật Inventory và ghi InventoryHistory

---

## 🟥 **6. SALES (XUẤT KHO)**
*(4 bảng)*

| Entity |
| :--- |
| SalesOrders |
| SalesOrderItems |
| GoodsIssues |
| GoodsIssueItems |

**Nội dung module:**
* Tạo đơn bán (SO) và Manager approve
* LockStock và tạo phiếu xuất (GI) theo location
* Trừ tồn kho và ghi InventoryHistory

---

## 🟪 **7. TRANSFER (CHUYỂN KHO)**
*(2 bảng)*

| Entity |
| :--- |
| TransferOrders |
| TransferOrderItems |

**Nội dung module:**
* Chuyển kho A → B (Approve transfer)
* Chuyển giữa từng location
* Ghi InventoryHistory 2 chiều (Out kho A / In kho B)

---

## 🧾 **TỔNG QUAN MODULE**

| STT | Module | Bảng | Mô tả |
| :--- | :--- | :--- | :--- |
| 1 | Auth | 6 | Login, JWT, Role, Permission |
| 2 | Master Data | 6 | Danh mục sản phẩm, NCC, KH |
| 3 | Warehouse | 2 | Kho + vị trí |
| 4 | Inventory | 2 | Tồn + lịch sử tồn |
| 5 | Purchase | 4 | Nhập kho |
| 6 | Sales | 4 | Xuất kho |
| 7 | Transfer | 2 | Chuyển kho |
