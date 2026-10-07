# Native GenOpt in Grasshopper — PR4 record

Branch `feature/native-genopt-grasshopper`, base `sow/2026-Q4` @ `0e62cf04`. The fourth PR of the Java-free GenOpt
replacement:
- SAM#182 (PR1, oracle), SAM_Tas#85 (PR1-T, Gate T protocol);
- SAM#183 (PR2, SAM.Math kernel, merged `0989ad81`; SAM `sow/2026-Q4` @ `79c101c2` used here);
- SAM_Tas#86 (PR3, `GenOptDocument.RunNative`, merged `63a5fec7`; SAM_Tas `sow/2026-Q4` @ `da891dd2` used here).

## 1. Status

Implementation, automated validation and real Rhino/Grasshopper acceptance complete. PR open for owner review. Not
merged. `PROJECT_PROGRESS.md` is not touched on this branch (post-merge closeout).

## 2. What changed

The existing component **SAMAnalytical.GenOpt** (GUID `5259b075-7da6-4d20-8364-67225c43dc4c`, SAM WIP > Tas) now runs
PR3's `GenOptDocument.RunNative` (SAM.Math kernel → `TasGenExecuteObjectiveEvaluator` → TasGenExecute → Tas) instead
of the legacy `GenOptDocument.Run()` (GenOpt.bat → java → cmd, registry project directory). No adapter, evaluator,
workspace, protocol or algorithm logic is duplicated in Grasshopper.

| File | Role |
|---|---|
| `Component/SAMAnalyticalGenOpt.cs` | Same GUID/name/category/inputs. Builds the document, calls `Modify.RunNative`, maps the report to outputs and runtime messages. Version 1.0.1 → 1.0.2. SPDX header added. |
| `Create/GenOptDocument.cs` | The component's existing input → `GenOptDocument` mapping, moved unchanged so it can be tested. |
| `Modify/RunNative.cs` | `RunNative(GenOptDocument)`: `ProgressWindowHost` dialog with Cancel on its own UI thread (the `RunWorkflow` pattern), cancel token, dispose-then-observe. The dialog-free overload passes a synchronous `IProgress` and returns refusals as an exception value. |
| `Classes/NativeGenOptReport.cs` | Kernel `OptimisationResult` (or refusal) → `successful`, outputs and messages (`SAM.Core.Log`; no Grasshopper types, so CI can test it). |
| `SAM.Analytical.Grasshopper.Tas.GenOpt.csproj` | HintPaths to `SAM.Core`, `SAM.Math` (SAM\build) and `SAM.Core.Windows` (SAM_Windows\build). |
| `Tests/SAM.Analytical.Grasshopper.Tas.GenOpt.Tests/` (new) | Boundary tests, see `TESTING.md`. Not in the solution. |
| `.github/workflows/build.yml` | One step: `dotnet test` of the new project after the solution build. |

### Component contract

| | Before (1.0.1) | After (1.0.2) |
|---|---|---|
| GUID / name / nickname / category | `5259b075-…` / SAMAnalytical.GenOpt / same / SAM WIP > Tas | unchanged |
| Inputs | 0 `_scriptPath` (FilePath, item), 1 `_parameters` (GooParameter, list), 2 `_objectives` (GooObjective, list), 3 `_algorithm_` (GooAlgorithm, item, optional, default GoldenSection), 4 `_run` (Boolean, item) | **unchanged** (names, order, types, access, defaults) |
| Outputs | 0 `successful` (Boolean) | 0 `successful` unchanged; appended 1 `outcome` (String), 2 `simulations` (Integer), 3 `bestPoint` (Number list), 4 `bestObjectives` (Number list), 5 `runDirectory` (String) |
| `successful` | true whenever `Run()` returned | true when the run ended normally (Success, simulation limit, nullspace) and was not cancelled |
| Execution | `Run()`: Java GenOpt, `cmd /c start`, `SetProjectDirectory` (registry) | `RunNative`: no Java, jar, cmd, registry, watcher or listing file |

A placed (1.0.1) component keeps its single saved output and its wiring; Grasshopper marks it for an advisory update,
and the new outputs can be added from the zoomable UI. New placements show all six.

### Behaviour and decisions

- **Runs only on `_run = true`**, synchronously on the Grasshopper UI thread, one evaluation at a time (as before). The
  progress dialog runs on its own thread and stays clickable; Grasshopper itself is busy until the run ends (UX
  limitation, same as the Tas workflow component). No new async framework.
- **Progress** is the kernel's `OptimisationProgress`, delivered synchronously (`Progress<T>` would post to the blocked
  UI thread). Text: `[n/MaxIte] step | total | Simulation n: <params> -> <objective> (lowest …)`. Golden section
  evaluates its first two points before reporting, so the first caption is number-free.
- **Cancel** (dialog button): cooperative. A running TasGenExecute finishes; the kernel stops before the next
  evaluation. Reported as a remark, `successful = false`, no best point. A cancel observed only after the run returned,
  or an unconfirmed dialog shutdown, withholds the result (same rule as `RunWorkflow`).
- **Best point**: kernel `Minimum` (pattern search); golden section reports none, so the lowest-objective entry, first
  on ties, NaN skipped (the PR3 acceptance definition). Golden section also reports the final interval.
- **`simulations`** is the kernel count. After a cancel it includes the number assigned when the cancel was observed
  (frozen PR2 convention; e.g. 3 when 2 evaluations ran); the message says so rather than implying 3 runs.
- **Errors**: refused algorithm (`NotSupportedException`) / settings (`GenOptCompatibilityException`), missing
  TasGenExecute, missing workspace and evaluation failures (with the evaluator's message) become Grasshopper errors,
  verbatim. Nothing falls back to Java or substitutes an algorithm. Exceptions from reading `_scriptPath` propagate as
  before.
- Run folder: PR3's default `<script folder>\SAM_NativeGenOpt\<run>`; TasGenExecute: the installed one. No new inputs.
- Legacy `Run()`, the GPSCoordinateSearch refusal, PR2's cancelled-count convention: unchanged (PR6 / frozen).

## 3. Validation

### Build and automated (this machine; CI on the PR)

- `SAM_Tas_Grasshopper.sln` Release rebuild (VS 18 MSBuild, CI flags): 0 errors, 0 GenOpt warnings, against SAM
  `79c101c2`, SAM_Tas `da891dd2` (GenOpt engine rebuilt from it), SAM_Windows Q3 `05a8eda` (ProgressWindowHost is
  identical in Q4).
- `check-guid-identity.ps1`: passed (104 component GUIDs, baseline unchanged).
- New tests: **50/50** with Rhino 8; **46 passed + 4 skipped** with `SAM_GENOPT_TESTS_NO_RHINO=1` (the CI situation).
- Mutation check, each caught: M1 `Run()` instead of `RunNative` (static tests; 2 fail), M2 `Progress<T>` instead of
  the synchronous adapter (1), M3 golden-section best = last entry (3), M4 late cancel ignored (1), M5 evaluation
  failure as warning (2). Note: M1 was first run against the stub tests, which *executed* the legacy route and hung in
  GenOpt's `cmd C:\c` (PR3 finding 1); that tree was killed and M1 re-checked with the static tests only.

### Real Rhino 8 / Grasshopper acceptance (licensed, local; nothing licensed committed)

Harness in `C:\TasOut\pr4-acc` (not committed). Source: the PR3/Gate T Systems Demo project (`Systems Training`
TBD/TPD/T3D/TSD, Demo script, objectives Result/Cost/CO2), byte-identical to PR3's input, one fresh copy per run.

1. The three definitions (`gs.gh`, `hj.gh`, `cancel.gh`) were **authored and saved with the pre-PR4 plugin** (component
   1.0.1) from existing components only: panels for `_scriptPath` / `_parameters` (`Setpoint,3,-5,35,1,…` and
   `Setpoint,10,-5,35,2,…`) / `_objectives`, the GoldenSection or GPSHookeJeeves algorithm component with its defaults,
   a toggle saved false. No GenOpt example definition exists in the repository; the shipped `.ghuser` files contain no
   GenOpt component.
2. PR4 deployed (`SAM.Analytical.Grasshopper.Tas.GenOpt.gha` SHA-256 `375DBE763ECD…` = build output). Each run:
   Rhino 8 GUI started, Grasshopper editor shown, saved definition opened on the canvas, toggle set, solved. A monitor
   logged every new process with its parent; `HKCU\Software\EDSL\TasManager` was exported before and after.

| Run (final binary) | Result | vs frozen PR3 |
|---|---|---|
| A. GoldenSection, `gs.gh` as saved (1.0.1 component, only `successful`) | Success, 11 simulations, best Setpoint 4.968943799848584, Result 7360.04370117188 (Cost 7360.04370117188, CO2 4076.69276428223), interval [4.767943850222925, 5.093168600454254], `successful = True` | **11/11 evaluations bit-identical** (Variables.txt candidate, Output.txt Result/Cost/CO2); count, outcome, best point and interval equal |
| B. GPSHookeJeeves, `hj.gh` as saved, then the 5 new outputs added the zoomable-UI way | Success, 16 simulations; outputs `outcome = Success`, `simulations = 16`, `bestPoint = [5]`, `bestObjectives = [7360.04370117188, 7360.04370117188, 4076.69276428223]`, `runDirectory` | **16/16 evaluations bit-identical**; count, outcome, best point equal |
| C. Cancel, `cancel.gh` (GoldenSection): dialog **Cancel** invoked (UI Automation) while TasGenExecute #2 ran | TasGenExecute #2 kept running and wrote its results; no evaluation 3 started; remark "cancelled by user", `successful = False`, no best point; 45 s | — |
| D. Refusal: `gs.gh` with a ParametricAlgorithm component on `_algorithm_` | Error "Not supported by the native route: … ParametricAlgorithm … No other algorithm is substituted."; nothing ran | — |

All runs: the definitions opened **without rewiring** (5 inputs, same instance ids, all sources connected); Rhino's
only child processes were TasGenExecute (directly, args[0] = run snapshot) and Rhino's own helpers (Downloader,
rhiexec, Cycles compiler); **no java/javaw process at all**, no `cmd` under Rhino; the TasManager registry key was
**unchanged**. Unrelated `cmd.exe` from other software on the machine appear in the logs with their own parents. The
monitor polls, so it can miss a reused PID (it logged 10/11 and 15/16 TasGenExecute launches); the evaluation folders
are the authoritative count. Dialog captures and logs are in `C:\TasOut\pr4-acc`.

## 4. Findings and risks

1. **Deployment consistency (found in acceptance, environment, not code).** Rhino 8 loads the SAM Rhino package
   (`%APPDATA%\McNeel\Rhinoceros\packages\8.0\SAM\1.0.0`) at start-up. Here it held the pre-PR2 `SAM.Math.dll` (31 KB,
   10-06 install). All SAM assemblies are version 1.0.0.0, so that copy wins and the PR4 plugin's types cannot load:
   Grasshopper silently skips the whole GenOpt plugin ("Unrecognized Objects"). PR3's `RunNative` would fail the same
   way at run time. Fixed locally by replacing that one file with the current `SAM.Math.dll` (original kept in
   `C:\TasOut\pr4-acc\env-backup`); `%APPDATA%\SAM\SAMdependencies\SAM.Math.dll` is also pre-PR2. A SAM_Deploy install
   must ship one SAM.Math (and SAM.Core) to every folder; this is a SAM_Deploy concern, not changed here.
2. **UX**: Grasshopper is busy for the whole run (minutes); only the dialog responds. Matches the Tas workflow
   component; an async component is out of scope.
3. Headless component tests are limited to construction: outside Rhino, Grasshopper's component server cannot start
   (`rhcommon_c`) and shows a modal loading error, so serialization/cloning is covered by the real acceptance only.
4. PR3's refusal text lists GPSCoordinateSearch among "Supported (Phase 1)" although it is refused (D3); cosmetic,
   SAM_Tas, frozen.
5. Acceptance covers the Systems Demo with one parameter (as PR3).

## 5. Next step

Owner review of this PR, then CI on the reviewed head, merge (merge commit, head-SHA protection), post-merge CI, and
the `PROJECT_PROGRESS.md` closeout on `sow/2026-Q4`. Then PR5 (SAM_UI) only when requested; consider the deployment
consistency finding for SAM_Deploy / PR6.
