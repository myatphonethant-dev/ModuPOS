# API Contract: Organization Module

**Feature Branch**: `002-database-feature-alignment`
**Date**: 2026-11-16
**Status**: Completed

## 1. Branches Endpoints (`/api/branches`)

*Note: In accordance with Decision 3 (Q1: Option A), branch deletion is prohibited via the API.*

### `GET /api/branches`
- **Description**: Returns paginated branches.
- **Query Parameters**:
  - `pageNumber` (int, default: 1)
  - `pageSize` (int, default: 10)
- **Response**: `200 OK` with `PagedResult<BranchResponseDto>`

### `GET /api/branches/{id}`
- **Description**: Retrieves single branch by primary key.
- **Response**: `200 OK` with `Result<BranchResponseDto>` or `404 Not Found`

### `POST /api/branches`
- **Description**: Creates a new branch.
- **Request Body**:
  ```json
  {
    "branchCode": "BR-MANDALAY-01",
    "branchName": "Mandalay Central Store",
    "address": "78th St, Mandalay",
    "createdBy": "admin"
  }
  ```
- **Response**: `201 Created` with `Result<BranchResponseDto>` or `400 Bad Request`

### `PUT /api/branches/{id}`
- **Description**: Updates branch metadata.
- **Request Body**:
  ```json
  {
    "branchName": "Mandalay Downtown Store",
    "address": "78th & 30th St, Mandalay",
    "updatedBy": "admin"
  }
  ```
- **Response**: `200 OK` with `Result<BranchResponseDto>` or `404 Not Found`

---

## 2. Staff Endpoints (`/api/staff`)

### `GET /api/staff`
- **Description**: Returns paginated list of active staff members with optional branch filter.
- **Query Parameters**:
  - `branchCode` (string?, optional)
  - `pageNumber` (int, default: 1)
  - `pageSize` (int, default: 10)
- **Response**: `200 OK` with `PagedResult<StaffResponseDto>`

### `GET /api/staff/{id}`
- **Response**: `200 OK` with `Result<StaffResponseDto>` or `404 Not Found`

### `POST /api/staff`
- **Request Body**:
  ```json
  {
    "staffCode": "STF-001",
    "staffName": "Aung Aung",
    "dateOfBirth": "1995-05-12T00:00:00Z",
    "phone": "+95912345678",
    "email": "aungaung@example.com",
    "address": "No. 12, Main Road, Yangon",
    "branchCode": "BR-YGN-01",
    "createdBy": "admin"
  }
  ```
- **Response**: `201 Created` with `Result<StaffResponseDto>` or `400 Bad Request`

### `PUT /api/staff/{id}`
- **Response**: `200 OK` with `Result<StaffResponseDto>` or `404 Not Found`

### `DELETE /api/staff/{id}`
- **Description**: Soft-deletes staff member (`DeleteFlag = 1`).
- **Response**: `200 OK` with `Result` or `404 Not Found`

---

## 3. Staff Roles Endpoints (`/api/staff/roles`)

### `GET /api/staff/roles`
- **Description**: Returns paginated list of non-deleted staff roles.
- **Query Parameters**: `pageNumber` (int), `pageSize` (int)
- **Response**: `200 OK` with `PagedResult<StaffRoleResponseDto>`

### `POST /api/staff/roles`
- **Request Body**:
  ```json
  {
    "staffRoleCode": "ROLE-CASHIER",
    "staffRoleName": "Front Desk Cashier",
    "salary": 350000.00,
    "createdBy": "admin"
  }
  ```
- **Response**: `201 Created` with `Result<StaffRoleResponseDto>` or `400 Bad Request`

### `PUT /api/staff/roles/{id}`
- **Response**: `200 OK` with `Result<StaffRoleResponseDto>` or `404 Not Found`

### `DELETE /api/staff/roles/{id}`
- **Description**: Soft-deletes staff role (`DeleteFlag = 1`).
- **Response**: `200 OK` with `Result` or `404 Not Found`

---

## 4. Shifts Endpoints (`/api/shifts`)

*Note: In accordance with Decision 3 (Q1: Option A), shift deletion is prohibited via the API.*

### `GET /api/shifts`
- **Description**: Returns paginated list of operational shifts.
- **Query Parameters**: `pageNumber` (int), `pageSize` (int)
- **Response**: `200 OK` with `PagedResult<ShiftResponseDto>`

### `POST /api/shifts`
- **Request Body**:
  ```json
  {
    "shiftCode": "SHIFT-MORN-01",
    "shiftType": "Morning",
    "shiftStartTime": "2026-11-16T08:00:00Z",
    "shiftEndTime": "2026-11-16T16:00:00Z",
    "createdBy": "admin"
  }
  ```
- **Response**: `201 Created` with `Result<ShiftResponseDto>` or `400 Bad Request`

### `PUT /api/shifts/{id}`
- **Response**: `200 OK` with `Result<ShiftResponseDto>` or `404 Not Found`

### `POST /api/shifts/assign`
- **Description**: Assigns a staff member to a scheduled shift.
- **Request Body**:
  ```json
  {
    "staffShiftCode": "SS-20261116-001",
    "staffCode": "STF-001",
    "shiftCode": "SHIFT-MORN-01"
  }
  ```
- **Response**: `200 OK` with `Result<StaffShiftResponseDto>` or `400 Bad Request`

### `GET /api/shifts/staff-shifts`
- **Description**: Returns paginated staff shift assignments.
- **Query Parameters**:
  - `shiftCode` (string?, optional)
  - `pageNumber` (int, default: 1)
  - `pageSize` (int, default: 10)
- **Response**: `200 OK` with `PagedResult<StaffShiftResponseDto>`
