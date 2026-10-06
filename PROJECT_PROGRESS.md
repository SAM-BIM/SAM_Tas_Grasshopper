# Project Progress - SAM_Tas_Grasshopper (2026-Q4)

## Branch

`sow/2026-Q4` - bootstrapped 2026-10-06 from `master` `b4fce7c8`. Frozen Q3 record: `sow/2026-Q3` @ `9ddf8ff6` (not modified).

## Last updated

2026-10-06 (Q4 operational cleanup).

## Current status

Q4 branch cut from `master` `b4fce7c8`, which is the exact commit pinned in SAM_Deploy's frozen Q3 baseline (`v20261006.1`). Bootstrap added only internal docs (this file, `AGENTS.md`) and a narrow CI branch-reference update (see Decisions). No product source changed. No Q4 product work has started.

## Q4 priorities

Not yet set by the owner. Record them here at the first Q4 planning pass. Known carry-over work is listed below.

## Known carry-over work

- **SAM Grasshopper icon redesign - PR #8** (`feature/sam-gh-icon-redesign` @ `f12a6af2`) was retargeted from `sow/2026-Q3` to `sow/2026-Q4` on 2026-10-06 (not merged). Its head sits directly on the Q3 tip `9ddf8ff6`, which is an ancestor of `master`/`sow/2026-Q4`, so the PR is exactly its 8 icon-only commits (517 files) and is mergeable.
- Branch `contrib/sow-2026-Q3` - preserve - never merge: HoareLea-compatible unrelated lineage (6 commits not in Q3); see repository-specific rules.
- Branch `fix/parto-mixed-diagnostic-filename-2026-09-28` - Q3 complete: merged as SAM_Tas_Grasshopper#7; all commits in Q3.
- Branch `sync/q3-final` - Q3 complete / obsolete: already contained in master.

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

- None at bootstrap.

## Next step

- Owner to set Q4 priorities; then start the first Q4 task from this branch.

## Q4 operational cleanup (2026-10-06)

- Reviewed every active Q2/Q3 reference in this repository on `sow/2026-Q4` (workflow branch filters, dependency-branch resolution, `.gitmodules`/validation, docs). Historical Q2/Q3 mentions (feature documentation records, the frozen Q3 section below) are intentionally unchanged.
- Changed (`af2de7e`): replaced the hard-coded fallback list `['sow/2026-Q4', 'sow/2026-Q3', 'sow/2026-Q2']` in `.github/workflows/build.yml` with a lookup of the newest `sow/YYYY-Qn` branch each dependency has (explicit head/base/canonical-quarter candidates unchanged), so a Q4 build can never fall back to the frozen Q3/Q2 lines and the next quarter needs no edit here. Resolves to `sow/2026-Q4` today.
- Checked, no action: the `github.repository_owner == 'SAM-BIM'` build guard (intentional; its comment names HoareLea only to explain why the guard exists), no CODEOWNERS file in this repository (separate lineage), and workflow secrets (no HoareLea-named secret). The local `upstream` (HoareLea) remote is preserved.
- Carry-over: **SAM Grasshopper icon redesign - PR #8** (`feature/sam-gh-icon-redesign` @ `f12a6af2`) was retargeted from `sow/2026-Q3` to `sow/2026-Q4` on 2026-10-06 (not merged). Its head sits directly on the Q3 tip `9ddf8ff6`, which is an ancestor of `master`/`sow/2026-Q4`, so the PR is exactly its 8 icon-only commits (517 files) and is mergeable.
- Full cross-repository record, migration table and owner decisions: `SAM_Deploy:sow/2026-Q4` `PROJECT_PROGRESS.md`.

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
