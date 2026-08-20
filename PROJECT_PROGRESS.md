# Project Progress

## Branch
`feature/partf-terminal-transfer-compliance` (PR: SAM_Tas_Grasshopper#4 against `sow/2026-Q3`)

## Last updated
2026-08-20 - final pre-merge review pass; Codex backlog triaged, two robustness fixes applied.

## Current status
Part O steps 8-9 are exposed for production use. The TSD query component exposes the TM59
verification report, the Part O diagnostics logging component is registered in the GUID baseline,
and the gbXML workflow component surfaces SAM_Tas's aperture/schedule diagnostics (ISSUE lines as
warnings, everything else as remarks). CI build green at `f023594e`.

## Completed
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

## Decisions / assumptions
- A failed hourly diagnostic companion write is reported as a WARNING, not a run failure -
  the summary file has already written. (Deliberate since the component's first commit; the
  "what does successful mean" question is parked with the same decision on SAM #73.)
- The component layer transports SAM_Tas's notes verbatim; it does not interpret or re-rank
  them. `SAM.Analytical.Tas.Modify.NotePrefix_Issue` is the single severity marker.

## Files changed
- `Grasshopper/SAM.Analytical.Grasshopper.Tas/Component/TasLogPartODiagnostics.cs`
- `Grasshopper/SAM.Analytical.Grasshopper.Tas/Component/Tas.TSDQueryTM59Results.cs`
- `Grasshopper/SAM.Analytical.Grasshopper.Tas/Component/SAMAnalyticalWorkflowgbXML.cs`
- `Grasshopper/SAM.Analytical.Grasshopper.Tas/Modify/RunWorkflow.cs`
- `Grasshopper/SAM.Analytical.Grasshopper.Tas/Component/SAMAnalyticalCreateTBDByTM59.cs`
- `.github/scripts/guid-baseline.json`
- `PROJECT_PROGRESS.md` (this file)

## Validation
- `SAM_Tas_Grasshopper.sln` Release: **0 errors** (this pass, against SAM #73 and SAM_Tas #29
  current heads plus SAM_Systems #14).
- CI `build` + `spdx` green at `f023594e`.

## Issues / blockers
- None blocking. `hourly_` write failure keeps `successful = true` by design (see Decisions).
- Codex fresh-head review unavailable: the `chatgpt-codex-connector` bot refuses
  `@codex review` with "create a Codex account and connect to github". The delta since the
  last reviewed head (`ebdf0bcd`) was reviewed manually.

## Next step
- Wait for SAM #73, SAM_Systems #14 and SAM_Tas #29 to merge first (dependency chain), then
  merge this PR. After the chain merges, update `sow/2026-Q3` and open a fresh feature branch
  for the next Part O stage.
