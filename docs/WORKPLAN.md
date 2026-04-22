# WORKPLAN

## Project Context

- Проект уже переведён на verification-driven архитектуру.
- Verification является source of truth для runtime status.
- Legacy fallback, date-based logic и `DriverStatusEvaluator` не должны возвращаться в runtime.
- Текущий продуктовый gap: приложение ещё не умеет честно направлять пользователя на точный официальный путь обновления именно его драйвера.

## Completed Baseline

- Verification-first pipeline уже выпущен и находится в `main`.
- Runtime status определяется детерминированно по verification.
- Hidden coupling между decision / validation / mapper / UI убран.
- `null` / invalid verification даёт safe default `NeedsReview`.

## Current Priority

- PRIORITY 1: честный official update flow без heuristic routing и без ложных direct links.
- Последняя завершённая задача: `feature/official-update-flow-foundation`.
- Ветка интеграции: `integration/honest-verification`.
- Следующий шаг: NVIDIA routing, ещё не начат.
- Main не трогался. Release в `main` не выполнялся.

## Active Tasks

- [Active] Нет активной task branch в работе. Следующая задача ещё не начата.

## Pending Tasks

- [Pending] Начать `feature/nvidia-official-update-routing` от `integration/honest-verification`.
- [Pending] Добавить exact official direct driver page routing для поддерживаемых vendor/device cases.
- [Pending] Добавить vendor app routing policy: direct page vs official app.
- [Pending] Подключить update action foundation к UI action wiring без ломки verification pipeline.
- [Pending] Добавить post-update rescan flow поверх verification-driven статуса.

## Completed Tasks

- [Completed] Verification-driven архитектура выпущена в `main`.
- [Completed] Создан `docs/WORKPLAN.md` как progress log для текущего workflow.
- [Completed] Добавлены `UpdateActionType`, `UpdateAction`, `DriverUpdateContext`, `IUpdateActionResolver`.
- [Completed] Добавлена детерминированная foundation-реализация `DeterministicUpdateActionResolver`.
- [Completed] Добавлены unit tests для foundation-логики update routing.
- [Completed] Выполнены `dotnet build` и `dotnet test` без ошибок.
- [Completed] Выполнен refinement текущей задачи до merge-ready состояния в рамках текущей ветки.
- [Completed] Проверено удаление `DriverUpdateAction`: скрытых runtime/UI/test dependencies не найдено.
- [Completed] Проверен rename `DriverUpdateActionType` -> `UpdateActionType`: скрытого breaking impact не найдено.
- [Completed] `feature/official-update-flow-foundation` fast-forward merged в `integration/honest-verification`.
- [Completed] Foundation для update flow внедрён в integration-ветку без касания `main`.

## Last Completed Task Status

- Последняя завершённая задача: foundation для exact official update flow реализован и влит в `integration/honest-verification`.
- Стартовый коммит текущей задачи: `b34c6a9`.
- Результат: build success, tests success (`180/180`).
- Merge status: `main` не трогался, release не выполнялся.
- Следующий логичный шаг: начать NVIDIA routing как отдельную task branch от `integration/honest-verification`.

## Important Constraints

- Никаких Google/search redirects.
- Никаких heuristic guesses или approximate matches.
- Только official source.
- Если точный verified route ещё не реализован, возвращать честный safe result.
- `WORKPLAN.md` используется как progress log и reference, но не заменяет прямые указания пользователя.

## Do not change without approval

- Базовую ветку и branch workflow.
- Verification-driven runtime logic.
- Decision layer и verification pipeline.
- Приоритет `PRIORITY 1`.
- Запрет на merge/push без явной команды.
