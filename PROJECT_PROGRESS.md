# Project Progress - SAM_Tas_Grasshopper (2026-Q4)

## Branch

`sow/2026-Q4` - bootstrapped 2026-10-06 from `master` `b4fce7c8`. Frozen Q3 record: `sow/2026-Q3` @ `9ddf8ff6` (not modified).

## Last updated

2026-10-08 (PR #12 merged: shared native result rules, PR6 of the Java-free GenOpt migration). Earlier: 2026-10-07 (PR #11 merged: native GenOpt in Grasshopper, PR4); 2026-10-07 (PR #10 merged: T3D route selector); 2026-10-06 (Q4 operational cleanup).

## Current status

Q4 branch cut from `master` `b4fce7c8`, which is the exact commit pinned in SAM_Deploy's frozen Q3 baseline (`v20261006.1`). Bootstrap added only internal docs (this file, `AGENTS.md`) and a narrow CI branch-reference update (see Decisions).

First Q4 product change merged 2026-10-07: **PR #10** exposes SAM_Tas `WorkflowSettings.T3DRoute` as a Boolean `T3DRoute_` input on `SAMAnalytical.WorkflowgbXML` (see "T3D route selector" below).

Merged 2026-10-07: **PR #11** (PR4 of the Java-free GenOpt migration) - `SAMAnalytical.GenOpt` runs SAM_Tas `GenOptDocument.RunNative` instead of the legacy Java route (see "Native GenOpt in Grasshopper" below). PR4 is frozen.

## Q4 priorities

Not yet set by the owner. Record them here at the first Q4 planning pass. Known carry-over work is listed below.

## Known carry-over work

- **SAM Grasshopper icon redesign - PR #8** (`feature/sam-gh-icon-redesign` @ `f12a6af2`) was retargeted from `sow/2026-Q3` to `sow/2026-Q4` on 2026-10-06 (not merged). Its head sits directly on the Q3 tip `9ddf8ff6`, which is an ancestor of `master`/`sow/2026-Q4`, so the PR is exactly its 8 icon-only commits (517 files) and is mergeable.
- Branch `contrib/sow-2026-Q3` - preserve - never merge: HoareLea-compatible unrelated lineage (6 commits not in Q3); see repository-specific rules.
- Branch `fix/parto-mixed-diagnostic-filename-2026-09-28` - Q3 complete: merged as SAM_Tas_Grasshopper#7; all commits in Q3.
- Branch `sync/q3-final` - Q3 complete / obsolete: already contained in master.
- **Release task (from PR4 acceptance; owner-accepted, not blocking PR4, must be resolved before broad installer acceptance):** "Ensure SAM_Deploy ships one consistent current SAM.Math.dll to all SAM/Rhino package/dependency locations so the PR2/PR3 native optimiser assemblies cannot be shadowed by a stale 1.0.0.0 assembly." Locations seen: `%APPDATA%\McNeel\Rhinoceros\packages\8.0\SAM\1.0.0\` (loaded by Rhino first), `%APPDATA%\SAM\SAMdependencies\`, `%APPDATA%\SAM\`. SAM_Deploy was deliberately not changed in PR4.

## Repository-specific next steps

- Decide how PR #8 is retargeted (it is based on the same history as `master`; Q3 is an ancestor of `master`), then merge per normal review.
- Keep `contrib/sow-2026-Q3` preserved and separate; never join it to normal history.
- Follow the continuity convention in `AGENTS.md` for every PR and closeout.

## Decisions / assumptions

- Q4 base is `master` `b4fce7c8`; the internal files were recovered from `sow/2026-Q3` into this branch only, never onto `master`.
- `sow/2026-Q3` is an ancestor of `master`, so Q4 history includes the full Q3 history.
- Historical Q2/Q3 content below is kept as evidence; its branch names, SHAs and next steps describe Q3 and are not current instructions.
- CI: the hard fallback list for dependency checkout now tries `sow/2026-Q4` first (then the previous Q3/Q2 entries).

## Validation

- Bootstrap verified 2026-10-06: `sow/2026-Q4` was created at exactly `b4fce7c8` and the push was a normal (non-forced) branch creation.

## Issues / blockers

- No blocker in this repository. Open release task: consistent SAM.Math in every SAM_Deploy location (see Known carry-over work).

## Next step

- GenOpt migration: PR5 (SAM_UI) may begin, in a new session; PR6 retires the Java/legacy GenOpt runtime. Resolve the SAM_Deploy SAM.Math release task before broad installer acceptance.
- Owner to set Q4 priorities and decide PR #8; the T3D route selector (PR #10) and native GenOpt (PR #11) are merged and closed.

## Q4 operational cleanup (2026-10-06)

- Reviewed every active Q2/Q3 reference in this repository on `sow/2026-Q4` (workflow branch filters, dependency-branch resolution, `.gitmodules`/validation, docs). Historical Q2/Q3 mentions (feature documentation records, the frozen Q3 section below) are intentionally unchanged.
- Changed (`af2de7e`): replaced the hard-coded fallback list `['sow/2026-Q4', 'sow/2026-Q3', 'sow/2026-Q2']` in `.github/workflows/build.yml` with a lookup of the newest `sow/YYYY-Qn` branch each dependency has (explicit head/base/canonical-quarter candidates unchanged), so a Q4 build can never fall back to the frozen Q3/Q2 lines and the next quarter needs no edit here. Resolves to `sow/2026-Q4` today.
- Checked, no action: the `github.repository_owner == 'SAM-BIM'` build guard (intentional; its comment names HoareLea only to explain why the guard exists), no CODEOWNERS file in this repository (separate lineage), and workflow secrets (no HoareLea-named secret). The local `upstream` (HoareLea) remote is preserved.
- Carry-over: **SAM Grasshopper icon redesign - PR #8** (`feature/sam-gh-icon-redesign` @ `f12a6af2`) was retargeted from `sow/2026-Q3` to `sow/2026-Q4` on 2026-10-06 (not merged). Its head sits directly on the Q3 tip `9ddf8ff6`, which is an ancestor of `master`/`sow/2026-Q4`, so the PR is exactly its 8 icon-only commits (517 files) and is mergeable.
- Full cross-repository record, migration table and owner decisions: `SAM_Deploy:sow/2026-Q4` `PROJECT_PROGRESS.md`.

## Native GenOpt result rules from SAM_Tas (PR #12, merged 2026-10-08)

- **Status:** complete, closed. SAM-BIM/SAM_Tas_Grasshopper#12 (`feature/native-optimisation-pr6-retire-legacy`) merged
  into `sow/2026-Q4` as merge commit `c43c2cadce41c18855145f1b329287b200f5a525` (parents: Q4 base `a01f670d` + reviewed
  PR head `ab2bd87db96b390b56a70226bc82cf82cfd29c13`; merge tree `4413858` identical to the head tree); merge method:
  merge commit with `--match-head-commit`. PR CI (`build`, `spdx`) green on the head; post-merge `Build (Windows)` on
  `c43c2cad` green (built against SAM_Tas `sow/2026-Q4` with SAM_Tas#87). Codex: P1 (record must hold the validation)
  fixed, thread resolved. Branch removed locally and on origin. Record:
  `Grasshopper/SAM.Analytical.Grasshopper.Tas.GenOpt/NATIVE_GENOPT_PR6.md`; full PR6 record: SAM_Tas
  `NATIVE_GENOPT_PR6.md`. Merged after SAM_Tas#87 (merge `8dffaa3d`), per the owner-approved order.
- **Work completed:** `NativeGenOptReport` reads success / withholding / best point / interval / refusal wording from
  SAM_Tas `NativeGenOptOutcome` (own `Best` and refusal mapping removed; log lines, order and record types unchanged).
  `Modify.RunNative` progress text uses `NativeGenOptOutcome.IsLower`. GPSCoordinateSearch now reported as "Not supported
  by the native route" (SAM_Tas PR6). `NoJavaRouteTests.UsesTheSharedResultRules` pins the shared rule.
- **Files changed (6):** `Classes/NativeGenOptReport.cs`, `Modify/RunNative.cs`, `NATIVE_GENOPT_PR6.md`; tests
  `NativeGenOptReportTests.cs`, `NativeBoundaryTests.cs`, `NoJavaRouteTests.cs`.
- **Validation:** Release build of the GenOpt plugin and tests against the PR6 SAM_Tas build: 0 errors. Tests with
  `SAM_GENOPT_TESTS_NO_RHINO=1`: 49 passed, 4 skipped (Rhino-hosted `ComponentContractTests`, also skipped in CI; they
  hung during Rhino start-up in the unattended session; they pin GUID/inputs/outputs, which PR6 did not touch). No Rhino
  or licensed acceptance rerun: wording and native execution unchanged; PR4 acceptance stands.
- **Unresolved issues, risks:** the 4 Rhino-hosted contract tests were not run locally for PR6. Grasshopper users see "Not
  supported" instead of "Invalid GenOpt settings" for GPSCoordinateSearch.
- **Next step:** none for this PR. SAM_Deploy shipping task (current SAM.Math / GenOpt assemblies) remains separate.

## Native GenOpt in Grasshopper (PR #11, merged 2026-10-07)

**Status: merged, frozen.** [PR #11](https://github.com/SAM-BIM/SAM_Tas_Grasshopper/pull/11) `feature/native-genopt-grasshopper` -> `sow/2026-Q4`, merge commit `28c071c5` (reviewed head `5aa29318`, base `0e62cf04`). Normal merge commit pinned to the head SHA (`--match-head-commit`); merged tree identical to the head. Approval is required only for `master`; no CODEOWNERS file or ruleset covers `sow/2026-Q4`. Codex review on the head: completed, no findings. Feature branch deleted locally and on origin. Full record: `Grasshopper/SAM.Analytical.Grasshopper.Tas.GenOpt/NATIVE_GENOPT_GRASSHOPPER.md`.

**Migration context.** PR1 SAM#182 / SAM_Tas#85 (oracle, Gate T), PR2 SAM#183 (SAM.Math kernel), PR3 SAM_Tas#86 (`GenOptDocument.RunNative`, merged `63a5fec7`) - all frozen. Built against SAM `79c101c2`, SAM_Tas `da891dd2`. Next: PR5 SAM_UI, PR6 Java retirement.

**What it does.** The existing `SAMAnalytical.GenOpt` component (GUID `5259b075-7da6-4d20-8364-67225c43dc4c`, SAM WIP > Tas) builds the same `GenOptDocument` from its inputs and runs `GenOptDocument.RunNative` (SAM.Math kernel -> TasGenExecute). No Java, GenOpt jar, cmd, registry project directory, file watcher or listing file. No adapter/evaluator/algorithm logic in Grasshopper.
- Contract: GUID, name, category and the 5 inputs unchanged; output `successful` stays first (now: ended normally and not cancelled); appended `outcome`, `simulations`, `bestPoint`, `bestObjectives`, `runDirectory`. Version 1.0.1 -> 1.0.2 (advisory). Placed components keep their saved output and wiring.
- Progress dialog with Cancel on its own UI thread (`ProgressWindowHost`, the `RunWorkflow` pattern); progress from the kernel's `OptimisationProgress`, delivered synchronously. Cancel is cooperative: a running TasGenExecute finishes, nothing further starts. Run stays synchronous on the Grasshopper thread (UX limitation, as the Tas workflow component).
- Refused algorithms/settings, missing TasGenExecute and evaluation failures are Grasshopper errors, verbatim; no Java fallback or algorithm substitution. `simulations` after a cancel follows the frozen PR2 convention (includes the number assigned at the cancel); the message says so.

**Files changed.** `Grasshopper/SAM.Analytical.Grasshopper.Tas.GenOpt/`: `Component/SAMAnalyticalGenOpt.cs`, new `Create/GenOptDocument.cs`, `Modify/RunNative.cs`, `Classes/NativeGenOptReport.cs`, the `.csproj` (SAM.Core, SAM.Math, SAM.Core.Windows HintPaths), `NATIVE_GENOPT_GRASSHOPPER.md`; new `Tests/SAM.Analytical.Grasshopper.Tas.GenOpt.Tests/` (not in the .sln; reuses SAM_Tas's PR3 `StubTasGenExecute` via ProjectReference); `.github/workflows/build.yml` (one `dotnet test` step).

**Validation.**
- `SAM_Tas_Grasshopper.sln` Release rebuild: 0 errors; `check-guid-identity.ps1` passed (104 GUIDs, baseline unchanged).
- Tests 50/50 with Rhino 8; 46 + 4 skipped without Rhino (CI). Five deliberate mutations each caught.
- PR CI on `5aa29318`: build + spdx green. Post-merge CI on `28c071c5` (run 37659083559): success - solution build, the new test step (46 passed, 4 Rhino-only skipped) and the six-output check.
- Real Rhino 8 / Grasshopper acceptance on the final binary, Systems Demo, definitions saved by the pre-PR4 plugin (harness `C:\TasOut\pr4-acc`, not committed): GoldenSection Success 11 sims, best Setpoint 4.968943799848584 / Result 7360.04370117188, **11/11 evaluations bit-identical to frozen PR3**; GPSHookeJeeves Success 16 sims, bestPoint [5], **16/16 bit-identical**; Cancel on the dialog during evaluation 2 -> it finished, nothing further started; unsupported algorithm refused before execution. Every run: no rewiring, no java/javaw, no cmd under Rhino, `HKCU\Software\EDSL\TasManager` unchanged.

**Known caveats.**
- Release task (carried above): a stale pre-PR2 `SAM.Math.dll` in the Rhino SAM package folder made Grasshopper silently skip the whole GenOpt plugin (all SAM assemblies are 1.0.0.0). Fixed locally only (original kept in `C:\TasOut\pr4-acc\env-backup`).
- Headless tests cover component construction only: outside Rhino, Grasshopper's component server cannot start (`rhcommon_c`) and shows a modal error, so saved-definition reading is covered by the real acceptance.
- PR3's refusal text lists GPSCoordinateSearch as "Supported (Phase 1)" although it is refused (cosmetic, SAM_Tas, frozen).

## T3D route selector (PR #10, merged 2026-10-07)

**Status: merged and closed.** [PR #10](https://github.com/SAM-BIM/SAM_Tas_Grasshopper/pull/10) `feature/t3d-route-selector` -> `sow/2026-Q4`, merge commit `888d92d2` (PR head `28b4d46`, base `72dfe8e1`). Merged with a normal merge commit and head-SHA protection; the feature branch was deleted locally and on origin.

**What it does.** The existing `SAMAnalytical.WorkflowgbXML` component (SAM > Tas, GUID `3a47ec9c-d007-4c80-b91d-d828fb05baa3`) has a new optional **Boolean** input `T3DRoute_`, default `false`, placed directly above `_run` (`_run` stays last). It only sets `WorkflowSettings.T3DRoute`; the Direct T3D implementation is entirely in SAM_Tas (SAM_Tas#84, `f9202503`, on `sow/2026-Q4`).

- `false`, disconnected, null or unreadable -> `T3DRoute.GbXML` (the current workflow). Only an explicit `true` -> `T3DRoute.Direct`. Direct can never be selected by omission.
- Every run adds the remark `T3D route: GbXML.` or `T3D route: Direct.`; on Direct the SAM_Tas core also adds a `Direct T3D conversion: ...` note.
- With Direct the core ignores the gbXML file, but the component still requires `_pathgbXML` (unchanged).

**Backward compatibility.** The 17 original inputs are unchanged in name and relative order and are found by name; component GUID unchanged; component version bumped `1.0.12` -> `1.0.13` so placed components are offered the update. An older placed component without the input behaves exactly as before (GbXML).

**Files changed.** `Grasshopper/SAM.Analytical.Grasshopper.Tas/Component/SAMAnalyticalWorkflowgbXML.cs` only (net of the PR; an intermediate string-input version and its `Query/TryGetT3DRoute.cs` helper were removed before merge).

**Dependency.** None pinned in this repo: it builds against sibling `SAM_Tas\build\`, and CI clones the newest `sow/YYYY-Qn` (`sow/2026-Q4`, which contains the core). No dependency change. Local builds need SAM_Tas at `sow/2026-Q4` (a Q3 checkout has no `T3DRoute`).

**Validation.**
- CI on the PR head `28b4d46`: `build` and `spdx` both green; PR clean, no reviews or comments.
- Local Debug/Release build of `SAM_Tas_Grasshopper.sln` against SAM_Tas Q4: 0 errors; `check-guid-identity.ps1` passed (104 component GUIDs).
- Headless checks on the built DLL: 17 original inputs in the original order, `T3DRoute_` 17th of 18 (Boolean, optional, default false), `_run` last, GUID unchanged, default `WorkflowSettings.T3DRoute == GbXML`.
- Manual Grasshopper acceptance (owner, 2026-10-07): false/disconnected -> GbXML; true -> Direct with the `Direct T3D conversion:` remark and the expected T3D/TBD; wiring usable after re-adding the input. On a 3-flat model the Direct run matched the gbXML run (identical object counts per type, areas, volumes and zone max sensible loads; load components within 0.2%); `timing.csv` shows `Converting SAM to T3D` in place of `Importing gbXML`. The final move of the input above `_run` was re-checked headlessly; the owner was asked to re-check it in Grasshopper and gave the go-ahead to merge.

**Known caveats / not tested.**
- The very oldest fixed-parameter `.gh` files (saved before the component had variable inputs) restore inputs by position; inserting the default-visible `T3DRoute_` before `_run` could shift `_run` there. Not tested with such a file. Mitigation if it shows up: make `T3DRoute_` `ParamVisibility.Voluntary`.
- Opening the Direct `.t3d`/`.tbd` in Tas was checked by the owner only.
- `SAM_Tas-ord` (local, dirty `codex/sam113-canonical-native-order`) and `sam-bim.github.io` (stale upstream) have pull problems unrelated to this work; `BuildAlls_v4.bat pull` aborts on them.

**Next step.** None required for this feature. Remaining Q4 items: owner to set Q4 priorities; decide PR #8 (icon redesign), see carry-over above. SAM_UI exposure of the route is out of scope here and was deliberately not started.

---

# Historical record - 2026-Q3 (frozen)

Source: tip of `sow/2026-Q3` (`9ddf8ff6`). Preserved verbatim except that heading levels are shifted down one. Everything below describes Q3 and is not a current instruction.

## Project Progress

### Branch
`sow/2026-Q3` (SAM_Tas_Grasshopper#4, `feature/partf-terminal-transfer-compliance`, is merged).

### Last updated
2026-09-28 - Mixed Part O PR3B follow-up (branch `fix/parto-mixed-diagnostic-filename-2026-09-28`, [SAM_Tas_Grasshopper#7](https://github.com/SAM-BIM/SAM_Tas_Grasshopper/pull/7)). Earlier: 2026-09-22 - .NET Framework app.config cleanup merged. Earlier: 2026-08-20 - final pre-merge review pass; Codex backlog triaged, two robustness fixes applied.

### Current status
Part O steps 8-9 are exposed for production use. The TSD query component exposes the TM59
verification report, the Part O diagnostics logging component is registered in the GUID baseline,
and the gbXML workflow component surfaces SAM_Tas's aperture/schedule diagnostics (ISSUE lines as
warnings, everything else as remarks). CI build green at `f023594e`.

### Completed
- 2026-09-28: [SAM_Tas_Grasshopper#7](https://github.com/SAM-BIM/SAM_Tas_Grasshopper/pull/7) - `TasLogPartODiagnostics` names the log file `<tsd>.<iteration>.partO.<timestamp>.jsonl` from `PartODiagnosticLog.RunPartOIteration` instead of `overheatingScenarios[0].Iteration`: the dwellings' one iteration, `Mixed` when they differ (a mixed building - Mixed Part O PR3B), `Undefined` when there is no scenario. Same rule as the log's own run record. **Depends on SAM_Tas#71** (merged `e7cc0ed`, adds `RunPartOIteration`); build against SAM_Tas `sow/2026-Q3` at or after it. Built locally (VS 18 MSBuild, Release, 0 errors); CI build + spdx green. No behaviour change for an ordinary single-iteration run.
- 2026-09-22: [SAM_Tas_Grasshopper#5](https://github.com/SAM-BIM/SAM_Tas_Grasshopper/pull/5) merged as `40d289f8` - deleted `Grasshopper/SAM.Analytical.Grasshopper.Tas.GenOpt/app.config`, `Grasshopper/SAM.Analytical.Grasshopper.Tas/app.config`, `Grasshopper/SAM.Core.Grasshopper.Tas/app.config`, `Grasshopper/SAM.Weather.Grasshopper.Tas/app.config`. These were inert net472-era binding-redirect files in net8.0-windows Library projects. Part of the repo-family .NET Framework `app.config` cleanup: base [SAM#126](https://github.com/SAM-BIM/SAM/pull/126) plus 17 sibling PRs, all merged into `sow/2026-Q3` on 2026-09-22 (SAM first), with their branches deleted. Validated by a full `BuildAlls_v4.bat` clean rebuild (exit 0, 0 errors) and CI build + spdx.
- Exposed the TM59 verification report on the TSD query component (Tas.TSDQueryTM59Results).
- Added the Part O diagnostics logging component (TasLogPartODiagnostics), which consumes
  SAM_Tas's `PartODiagnosticLog` and writes a summary JSONL plus an optional hourly companion.
- Registered the component's GUID (`fb234a4c-4302-4490-b6a7-6b19f9495fbc`) in
  `.github/scripts/guid-baseline.json`, which the build workflow checks strictly.
- Added voluntary text-file export to Tas.TSDQueryTM59Results.
- Surfaced the TAS workflow's own notes (aperture-type skips and schedule refusals) on the
  `SAMAnalyticalWorkflowgbXML` canvas: `ISSUE:` lines as warnings, summaries as remarks.
  Successful runs contribute summary remarks only - per-aperture lines are emitted only for a
  schedule that failed to reach the TBD.
- Final review fixes (this pass):
  - `SAMAnalyticalCreateTBDByTM59`: deleting a previous run's stale TM59 XML is now guarded - a
    locked or read-only file reports a warning instead of throwing out of `SolveInstance`
    (Codex 3813638645).
  - `TasLogPartODiagnostics`: log filenames now carry millisecond precision, so two runs within
    the same second for the same TSD and iteration no longer overwrite each other (Codex
    3802553097).
  - `PROJECT_PROGRESS.md`: this record (Codex 3805519220).

### Decisions / assumptions
- A failed hourly diagnostic companion write is reported as a WARNING, not a run failure -
  the summary file has already written. (Deliberate since the component's first commit; the
  "what does successful mean" question is parked with the same decision on SAM #73.)
- The component layer transports SAM_Tas's notes verbatim; it does not interpret or re-rank
  them. `SAM.Analytical.Tas.Modify.NotePrefix_Issue` is the single severity marker.

### Files changed
- `Grasshopper/SAM.Analytical.Grasshopper.Tas/Component/TasLogPartODiagnostics.cs`
- `Grasshopper/SAM.Analytical.Grasshopper.Tas/Component/Tas.TSDQueryTM59Results.cs`
- `Grasshopper/SAM.Analytical.Grasshopper.Tas/Component/SAMAnalyticalWorkflowgbXML.cs`
- `Grasshopper/SAM.Analytical.Grasshopper.Tas/Modify/RunWorkflow.cs`
- `Grasshopper/SAM.Analytical.Grasshopper.Tas/Component/SAMAnalyticalCreateTBDByTM59.cs`
- `.github/scripts/guid-baseline.json`
- `PROJECT_PROGRESS.md` (this file)

### Validation
- `SAM_Tas_Grasshopper.sln` Release: **0 errors** (this pass, against SAM #73 and SAM_Tas #29
  current heads plus SAM_Systems #14).
- CI `build` + `spdx` green at `f023594e`.

### Issues / blockers
- None blocking. `hourly_` write failure keeps `successful = true` by design (see Decisions).
- Codex fresh-head review unavailable: the `chatgpt-codex-connector` bot refuses
  `@codex review` with "create a Codex account and connect to github". The delta since the
  last reviewed head (`ebdf0bcd`) was reviewed manually.

### Next step
- Wait for SAM #73, SAM_Systems #14 and SAM_Tas #29 to merge first (dependency chain), then
  merge this PR. After the chain merges, update `sow/2026-Q3` and open a fresh feature branch
  for the next Part O stage.
