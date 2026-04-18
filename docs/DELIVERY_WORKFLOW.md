# DELIVERY_WORKFLOW

## Canonical update path
The canonical update mechanism for Driver Health Checker is **GitHub Releases** consumed by the installed application updater.

## Current setup artifacts
The repository still contains legacy setup/release helper files:
- `DriverHealthCheckerInstaller.iss`
- `publish_release.bat`
- `release_gh.bat`
- `upload_velopack_github.bat`
- `build_velopack_release.bat`

These files are treated as **legacy/manual tooling** and are not part of automated CI validation.

## Guardrails
1. Do not publish releases from feature/refactor branches.
2. Do not produce distribution artifacts as part of normal development validation.
3. Validate code through build/test checks and manual installed-app validation.
4. Release publication remains a manual, user-controlled process.
