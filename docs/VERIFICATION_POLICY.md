# VERIFICATION_POLICY

## Purpose

This document defines the mandatory product policy for driver verification, status assignment, and update-path resolution.

It is the policy source of truth for the migration to the honest verification model.

## Allowed driver statuses

Only the following driver health statuses are valid in the target model:

### `UpToDate`

Meaning:
- an official source confirms that no newer driver version is available for the verified device or package.

Requirements:
- the verification source must be official;
- the verification result must be specific enough to the device, package, vendor app flow, or OEM support channel being used;
- the application must be able to explain which official verification path produced the result.

### `UpdateAvailable`

Meaning:
- an official source confirms that a newer driver version is available for the verified device or package.

Requirements:
- the newer version must be confirmed by an official source;
- the application must expose the corresponding official update path.

### `UnableToVerifyReliably`

Meaning:
- the application could not complete sufficiently reliable official verification.

Requirements:
- this state must be explicit;
- the UI must not silently convert this state into a guessed "outdated", "needs review", or similar status;
- the application should preserve the best available official action path or a manual explanation when possible.

## What counts as valid official verification

Official verification is valid only when the project can trace the result to an official source appropriate for the device scenario.

Examples of valid official verification:
- an installed official vendor app that is the vendor-supported update mechanism for the device category;
- an official vendor app install path when that app is the intended authoritative update channel;
- an official direct driver page that clearly corresponds to the verified device, family, package, or update flow;
- an exact OEM support page for the specific device family/model when OEM support is the authoritative source;
- another official vendor/OEM verification mechanism that can explicitly confirm whether a newer version exists.

## What is NOT valid official verification

The following inputs must not be used to determine driver health status:
- driver date;
- driver age;
- local age thresholds;
- heuristic assumptions;
- keyword matches used as health evidence;
- local version/date changes across rescans;
- generic "probably outdated" reasoning;
- generic landing pages that do not actually verify update availability;
- non-official mirrors, search results, forum posts, or community guesses.

These signals may still exist as diagnostic or routing context, but they are not valid status evidence.

## Allowed update path types

The update action model must resolve to one of the following path types.

### `InstalledOfficialApp`

Definition:
- the required official vendor app is already installed locally and is the best supported path for update handling.

Examples:
- NVIDIA App / GeForce Experience when it is the authoritative supported path for the detected device scenario.

### `OfficialAppInstall`

Definition:
- an official vendor app is the best supported path, but it is not installed locally, so the user is sent to its official installation page.

### `DirectDriverPage`

Definition:
- the user can be sent directly to a precise official driver/update page without introducing unnecessary vendor software.

### `ExactSupportPage`

Definition:
- the most accurate official path is a device-specific or family-specific OEM/vendor support page rather than a direct driver package page.

### `ManualExplanation`

Definition:
- the application cannot provide a reliable direct navigation target, so it must present an honest explanation of the limitation and the safest official direction available.

## Path selection rules

1. No guessing.
2. No indirect assumptions.
3. If verification is not reliable, the application must explicitly say so.
4. If an official app is the best supported path, prefer the app over a broader direct page.
5. If a precise direct update page is available and better than extra software, prefer the direct page.
6. Generic official landing pages must not be presented as precise update paths unless they are genuinely the best official route available.
7. Manual explanations must be truthful and must not pretend to be verification.

## Transitional note

Legacy runtime code may still contain date-based and heuristic behavior during migration. That legacy behavior is temporary and does not change this policy.
