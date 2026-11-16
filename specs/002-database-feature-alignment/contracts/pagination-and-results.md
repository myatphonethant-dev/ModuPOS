# API Contract: Shared Result Envelopes and Pagination Protocol

**Feature Branch**: `002-database-feature-alignment`
**Date**: 2026-11-16
**Status**: Completed

## 1. Protocol Overview

All REST API endpoints across ModuPOS return structured responses adhering strictly to the **Result Pattern** (`ModuPOS.Shared.Results`) and **Pagination Protocol** (`ModuPOS.Shared.Pagination`).

Raw database entities or uncaught internal exceptions are never returned directly.

---

## 2. Response Envelopes

### A. Non-Payload Result (`Result`)
Used for operational actions that do not yield data (e.g., token revocation, deletion):

```json
// Success
{
  "isSuccess": true,
  "message": "Product deleted successfully.",
  "isFailure": false
}

// Failure
{
  "isSuccess": false,
  "message": "Product not found or already deleted.",
  "isFailure": true
}
```

### B. Single Entity Result (`Result<T>`)
Used for single-resource lookups, creations, and updates:

```json
// Success
{
  "isSuccess": true,
  "message": "Success",
  "data": {
    "productId": 1,
    "productCode": "P001",
    "name": "Coca Cola 330ml",
    "price": 1200.00,
    "quantity": 50.00
  },
  "isFailure": false
}

// Failure
{
  "isSuccess": false,
  "message": "Product with code 'P001' already exists.",
  "data": null,
  "isFailure": true
}
```

### C. Paginated Collection Result (`PagedResult<T>`)
Used for 100% of collection query operations:

```json
{
  "isSuccess": true,
  "message": "Success",
  "data": [
    {
      "productId": 1,
      "productCode": "P001",
      "name": "Coca Cola 330ml",
      "price": 1200.00,
      "quantity": 50.00
    }
  ],
  "pagination": {
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  },
  "isFailure": false
}
```

---

## 3. Query Parameter Standards

Every collection GET endpoint binds parameters from query string:

| Parameter | Type | Default | Constraints | Description |
|---|---|---|---|---|
| `pageNumber` | integer | `1` | `>= 1` | The requested page sequence |
| `pageSize` | integer | `10` | `1 <= pageSize <= 100` | The number of records per page |

Domain-specific query filters (such as `categoryId`, `branchCode`, `search`) are accepted as additional query string parameters.

---

## 4. HTTP Status Code Mapping Matrix

| Result State | HTTP Status Code | Scenario |
|---|---|---|
| `IsSuccess == true` on Query/Update/Delete | `200 OK` | Record retrieved, updated, or operation completed |
| `IsSuccess == true` on Resource Creation | `201 Created` | New entity created; includes `Location` header to `GetById` |
| `IsFailure == true` on Not Found | `404 Not Found` | Entity with specified ID does not exist or is soft-deleted |
| `IsFailure == true` on Business/Validation Rule | `400 Bad Request` | Duplicate code, missing required field, invalid FK |
| Unauthenticated / Missing Bearer Token | `401 Unauthorized` | JWT access token missing or expired |
| Insufficient Permissions | `403 Forbidden` | Authenticated user lacks required permission |
