# Database Schema

This document contains full table list of ModuPOS. This is still an ongoing schema and have changes to make. 

### `Role`

| Column | Type | Nullable | Key | Notes |
| :--- | :--- | :--- | :--- | :--- |
| id | int | NO | PK |  |
| name | varchar(50) | NO |  |  |
| is_deleted | bit | NO |  |  |

### `Permission`

| Column | Type | Nullable | Key | Notes |
| :--- | :--- | :--- | :--- | :--- |
| id | int | NO | PK |  |
| name | varchar(50) | NO |  |  |
| is_deleted | bit | NO |  |  |

### `Role_Permission`

| Column | Type | Nullable | Key | Notes |
| :--- | :--- | :--- | :--- | :--- |
| id | int | NO | PK |  |
| role_id | int | NO |  |  |
| permission_id | int | NO |  |  |
| is_deleted | bit | NO |  |  |

### `User`

| Column | Type | Nullable | Key | Notes |
| :--- | :--- | :--- | :--- | :--- |
| id | int | NO | PK |  |
| role_id | int | NO |  |  |
| username | varchar(50) | NO |  |  |
| password_hash | varchar(512) | NO |  |  |
| email | varchar(100) | YES |  |  |
| is_active | bit | NO |  |  |
| is_deleted | bit | NO |  |  |
| created_at | datetime2 | NO |  |  |
| created_by | varchar(50) | YES |  |  |
| updated_at | datetime2 | YES |  |  |
| updated_by | varchar(50) | YES |  |  |

### `Refresh_Token`

| Column | Type | Nullable | Key | Notes |
| :--- | :--- | :--- | :--- | :--- |
| id | int | NO | PK |  |
| user_id | int | YES |  |  |
| refresh_token | varchar(512) | YES |  |  |
| expires_at | datetime2 | YES |  |  |
| created_at | datetime2 | NO |  |  |
| revoked_at | datetime2 | YES |  |  |

### `Branch`

| Column | Type | Nullable | Key | Notes |
| :--- | :--- | :--- | :--- | :--- |
| BranchId | int | NO | PK |  |
| BranchCode | varchar(50) | NO |  |  |
| BranchName | nvarchar(200) | NO |  |  |
| Address | nvarchar(500) | YES |  |  |
| CreatedAt | datetime2 | NO |  |  |
| CreatedBy | varchar(50) | YES |  |  |
| UpdatedAt | datetime2 | YES |  |  |
| UpdatedBy | varchar(50) | YES |  |  |

### `Staff`

| Column | Type | Nullable | Key | Notes |
| :--- | :--- | :--- | :--- | :--- |
| StaffId | int | NO | PK |  |
| StaffCode | varchar(50) | NO |  |  |
| StaffName | nvarchar(200) | NO |  |  |
| DateOfBirth | datetime2 | YES |  |  |
| Phone | varchar(50) | YES |  |  |
| Email | varchar(100) | YES |  |  |
| Address | nvarchar(500) | YES |  |  |
| BranchCode | varchar(50) | YES |  |  |
| IsActive | bit | NO |  |  |
| DeleteFlag | bit | NO |  |  |
| CreatedAt | datetime2 | NO |  |  |
| CreatedBy | varchar(50) | YES |  |  |
| UpdatedAt | datetime2 | YES |  |  |
| UpdatedBy | varchar(50) | YES |  |  |

### `StaffRole`

| Column | Type | Nullable | Key | Notes |
| :--- | :--- | :--- | :--- | :--- |
| StaffRoleId | int | NO | PK |  |
| StaffRoleCode | varchar(50) | NO |  |  |
| StaffRoleName | nvarchar(100) | NO |  |  |
| Salary | decimal(18,2) | YES |  |  |
| DeleteFlag | bit | NO |  |  |
| CreatedAt | datetime2 | NO |  |  |
| CreatedBy | varchar(50) | YES |  |  |
| UpdatedAt | datetime2 | YES |  |  |
| UpdatedBy | varchar(50) | YES |  |  |

### `Shift`

| Column | Type | Nullable | Key | Notes |
| :--- | :--- | :--- | :--- | :--- |
| ShiftId | int | NO | PK |  |
| ShiftCode | varchar(50) | NO |  |  |
| ShiftType | varchar(50) | YES |  |  |
| ShiftStartTime | datetime2 | YES |  |  |
| ShiftEndTime | datetime2 | YES |  |  |
| CreatedAt | datetime2 | NO |  |  |
| CreatedBy | varchar(50) | YES |  |  |
| UpdatedAt | datetime2 | YES |  |  |
| UpdatedBy | varchar(50) | YES |  |  |

### `StaffShift`

| Column | Type | Nullable | Key | Notes |
| :--- | :--- | :--- | :--- | :--- |
| StaffShiftId | int | NO | PK |  |
| StaffShiftCode | varchar(50) | NO |  |  |
| StaffCode | varchar(50) | NO |  |  |
| ShiftCode | varchar(50) | NO |  |  |

### `Category`

| Column | Type | Nullable | Key | Notes |
| :--- | :--- | :--- | :--- | :--- |
| CategoryId | int | NO | PK |  |
| CategoryCode | varchar(50) | NO |  |  |
| CategoryName | nvarchar(200) | NO |  |  |
| DeleteFlag | bit | NO |  |  |

### `Product`

| Column | Type | Nullable | Key | Notes |
| :--- | :--- | :--- | :--- | :--- |
| ProductId | int | NO | PK |  |
| ProductCode | varchar(50) | NO |  |  |
| CategoryId | int | YES |  |  |
| Name | nvarchar(200) | NO |  |  |
| ImageUrl | varchar(500) | YES |  |  |
| Quantity | decimal(18,2) | NO |  |  |
| Price | decimal(18,2) | NO |  |  |
| DeleteFlag | bit | NO |  |  |
| CreatedAt | datetime2 | NO |  |  |
| CreatedBy | varchar(50) | YES |  |  |
| UpdatedAt | datetime2 | YES |  |  |
| UpdatedBy | varchar(50) | YES |  |  |
