# PROJECT_RULES

## Branch and delivery workflow
1. All work is performed in a dedicated branch created from `main`.
2. Working directly in `main` is forbidden.
3. Merging is not performed by Codex.
4. Publishing releases and distribution artifacts is forbidden in this branch.
5. Codex produces a candidate branch only; final merge/release decisions are manual.

## Versioning rules
1. Every code change must bump the application patch version.
2. `DriverHealthChecker.App/DriverHealthChecker.App.csproj` is the single source of truth for version.
3. Version-dependent files must remain consistent with the `.csproj` version.
4. Release publication is manual and performed later by the user.

## Product behavior and UX safety
1. Every UI action must trigger a real meaningful action.
2. Placeholder popups and dead-end actions are forbidden.
3. Google-search fallback actions are forbidden.
4. Misleading suggestions to upgrade Windows are forbidden.
5. The app supports both Windows 10 and Windows 11.
6. Windows Update can be suggested only when it is genuinely meaningful.
7. Driver source priority is OEM -> vendor official source -> vendor official tool -> meaningful Windows Update -> truthful safe fallback.

## Process and quality
1. Work is executed step-by-step according to `ROADMAP.md`.
2. Behavior stability has higher priority than cosmetic refactoring.
3. Full uncontrolled rewrites are forbidden.
4. Manual local validation is required before merge.
5. Progress reports to the user must be in Russian.
