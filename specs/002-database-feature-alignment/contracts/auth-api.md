# API Contract: Authentication & Access Control Module

**Feature Branch**: `002-database-feature-alignment`
**Date**: 2026-11-16
**Status**: Completed

## 1. Authentication Endpoints (`/api/auth`)

### `POST /api/auth/login`
- **Request Body**:
  ```json
  {
    "username": "admin",
    "password": "Password123!"
  }
  ```
- **Response**:
  - `200 OK` with `Result<LoginResponse>`
  - `400 Bad Request` if invalid credentials or inactive account
  ```json
  {
    "isSuccess": true,
    "message": "Success",
    "data": {
      "accessToken": "eyJhbGciOi...",
      "refreshToken": "xW7pQ...",
      "expiresAt": "2026-11-16T18:24:00Z",
      "userId": 1,
      "username": "admin",
      "roleId": 1,
      "roleName": "Administrator",
      "permissions": ["product.create", "staff.manage"]
    }
  }
  ```

### `POST /api/auth/refresh`
- **Request Body**: `{"refreshToken": "xW7pQ..."}`
- **Response**: `200 OK` with `Result<LoginResponse>` or `400 Bad Request`

### `POST /api/auth/revoke`
- **Request Body**: `{"refreshToken": "xW7pQ..."}`
- **Response**: `200 OK` with `Result` or `400 Bad Request`

---

## 2. Users Endpoints (`/api/users`)

### `GET /api/users`
- **Description**: Returns paginated list of non-deleted users.
- **Query Parameters**:
  - `pageNumber` (int, default: 1)
  - `pageSize` (int, default: 10)
- **Response**: `200 OK` with `PagedResult<UserResponseDto>`

### `GET /api/users/{id}`
- **Response**: `200 OK` with `Result<UserResponseDto>` or `404 Not Found`

### `POST /api/users`
- **Request Body**:
  ```json
  {
    "username": "cashier1",
    "password": "SecurePassword123!",
    "roleId": 2,
    "email": "cashier1@example.com",
    "createdBy": "admin"
  }
  ```
- **Response**: `201 Created` with `Result<UserResponseDto>` or `400 Bad Request`

### `PUT /api/users/{id}`
- **Request Body**:
  ```json
  {
    "roleId": 2,
    "email": "updated_cashier1@example.com",
    "isActive": true,
    "updatedBy": "admin"
  }
  ```
- **Response**: `200 OK` with `Result<UserResponseDto>` or `404 Not Found`

### `DELETE /api/users/{id}`
- **Description**: Soft-deletes user (`is_deleted = 1`).
- **Response**: `200 OK` with `Result` or `404 Not Found`

---

## 3. Roles Endpoints (`/api/roles`)

### `GET /api/roles`
- **Description**: Returns paginated list of non-deleted roles.
- **Query Parameters**: `pageNumber` (int, default: 1), `pageSize` (int, default: 10)
- **Response**: `200 OK` with `PagedResult<RoleDto>`

### `GET /api/roles/{id}`
- **Response**: `200 OK` with `Result<RoleDto>` or `404 Not Found`

### `POST /api/roles`
- **Request Body**:
  ```json
  {
    "name": "Supervisor",
    "permissionIds": [1, 2, 5]
  }
  ```
- **Response**: `201 Created` with `Result<RoleDto>` or `400 Bad Request`

### `PUT /api/roles/{id}`
- **Response**: `200 OK` with `Result<RoleDto>` or `404 Not Found`

### `DELETE /api/roles/{id}`
- **Description**: Soft-deletes role and related role-permissions (`is_deleted = 1`).
- **Response**: `200 OK` with `Result` or `404 Not Found`

---

## 4. Permissions Endpoints (`/api/permissions`)

### `GET /api/permissions`
- **Description**: Returns paginated list of non-deleted permissions.
- **Query Parameters**: `pageNumber` (int, default: 1), `pageSize` (int, default: 10)
- **Response**: `200 OK` with `PagedResult<PermissionDto>`

### `POST /api/permissions`
- **Request Body**: `{"name": "inventory.transfer"}`
- **Response**: `201 Created` with `Result<PermissionDto>` or `400 Bad Request`
