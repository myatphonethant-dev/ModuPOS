# Data Model Specification: Database Feature Alignment

**Feature Branch**: `002-database-feature-alignment`
**Date**: 2026-11-16
**Status**: Completed

## 1. Overview

The ModuPOS data model follows an MSSQL Database-First architecture with singular table names, clustered primary keys, and no foreign key constraints enforced at the database level. Domain relationships are maintained via application logic and navigation references.

All active tables are mapped to C# entities under `ModuPOS.Database.AppDbContextModels` within `AppDbContext`.

---

## 2. Entity Catalog & Table Definitions

### A. Inventory Domain

#### 1. `Category` Table
- **C# Entity**: `ModuPOS.Database.AppDbContextModels.Category`
- **Primary Key**: `CategoryId` (int, IDENTITY)
- **Soft Delete**: `DeleteFlag` (bit)
- **Columns**:

| Column Name | Type | Nullable | Description |
|---|---|---|---|
| `CategoryId` | INT IDENTITY(1,1) | NO | Clustered Primary Key |
| `CategoryCode` | VARCHAR(50) | NO | Unique business code |
| `CategoryName` | NVARCHAR(200) | NO | Human-readable name |
| `DeleteFlag` | BIT | NO | Soft-delete status (0 = Active, 1 = Deleted) |

- **Validation Rules**:
  - `CategoryCode` cannot be null or whitespace; must be unique among non-deleted categories.
  - `CategoryName` cannot be null or whitespace; max 200 characters.

#### 2. `Product` Table
- **C# Entity**: `ModuPOS.Database.AppDbContextModels.Product`
- **Primary Key**: `ProductId` (int, IDENTITY)
- **Soft Delete**: `DeleteFlag` (bit)
- **Audit Columns**: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- **Columns**:

| Column Name | Type | Nullable | Description |
|---|---|---|---|
| `ProductId` | INT IDENTITY(1,1) | NO | Clustered Primary Key |
| `ProductCode` | VARCHAR(50) | NO | Unique business product code |
| `CategoryId` | INT | YES | Optional foreign reference to `Category.CategoryId` |
| `Name` | NVARCHAR(200) | NO | Product name |
| `ImageUrl` | VARCHAR(500) | YES | Optional image asset URL |
| `Quantity` | DECIMAL(18,2) | NO | Available stock quantity (default 0.00) |
| `Price` | DECIMAL(18,2) | NO | Selling unit price (default 0.00) |
| `DeleteFlag` | BIT | NO | Soft-delete status (0 = Active, 1 = Deleted) |
| `CreatedAt` | DATETIME2 | NO | Creation timestamp (UTC, default `SYSUTCDATETIME()`) |
| `CreatedBy` | VARCHAR(50) | YES | Creator username |
| `UpdatedAt` | DATETIME2 | YES | Last update timestamp (UTC) |
| `UpdatedBy` | VARCHAR(50) | YES | Updater username |

- **Validation Rules**:
  - `ProductCode` and `Name` required; `ProductCode` must be unique among active products.
  - `Price` and `Quantity` cannot be negative.
  - If `CategoryId` is provided, the category must exist and have `DeleteFlag == false`.

---

### B. Organization Domain

#### 3. `Branch` Table
- **C# Entity**: `ModuPOS.Database.AppDbContextModels.Branch`
- **Primary Key**: `BranchId` (int, IDENTITY)
- **Soft Delete**: None (Deletion prohibited via API)
- **Audit Columns**: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- **Columns**:

| Column Name | Type | Nullable | Description |
|---|---|---|---|
| `BranchId` | INT IDENTITY(1,1) | NO | Clustered Primary Key |
| `BranchCode` | VARCHAR(50) | NO | Unique business code |
| `BranchName` | NVARCHAR(200) | NO | Branch display name |
| `Address` | NVARCHAR(500) | YES | Physical store address |
| `CreatedAt` | DATETIME2 | NO | Creation timestamp (UTC) |
| `CreatedBy` | VARCHAR(50) | YES | Creator identifier |
| `UpdatedAt` | DATETIME2 | YES | Last update timestamp (UTC) |
| `UpdatedBy` | VARCHAR(50) | YES | Updater identifier |

- **Validation Rules**:
  - `BranchCode` and `BranchName` required. `BranchCode` must be unique.

#### 4. `Staff` Table
- **C# Entity**: `ModuPOS.Database.AppDbContextModels.Staff`
- **Primary Key**: `StaffId` (int, IDENTITY)
- **Soft Delete**: `DeleteFlag` (bit)
- **Audit Columns**: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- **Columns**:

| Column Name | Type | Nullable | Description |
|---|---|---|---|
| `StaffId` | INT IDENTITY(1,1) | NO | Clustered Primary Key |
| `StaffCode` | VARCHAR(50) | NO | Unique employee code |
| `StaffName` | NVARCHAR(200) | NO | Full name |
| `DateOfBirth` | DATETIME2 | YES | Birth date |
| `Phone` | VARCHAR(50) | YES | Primary contact telephone |
| `Email` | VARCHAR(100) | YES | Email address |
| `Address` | NVARCHAR(500) | YES | Residential address |
| `BranchCode` | VARCHAR(50) | YES | Associated store branch code |
| `IsActive` | BIT | NO | Active employment status (default 1) |
| `DeleteFlag` | BIT | NO | Soft-delete status (0 = Active, 1 = Deleted) |
| `CreatedAt` | DATETIME2 | NO | Creation timestamp (UTC) |
| `CreatedBy` | VARCHAR(50) | YES | Creator identifier |
| `UpdatedAt` | DATETIME2 | YES | Last update timestamp (UTC) |
| `UpdatedBy` | VARCHAR(50) | YES | Updater identifier |

- **Validation Rules**:
  - `StaffCode` and `StaffName` required. `StaffCode` must be unique among active staff.
  - If `BranchCode` is provided, it should correspond to an active branch.

#### 5. `StaffRole` Table
- **C# Entity**: `ModuPOS.Database.AppDbContextModels.StaffRole`
- **Primary Key**: `StaffRoleId` (int, IDENTITY)
- **Soft Delete**: `DeleteFlag` (bit)
- **Audit Columns**: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- **Columns**:

| Column Name | Type | Nullable | Description |
|---|---|---|---|
| `StaffRoleId` | INT IDENTITY(1,1) | NO | Clustered Primary Key |
| `StaffRoleCode` | VARCHAR(50) | NO | Unique role title code |
| `StaffRoleName` | NVARCHAR(100) | NO | Job title / role name |
| `Salary` | DECIMAL(18,2) | YES | Base compensation |
| `DeleteFlag` | BIT | NO | Soft-delete status (0 = Active, 1 = Deleted) |
| `CreatedAt` | DATETIME2 | NO | Creation timestamp (UTC) |
| `CreatedBy` | VARCHAR(50) | YES | Creator identifier |
| `UpdatedAt` | DATETIME2 | YES | Last update timestamp (UTC) |
| `UpdatedBy` | VARCHAR(50) | YES | Updater identifier |

#### 6. `Shift` Table
- **C# Entity**: `ModuPOS.Database.AppDbContextModels.Shift`
- **Primary Key**: `ShiftId` (int, IDENTITY)
- **Soft Delete**: None (Deletion prohibited via API)
- **Audit Columns**: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- **Columns**:

| Column Name | Type | Nullable | Description |
|---|---|---|---|
| `ShiftId` | INT IDENTITY(1,1) | NO | Clustered Primary Key |
| `ShiftCode` | VARCHAR(50) | NO | Unique shift identifier |
| `ShiftType` | VARCHAR(50) | YES | Shift categorization (e.g. Morning, Evening) |
| `ShiftStartTime` | DATETIME2 | YES | Scheduled shift start |
| `ShiftEndTime` | DATETIME2 | YES | Scheduled shift end |
| `CreatedAt` | DATETIME2 | NO | Creation timestamp (UTC) |
| `CreatedBy` | VARCHAR(50) | YES | Creator identifier |
| `UpdatedAt` | DATETIME2 | YES | Last update timestamp (UTC) |
| `UpdatedBy` | VARCHAR(50) | YES | Updater identifier |

#### 7. `StaffShift` Table
- **C# Entity**: `ModuPOS.Database.AppDbContextModels.StaffShift`
- **Primary Key**: `StaffShiftId` (int, IDENTITY)
- **Soft Delete**: None
- **Columns**:

| Column Name | Type | Nullable | Description |
|---|---|---|---|
| `StaffShiftId` | INT IDENTITY(1,1) | NO | Clustered Primary Key |
| `StaffShiftCode` | VARCHAR(50) | NO | Unique assignment reference code |
| `StaffCode` | VARCHAR(50) | NO | Assigned staff member code |
| `ShiftCode` | VARCHAR(50) | NO | Assigned work shift code |

---

### C. Authentication & Authorization Domain

#### 8. `Role` Table
- **C# Entity**: `ModuPOS.Database.AppDbContextModels.Role`
- **Primary Key**: `id` (int, IDENTITY)
- **Soft Delete**: `is_deleted` (bit)
- **Columns**: `id`, `name`, `is_deleted`

#### 9. `Permission` Table
- **C# Entity**: `ModuPOS.Database.AppDbContextModels.Permission`
- **Primary Key**: `id` (int, IDENTITY)
- **Soft Delete**: `is_deleted` (bit)
- **Columns**: `id`, `name`, `is_deleted`

#### 10. `Role_Permission` Table
- **C# Entity**: `ModuPOS.Database.AppDbContextModels.RolePermission`
- **Primary Key**: `id` (int, IDENTITY)
- **Soft Delete**: `is_deleted` (bit)
- **Columns**: `id`, `role_id`, `permission_id`, `is_deleted`

#### 11. `User` Table
- **C# Entity**: `ModuPOS.Database.AppDbContextModels.User`
- **Primary Key**: `id` (int, IDENTITY)
- **Soft Delete**: `is_deleted` (bit)
- **Audit Columns**: `created_at`, `created_by`, `updated_at`, `updated_by`
- **Columns**: `id`, `role_id`, `username`, `password_hash`, `email`, `is_active`, `is_deleted`, `created_at`, `created_by`, `updated_at`, `updated_by`

#### 12. `Refresh_Token` Table
- **C# Entity**: `ModuPOS.Database.AppDbContextModels.RefreshToken`
- **Primary Key**: `id` (int, IDENTITY)
- **Soft Delete**: `revoked_at` (timestamp presence)
- **Columns**: `id`, `user_id`, `refresh_token` (mapped as `RefreshToken1`), `expires_at`, `created_at`, `revoked_at`

---

## 3. Standard Shared Envelopes

### `Pagination` & `PaginationRequest`
```csharp
public class PaginationRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class Pagination
{
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
```

### `PagedResult<T>`
```json
{
  "isSuccess": true,
  "message": "Success",
  "data": [ ... ],
  "pagination": {
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 42,
    "totalPages": 5,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```
