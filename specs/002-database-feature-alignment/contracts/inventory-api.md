# API Contract: Inventory Module

**Feature Branch**: `002-database-feature-alignment`
**Date**: 2026-11-16
**Status**: Completed

## 1. Categories Endpoints (`/api/categories`)

### `GET /api/categories`
- **Description**: Returns a paginated list of non-deleted categories.
- **Query Parameters**:
  - `pageNumber` (int, default: 1)
  - `pageSize` (int, default: 10)
- **Response**: `200 OK` with `PagedResult<CategoryResponseDto>`

### `GET /api/categories/{id}`
- **Description**: Returns a single category by primary key.
- **Response**:
  - `200 OK` with `Result<CategoryResponseDto>`
  - `404 Not Found` if missing or soft-deleted

### `POST /api/categories`
- **Description**: Creates a new category.
- **Request Body**:
  ```json
  {
    "categoryCode": "CAT-BEV",
    "categoryName": "Beverages"
  }
  ```
- **Response**:
  - `201 Created` with `Result<CategoryResponseDto>`
  - `400 Bad Request` if duplicate code or empty fields

### `PUT /api/categories/{id}`
- **Description**: Updates an existing category.
- **Request Body**:
  ```json
  {
    "categoryCode": "CAT-BEV",
    "categoryName": "Cold Beverages"
  }
  ```
- **Response**:
  - `200 OK` with `Result<CategoryResponseDto>`
  - `404 Not Found` if missing or deleted
  - `400 Bad Request` if duplicate code

### `DELETE /api/categories/{id}`
- **Description**: Soft-deletes a category (`DeleteFlag = 1`).
- **Response**:
  - `200 OK` with `Result`
  - `404 Not Found` if missing or already deleted

---

## 2. Products Endpoints (`/api/products`)

### `GET /api/products`
- **Description**: Returns a paginated list of active products with optional category and search filters.
- **Query Parameters**:
  - `categoryId` (int?, optional)
  - `search` (string?, optional — matches name or product code)
  - `pageNumber` (int, default: 1)
  - `pageSize` (int, default: 10)
- **Response**: `200 OK` with `PagedResult<ProductResponseDto>`

### `GET /api/products/{id}`
- **Description**: Retrieves single product by ID.
- **Response**:
  - `200 OK` with `Result<ProductResponseDto>`
  - `404 Not Found` if missing or deleted

### `POST /api/products`
- **Description**: Creates a new product.
- **Request Body**:
  ```json
  {
    "productCode": "P-COKE-330",
    "categoryId": 1,
    "name": "Coca Cola 330ml Can",
    "price": 1500.00,
    "quantity": 100.00,
    "imageUrl": "https://cdn.example.com/coke.png",
    "createdBy": "admin"
  }
  ```
- **Response**:
  - `201 Created` with `Result<ProductResponseDto>`
  - `400 Bad Request` on validation failure

### `PUT /api/products/{id}`
- **Description**: Updates product details.
- **Request Body**:
  ```json
  {
    "productCode": "P-COKE-330",
    "categoryId": 1,
    "name": "Coca Cola 330ml Can (Cold)",
    "price": 1600.00,
    "quantity": 90.00,
    "imageUrl": "https://cdn.example.com/coke.png",
    "updatedBy": "admin"
  }
  ```
- **Response**:
  - `200 OK` with `Result<ProductResponseDto>`
  - `400 Bad Request` on duplicate code or invalid category
  - `404 Not Found` if not found

### `DELETE /api/products/{id}`
- **Description**: Soft-deletes a product (`DeleteFlag = 1`).
- **Response**:
  - `200 OK` with `Result`
  - `404 Not Found` if not found or already deleted
