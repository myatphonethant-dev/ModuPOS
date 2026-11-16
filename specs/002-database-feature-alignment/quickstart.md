# Quickstart & Validation Guide: Database Feature Alignment

**Feature Branch**: `002-database-feature-alignment`
**Date**: 2026-11-16
**Status**: Completed

## 1. Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/) installed.
- Access to local MSSQL database server or In-Memory test database.
- Repository cloned at root `d:\ModuPOS`.

---

## 2. Solution Build & Clean Compilation Verification

Verify that all projects compile cleanly with zero errors after applying the updated Result and Pagination signatures:

```powershell
dotnet clean
dotnet build -c Release
```

Expected Outcome: `Build succeeded. 0 Warning(s), 0 Error(s)`.

---

## 3. Automated Test Execution

Run the complete test suite across unit tests and integration tests:

```powershell
# Run all unit tests
dotnet test tests/ModuPOS.Auth.UnitTests/ModuPOS.Auth.UnitTests.csproj -c Release

# Run API integration tests
dotnet test tests/ModuPOS.Api.IntegrationTests/ModuPOS.Api.IntegrationTests.csproj -c Release
```

Expected Outcome: All tests pass, verifying:
1. `AuthServiceTests`: Login, token generation, password hashing, and token revocation.
2. `ProductServiceTests`: Product creation, duplicate code rejection, soft delete (`DeleteFlag = true`).
3. `CategoryServiceTests`: Category CRUD and soft-deletion.
4. `OrganizationServiceTests`: Branch, Staff, StaffRole, and Shift assignment flows.
5. `ApiIntegrationTests`: Correct HTTP status codes and `PagedResult<T>` envelope serialization.

---

## 4. End-to-End API Scenario Validation

Start the API project:

```powershell
dotnet run --project ModuPOS.Api
```

### Scenario A: Paginated Product Query
```bash
curl -X GET "https://localhost:7123/api/products?pageNumber=1&pageSize=5" -H "Accept: application/json"
```

**Expected Response**:
```json
{
  "isSuccess": true,
  "message": "Success",
  "data": [ ...5 items... ],
  "pagination": {
    "pageNumber": 1,
    "pageSize": 5,
    "totalCount": 25,
    "totalPages": 5,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

### Scenario B: Soft-Deletion Verification
1. Call `DELETE /api/products/1`.
   - Expected: `200 OK` with `{"isSuccess": true, "message": "Product deleted successfully."}`
2. Call `GET /api/products/1`.
   - Expected: `404 Not Found` with `{"isSuccess": false, "message": "Product not found."}`
3. Call `GET /api/products?pageNumber=1&pageSize=10`.
   - Expected: The deleted product is absent from the list.

### Scenario C: Prohibited Branch Deletion
Attempting to issue a `DELETE` on a branch:
```bash
curl -X DELETE "https://localhost:7123/api/branches/1"
```
**Expected Response**: `405 Method Not Allowed` (or endpoint absent), preventing accidental deletion of branch operational records.
