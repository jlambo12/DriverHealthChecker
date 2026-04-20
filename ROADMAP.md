# ROADMAP

Status values: `TODO`, `IN_PROGRESS`, `DONE`, `BLOCKED`.

## Migration objective

Driver Health Checker is being migrated to an honest verification model where driver health is determined only by verifiable official sources.

The target pipeline is:

`Scan -> Identity -> OfficialSupportChannel -> OfficialVerification -> UpdateAction -> Presentation`

## Product rules enforced by this roadmap

1. Driver status must not be derived from date, age, heuristics, assumptions, or local rescan deltas.
2. `UpToDate` is valid only when an official source confirms that no newer version exists.
3. `UpdateAvailable` is valid only when an official source confirms that a newer version exists.
4. `UnableToVerifyReliably` must be used when official verification cannot be completed with sufficient confidence.
5. Update actions must resolve to one of the approved official paths defined in `docs/VERIFICATION_POLICY.md`.
6. NVIDIA App / GeForce Experience support must be preserved as part of the general official app strategy.

## Stages

| Stage | Title | Status | Scope |
|---|---|---|---|
| 1 | Docs & policy alignment | IN_PROGRESS | Rewrite project source-of-truth documents for the honest verification model |
| 2 | Domain contracts | TODO | Introduce verification-oriented models, enums, and contracts without switching behavior yet |
| 3 | Inventory enrichment | TODO | Expand local scan data to capture stable device and package identity for official verification |
| 4 | Support channel resolution | TODO | Resolve precise official support channels and official app paths per device/vendor/OEM |
| 5 | Official verification | TODO | Add vendor/OEM verification logic that confirms update availability or inability to verify reliably |
| 6 | UI migration | TODO | Rework UI, filters, and summaries around the new verification statuses and action model |
| 7 | Cleanup | TODO | Remove legacy date-based status logic, obsolete documents, and transitional compatibility code |

## Stage details

### 1. Docs & policy alignment

Goal:
- make the new product model explicit before changing runtime behavior;
- define the verification policy and migration order;
- remove documentation ambiguity around legacy date-based logic.

Deliverables:
- updated `README.md`;
- updated `ROADMAP.md`;
- new `docs/VERIFICATION_POLICY.md`.

Done criteria:
- project source-of-truth documents no longer describe date-based health status as valid target behavior;
- the migration stages are documented and ordered;
- status semantics and allowed update paths are defined in writing.

### 2. Domain contracts

Goal:
- introduce contracts for the honest verification pipeline before implementation details are migrated.

Scope:
- define verification-oriented status model;
- define official support channel model;
- define verification result model;
- separate change tracking from health status.

Done criteria:
- runtime code can represent `UpToDate`, `UpdateAvailable`, and `UnableToVerifyReliably` explicitly;
- runtime code can represent official support channels without falling back to generic action guesses.

### 3. Inventory enrichment

Goal:
- collect enough local driver and device identity data to support precise official verification.

Scope:
- expand scanned driver inventory beyond display name/manufacturer/version/date;
- capture stable identifiers needed for vendor/OEM mapping;
- keep scanning isolated from status calculation.

Done criteria:
- the scan layer exposes the data required for support-channel resolution and verification;
- status is still not inferred from local metadata alone.

### 4. Support channel resolution

Goal:
- resolve the best official update channel for each supported device scenario.

Scope:
- distinguish between installed official apps, official app install pages, direct driver pages, exact support pages, and manual explanations;
- preserve and generalize existing NVIDIA app support;
- keep OEM-first behavior where OEM support is the authoritative path.

Done criteria:
- update actions are derived from explicit official channel selection instead of generic URLs and string heuristics alone;
- the project can distinguish when an official app is preferable to a direct page.

### 5. Official verification

Goal:
- determine driver health only through reliable official-source checks.

Scope:
- implement verification orchestrator and vendor/OEM verifiers;
- report verified newer version availability;
- explicitly return `UnableToVerifyReliably` when verification is incomplete or unsupported.

Done criteria:
- no driver health status depends on driver date, age, or assumptions;
- unsupported verification paths do not silently degrade into "check" or "attention" style statuses.

### 6. UI migration

Goal:
- align presentation, filtering, and user messaging with the new verification model.

Scope:
- replace legacy status labels and styling;
- surface verification outcomes and verification failure reasons clearly;
- keep change history and rescan information as secondary context rather than health status.

Done criteria:
- UI shows only verification-based health states;
- UI actions match resolved official channels;
- summary text and filters reflect the new status contract.

### 7. Cleanup

Goal:
- remove obsolete logic, documents, and compatibility layers after the new model is active.

Scope:
- delete legacy date-based status evaluators;
- remove outdated roadmap/audit documents;
- simplify transitional mappings and dead code.

Done criteria:
- the repository contains one coherent product model;
- no maintained code path depends on legacy date-based status logic;
- documentation matches shipped behavior and migration policy.
