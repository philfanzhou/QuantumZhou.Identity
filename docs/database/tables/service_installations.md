# service_installations

Shared ServiceMantle installation-state table, mapped into the SignaCore model by
`AddServiceMantleInstallation()` and keyed by service identifier. One row per service; SignaCore uses
the single service id `signacore`.

> Added by ServiceMantle issue #70 as a purely additive slice. Nothing reads this table at runtime
> yet: `installation_state` remains the sole runtime authority for anonymous-setup protection. This
> table is activated by a later startup-phase task. Until then it is written only by the adoption
> backfill in the `AddServiceInstallations` migration.

## Columns

- service_id (string, max 128, primary key) — canonical service identifier (`signacore`)
- status (integer) — ServiceMantle `InstallationStatus`: `PendingSetup` (0) or `Completed` (1)
- created_at_utc (timestamp, not null)
- completed_at_utc (timestamp, nullable) — present only once `Completed`
- version (integer, not null) — optimistic concurrency token
- setup_code_generation (integer, not null, default 0) — non-sensitive setup-code issuance counter
- setup_code_digest (string, max 74, nullable) — `sha256-v1:` digest of an issued setup code; null on
  completed rows
- setup_code_issued_at_utc (timestamp, nullable)
- setup_code_expires_at_utc (timestamp, nullable)

Timestamps use the ServiceMantle mapping's provider-default storage (`timestamp with time zone` on
PostgreSQL, `TEXT` on SQLite), which is intentionally isolated to this table and differs from the
SignaCore `DateTimeOffset`/Unix-microsecond convention used elsewhere.

## Relationships and invariants

- The ServiceMantle store enforces these invariants on read: `version >= 1`, `created_at_utc` is not
  the default value, `PendingSetup` carries no `completed_at_utc`, and `Completed` requires
  `completed_at_utc >= created_at_utc`.
- Adoption backfill (fail-closed): the migration inserts a single `Completed` row for `signacore`
  whenever the database is anything other than a brand-new empty install — that is, when
  `installation_state.status = Completed` or any business data exists. A Pending singleton with no
  business data, and a brand-new empty database, stay empty so anonymous setup remains open for a
  not-yet-completed install. This guarantees that once a later task reads this table, an upgraded
  database is never classified as `PendingSetup` and never re-exposes anonymous setup.
- The legacy `installation_state.setup_code_hash` is not carried over: a `Completed` row holds no
  setup-code material, and the legacy hash format is not migratable.

## Ownership

SignaCore owns this table's migrations and every save/transaction boundary, per the ServiceMantle
persistence contract. The ServiceMantle packages never generate or run migrations and never commit a
SignaCore work unit. Today the only writer is the adoption migration; once activated, writes flow
through the ServiceMantle installation/setup stores under a SignaCore-owned transaction.
