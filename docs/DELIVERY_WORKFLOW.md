# DELIVERY_WORKFLOW

## Canonical update path
The canonical update mechanism for Driver Health Checker is **GitHub Releases** consumed by the installed application updater (Velopack + `GithubSource`).

## Setup/release helper scripts
The repository keeps manual helper scripts for packaging/publishing workflows:
- `build_velopack_release.bat` — build self-contained package and `Releases/` artifacts.
- `publish_release.bat` — local publish helper (does **not** publish app updates).
- `release_gh.bat` — full build + upload flow with branch/clean-tree guardrails.
- `upload_velopack_github.bat` — upload existing `Releases/` artifacts (supports `--draft`).

These scripts are optional operational tooling. The canonical update path for users remains GitHub Releases.

## Guardrails
1. Do not publish releases from feature/refactor branches.
2. Do not produce distribution artifacts as part of normal development validation.
3. Validate code through build/test checks and manual installed-app validation.
4. Release publication remains a manual, user-controlled process.

## Recommended release flow (CI first)
1. Bump `<Version>` in `DriverHealthChecker.App.csproj`.
2. Commit and push changes to `main`.
3. Create and push tag `v<version>` (example: `v1.0.44`).
4. GitHub Actions workflow `.github/workflows/release.yml` builds, tests, packs, and publishes the release assets.

## Manual fallback flow
If CI release is unavailable, use the local scripts:
1. `release_gh.bat <version>` for full local build + upload.
2. or `build_velopack_release.bat <version>` then `upload_velopack_github.bat <version> --draft` for staging validation.

## Why CI release is better
- release runs only from tags (`v*`), reducing accidental publishes;
- build/test and packaging are deterministic on clean runner;
- uses GitHub token via workflow permissions instead of local env handling;
- stores packed artifacts as workflow artifacts for audit/debug.
