# ROADMAP

Status values: `TODO`, `IN_PROGRESS`, `DONE`, `PARTIAL`, `BLOCKED`.

| Step | Title | Status | Notes |
|---|---|---|---|
| 0 | Baseline and branch setup | DONE | Dedicated branch created from local `main`, solution structure reviewed, active update path and setup scripts inspected, rules/roadmap files added |
| 1 | Fix action system behavior | DONE | Removed search fallback and dead-end/placeholder action paths; action resolver now returns official URLs/tools or safe truthful messages |
| 2 | Add OEM-first logic for laptops and OEM-bound devices | DONE | Added OEM-aware resolution for Huawei/Honor laptop-bound components (audio/support stack) with OEM priority |
| 3 | Fix Windows Update misuse | DONE | Removed blanket Windows Update fallback for storage and generic cases; retained truthful safe alternatives |
| 4 | Fix initial window size and layout visibility | DONE | Increased default/min sizes and updated filter row layout to improve visibility of reset button |
| 5 | Extract domain models from MainWindow.xaml.cs | DONE | Moved `DriverItem`, `DriverSnapshot`, `OfficialAction`, `OfficialActionKind` to `Models/` |
| 6 | Extract driver selection logic | DONE | Moved selection logic to `DriverSelectionService` and wired from `MainWindow` |
| 7 | Fix version comparison logic | DONE | Added `DriverVersionComparer` with numeric segment comparison and tests |
| 8 | Extract WMI scanning | DONE | Added dedicated WMI scanner service and raw scan model; MainWindow no longer queries Win32_PnPSignedDriver directly |
| 9 | Extract comparison/rescan logic | DONE | Moved comparison + snapshot creation to `DriverComparisonService` and added tests for updated/unchanged/new/missing cases |
| 10 | Reduce MainWindow.xaml.cs | DONE | Extracted mapping logic into `DriverScanMapper`; MainWindow now focuses on orchestration/UI |
| 11 | Strengthen tests | DONE | Added evaluator/comparison/selection/version/action-resolution/scan-mapper/report coverage for practical regression safety |
| 12 | Normalize setup/release path | DONE | Added delivery workflow doc and marked setup/release helper scripts as legacy/manual tooling |
| 13 | Add SDK pinning | DONE | Added `global.json` and pinned SDK settings |
| 14 | Add CI | DONE | Added focused GitHub Actions workflow for restore/build/test |
| 15 | Improve logging and diagnostics | DONE | Added practical scan lifecycle, filtering, action resolution/fallback, comparison, and report writing logs |
