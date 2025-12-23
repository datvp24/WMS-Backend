---

# 🟦 **1. AUTH & PHÂN QUYỀN**

(6 bảng)

| Entity          |
| --------------- |
| Users           |
| Roles           |
| UserRoles       |
| Permissions     |
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

# 🟧 **2. MASTER DATA (DANH MỤC)**

(6 bảng)

| Entity     |
| ---------- |
| Products   |
| Categories |
| Units      |
| Brands     |
| Suppliers  |
| Customers  |

**Nội dung module:**

* CRUD đầy đủ 6 danh mục
* Query + Paging + Sorting + Filtering
* Check duplicate name/code
* Active/Inactive product
* List products theo category/supplier

---

# 🟩 **3. WAREHOUSE (KHO)**

(2 bảng)

| Entity     |
| ---------- |
| Warehouses |
| Locations  |

**Nội dung module:**

* CRUD Warehouse
* CRUD Location
* Validate layout (kệ – tầng – ô)
* Eg: A1-01-03
* Lock/Maintenance warehouse
* Không cho xuất/nhập khi kho bị khóa

---

# 🟨 **4. INVENTORY (TỒN KHO)**

(2 bảng)

| Entity           |
| ---------------- |
| Inventory        |
| InventoryHistory |

**Nội dung module:**

* Xem tồn theo location
* Xem tồn theo warehouse
* Xem tồn theo sản phẩm
* Xem tất cả location chứa 1 SP
* History nhập / xuất / chuyển / kiểm kê
* LockedStock (khi tạo Sales Order chưa xuất hàng)

---

# 🟫 **5. PURCHASE (NHẬP KHO)**

(4 bảng)

| Entity             |
| ------------------ |
| PurchaseOrders     |
| PurchaseOrderItems |
| GoodsReceipts      |
| GoodsReceiptItems  |

**Nội dung module:**

* Tạo đơn mua (PO)
* Approve/Reject PO
* Tạo phiếu nhập (GR)
* Nhập theo từng location
* Cập nhật Inventory
* Ghi InventoryHistory

---

# 🟥 **6. SALES (XUẤT KHO)**

(4 bảng)

| Entity          |
| --------------- |
| SalesOrders     |
| SalesOrderItems |
| GoodsIssues     |
| GoodsIssueItems |

**Nội dung module:**

* Tạo đơn bán (SO)
* Manager approve
* LockStock
* Tạo phiếu xuất (GI)
* Xuất theo location
* Trừ tồn kho
* Ghi InventoryHistory

---

# 🟪 **7. TRANSFER (CHUYỂN KHO)**

(2 bảng)

| Entity             |
| ------------------ |
| TransferOrders     |
| TransferOrderItems |

**Nội dung module:**

* Chuyển kho A → B
* Approve transfer
* Chuyển giữa từng location
* Ghi InventoryHistory 2 chiều:

  * Out từ kho A
  * In vào kho B

---

# 🟦 **8. STOCK TAKE (KIỂM KÊ)**

(2 bảng)

| Entity         |
| -------------- |
| StockTakes     |
| StockTakeItems |

**Nội dung module:**

* Tạo đợt kiểm kê
* Scan/Count theo location
* SystemQty vs CountedQty
* Xuất report lệch
* Tạo InventoryAdjustment
* Ghi InventoryHistory

---

# 🧩 **9. SYSTEM (OPTIONAL)**

(2 bảng)

| Entity    |
| --------- |
| AuditLogs |
| ErrorLogs |

**Nội dung module:**

* Ghi log hành động user
* Log lỗi backend
* Tự động ghi bằng Middleware



=====================
BỎ MUDULE 8 VÀ 9
---

# 🧾 **TỔNG QUAN MODULE**

| STT | Module      | Bảng | Mô tả                        |
| --- | ----------- | ---- | ---------------------------- |
| 1   | Auth        | 6    | Login, JWT, Role, Permission |
| 2   | Master Data | 6    | Danh mục sản phẩm, NCC, KH   |
| 3   | Warehouse   | 2    | Kho + vị trí                 |
| 4   | Inventory   | 2    | Tồn + lịch sử tồn            |
| 5   | Purchase    | 4    | Nhập kho                     |
| 6   | Sales       | 4    | Xuất kho                     |
| 7   | Transfer    | 2    | Chuyển kho                   |
