# Contract: Permission Catalog

Each module declares its permissions in code; the catalog is synced to the `Permission` table at startup (insert new, mark removed as retired, strip retired from roles, bump every `AuthorizationVersion`).

Code format: `<module>.<action>`, lowercase, dot-separated. Feature codes come from `ModuPOS.FeatureManagement.Features.FeatureRegistry.FeatureCodes`.

## Permissions introduced by this feature

| Code | Group | Feature | Depends on | Scope |
|------|-------|---------|-----------|-------|
| `role.view` | Roles | POS.AUTH | — | Business |
| `role.manage` | Roles | POS.AUTH | `role.view` | Business |
| `role.assign` | Roles | POS.AUTH | `role.view` | Business |
| `audit.view` | Audit | POS.AUTH | — | Business |
| `platform.audit.view` | Platform | — | — | Platform |

Later modules add their own (e.g. `sale.create`, `sale.refund` → depends `sale.view`, feature `POS.SALE`).

## Default roles created per business

| Role | Permissions |
|------|-------------|
| Owner (IsOwner, immutable) | All Business-scope, non-retired, feature-enabled permissions — computed, so new permissions are included automatically |
| Manager (system, editable) | `role.view`, `role.assign`, `audit.view` + module manager permissions as they are added |
| Cashier (system, editable) | Module cashier permissions as they are added (none from this feature) |
