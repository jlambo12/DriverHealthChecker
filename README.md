# DriverHealthChecker

## What the project does today

DriverHealthChecker is a Windows WPF application that scans locally installed drivers, groups a focused subset of user-relevant devices, and offers official follow-up actions where supported.

Current implementation capabilities:
- local driver inventory scan through WMI;
- device grouping for GPU, network, storage, audio, and OEM recommendation scenarios;
- action buttons for selected official vendor/OEM paths;
- rescan flow with local change detection;
- application self-update path through GitHub Releases and Velopack.

## Current implementation limitations

The current runtime implementation still contains legacy behavior that is being removed as part of the honest verification migration:
- driver health status is still influenced by local driver date and age;
- some decisions still depend on heuristics and approximate matching;
- some official actions still resolve to generic official landing pages instead of precise verification-backed update paths;
- rescan change detection is still mixed with health presentation.

These limitations are transitional and are not the target product model.

## Target product model

The project is being migrated to an official verification pipeline:

`Scan -> Identity -> OfficialSupportChannel -> OfficialVerification -> UpdateAction -> Presentation`

Target rules:
- no date-based status;
- no age-based status;
- no heuristic health guesses;
- `UpToDate` only when an official source confirms no newer version exists;
- `UpdateAvailable` only when an official source confirms a newer version exists;
- `UnableToVerifyReliably` when the project cannot complete reliable official verification;
- update actions must resolve to a valid official path, not a guess.

## Official path strategy

Allowed update paths are defined by policy and will be resolved explicitly per device/vendor scenario:
- `InstalledOfficialApp`
- `OfficialAppInstall`
- `DirectDriverPage`
- `ExactSupportPage`
- `ManualExplanation`

NVIDIA App / GeForce Experience support remains part of this strategy and will be preserved during migration.

## Source of truth documents

The current project source of truth is:
- `PROJECT_RULES.md` for process and delivery constraints;
- `ROADMAP.md` for migration stages;
- `docs/VERIFICATION_POLICY.md` for verification and update-path policy.

Older audit/MVP documents remain in the repository only until they are explicitly rewritten or removed during cleanup.

## Development environment

For automated environment setup on Windows use:

```powershell
.\scripts\setup-dev-env.ps1
```

The script installs the required .NET SDK if needed, validates the project configuration, and restores dependencies.
