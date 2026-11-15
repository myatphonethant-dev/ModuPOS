# Contract: Audit API

## GET /api/audit — `audit.view` (business)
Query: `from`, `to` (≤ 1 year back), `code[]`, `actorUserId`, `branchId`, `severity`, `page`, `pageSize` (≤ 200).
200 `{ items: [AuditEntry], page, total }`. Viewing is not audited (FR-039).

## GET /api/platform/businesses/{businessId}/audit — `platform.audit.view` (platform)
Same query plus required `reason` (5–500 chars). 400 `reason_required`. Each call writes `AUDIT.VIEWED_BY_PLATFORM` with business, filters, reason.

No create, update or delete endpoints exist. `AuditEntry` shape: see [data-model.md](../data-model.md#auditentry-t-businessid-nullable-for-platformpre-tenant-events). `Details` never contains passwords or tokens (FR-035).

## Internal: IAuditWriter (ModuPOS.Shared)
```text
WriteCritical(entry)   // joins current DB transaction; throws → caller's change fails
Enqueue(entry)         // non-blocking; queued, deduped (1 min), retried; alert after 5 min unsaved
```
Codes are restricted to the FR-033 catalog constants; unknown codes are rejected at compile time (constants) and at runtime (validation).
